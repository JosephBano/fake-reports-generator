using FakeReports.Api.Modelos;

namespace FakeReports.Api.Analisis;

public static class AnalizadorAsistencias
{
    public static ResultadoAnalisis Analizar(ReporteRequest request)
    {
        NormalizarZonasHorarias(request);

        var dias = new List<DiaAnalizado>();
        var resumen = new ResumenReporte();
        var fecha = request.FechaDesde.Date;
        var fechaHasta = request.FechaHasta.Date;

        while (fecha <= fechaHasta)
        {
            var dia = AnalizarDia(fecha, request);
            dias.Add(dia);
            fecha = fecha.AddDays(1);
        }

        foreach (var dia in dias.Where(d => !d.EsDiaExcluido))
        {
            switch (dia.Estado)
            {
                case "ausente": resumen.Ausencias++; break;
                case "severa": resumen.TardanzasSeveras++; break;
                case "leve": resumen.TardanzasLeves++; break;
                case "anomalo": resumen.RegistrosAnomalos++; break;
            }

            if (dia.AlmuerzoExcesoMinutos > 0)
                resumen.ExcesosAlmuerzo++;

            if (dia.SalidaAnticipadaMinutos > 0)
                resumen.SalidasAnticipadas++;

            if (!string.IsNullOrEmpty(dia.Justificacion))
                resumen.Justificaciones++;
        }

        resumen.TotalDias = dias.Count(d => !d.EsDiaExcluido && !d.EsFeriado && !d.EsDiaLibre);

        return new ResultadoAnalisis
        {
            Persona = request.Persona,
            Resumen = resumen,
            Dias = dias,
            Configuracion = request.Configuracion,
            FechaDesde = request.FechaDesde,
            FechaHasta = request.FechaHasta
        };
    }

    private static DiaAnalizado AnalizarDia(DateTime fecha, ReporteRequest request)
    {
        var dia = new DiaAnalizado
        {
            Fecha = fecha,
            EsDiaExcluido = request.DiasExcluidos.Any(d => d.Date == fecha),
            EsFeriado = request.Feriados.Any(d => d.Date == fecha)
        };

        if (dia.EsDiaExcluido)
        {
            dia.Estado = "excluido";
            dia.Observaciones.Add("Día excluido del reporte.");
            return dia;
        }

        var horarioDia = ObtenerHorarioDia(fecha.DayOfWeek, request.Horario);
        dia.HoraProgramadaEntrada = ParseHora(horarioDia?.Entrada);
        dia.HoraProgramadaSalida = ParseHora(horarioDia?.Salida);
        dia.EsDiaLibre = string.IsNullOrWhiteSpace(horarioDia?.Entrada) || string.IsNullOrWhiteSpace(horarioDia?.Salida);

        if (dia.EsFeriado || dia.EsDiaLibre)
        {
            dia.Estado = dia.EsFeriado ? "feriado" : "libre";
            return dia;
        }

        var registrosDia = request.Registros
            .Where(r => r.FechaHora.Date == fecha)
            .OrderBy(r => r.FechaHora)
            .ToList();

        if (registrosDia.Count == 0)
        {
            dia.Estado = "ausente";
            dia.Observaciones.Add("No se registraron marcaciones.");
            AplicarJustificacion(dia, request.Justificaciones);
            return dia;
        }

        dia.Registros = registrosDia;

        var tipos = registrosDia.Select(r => r.Tipo.ToLowerInvariant()).ToList();
        var secuenciaValida = tipos.SequenceEqual(new[] { "entrada", "salida" })
            || tipos.SequenceEqual(new[] { "entrada", "salida", "entrada", "salida" });

        if (!secuenciaValida)
        {
            dia.Estado = "anomalo";
            dia.Observaciones.Add($"Secuencia de marcaciones no válida: {string.Join(", ", tipos)}.");
        }

        // Primera entrada y última salida
        var entrada = registrosDia.FirstOrDefault(r => r.Tipo.ToLowerInvariant() == "entrada");
        var salida = registrosDia.LastOrDefault(r => r.Tipo.ToLowerInvariant() == "salida");

        if (entrada != null)
        {
            dia.Llegada = entrada.FechaHora.TimeOfDay;
            if (dia.HoraProgramadaEntrada.HasValue)
            {
                var retraso = (int)(dia.Llegada.Value - dia.HoraProgramadaEntrada.Value).TotalMinutes;
                if (retraso > 0)
                {
                    dia.RetrasoMinutos = retraso;
                    if (retraso >= request.Configuracion.UmbralTardanzaSeveraMin)
                        dia.Estado = "severa";
                    else if (retraso >= request.Configuracion.UmbralTardanzaLeveMin)
                        dia.Estado = dia.Estado == "anomalo" ? "anomalo" : "leve";
                }
            }
        }

        if (salida != null)
        {
            dia.Salida = salida.FechaHora.TimeOfDay;
            if (dia.HoraProgramadaSalida.HasValue)
            {
                var diferencia = (int)(dia.HoraProgramadaSalida.Value - dia.Salida.Value).TotalMinutes;
                if (diferencia > 0)
                    dia.SalidaAnticipadaMinutos = diferencia;
            }
        }

        // Almuerzo con 4 registros
        if (registrosDia.Count == 4)
        {
            dia.AlmuerzoSalida = registrosDia[1].FechaHora.TimeOfDay;
            dia.AlmuerzoRegreso = registrosDia[2].FechaHora.TimeOfDay;
            dia.AlmuerzoDuracionMinutos = (int)(dia.AlmuerzoRegreso.Value - dia.AlmuerzoSalida.Value).TotalMinutes;
            var exceso = dia.AlmuerzoDuracionMinutos - request.Horario.AlmuerzoMinutos;
            if (exceso > request.Configuracion.UmbralExcesoAlmuerzoMin)
                dia.AlmuerzoExcesoMinutos = exceso;
        }

        // Tiempo neto dentro de la oficina
        if (entrada != null && salida != null)
        {
            var tiempoTotal = (int)(salida.FechaHora - entrada.FechaHora).TotalMinutes;
            dia.TiempoNetoMinutos = tiempoTotal - dia.AlmuerzoDuracionMinutos;
        }

        AplicarJustificacion(dia, request.Justificaciones);
        return dia;
    }

    private static void NormalizarZonasHorarias(ReporteRequest request)
    {
        foreach (var r in request.Registros)
        {
            r.FechaHora = r.FechaHora.Kind switch
            {
                DateTimeKind.Utc => r.FechaHora.ToLocalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(r.FechaHora, DateTimeKind.Local),
                _ => r.FechaHora
            };
        }
    }

    private static TimeSpan? ParseHora(string? hora)
    {
        if (string.IsNullOrWhiteSpace(hora)) return null;
        if (TimeSpan.TryParse(hora, out var result)) return result;
        return null;
    }

    private static HorarioDia? ObtenerHorarioDia(DayOfWeek dia, HorarioSemanal horario)
    {
        return dia switch
        {
            DayOfWeek.Monday => horario.Lunes,
            DayOfWeek.Tuesday => horario.Martes,
            DayOfWeek.Wednesday => horario.Miercoles,
            DayOfWeek.Thursday => horario.Jueves,
            DayOfWeek.Friday => horario.Viernes,
            DayOfWeek.Saturday => horario.Sabado,
            DayOfWeek.Sunday => horario.Domingo,
            _ => null
        };
    }

    private static void AplicarJustificacion(DiaAnalizado dia, List<Justificacion> justificaciones)
    {
        var just = justificaciones.FirstOrDefault(j => j.Fecha.Date == dia.Fecha);
        if (just != null)
        {
            dia.Justificacion = $"{just.Tipo}: {just.Descripcion}";
            dia.Observaciones.Add(dia.Justificacion);
        }
    }
}
