using FakeReports.Api.Analisis;
using FakeReports.Api.Modelos;

namespace FakeReports.Api.Tests;

public class AnalizadorAsistenciasTests
{
    private static ReporteRequest CrearRequestBase(
        DateTime desde,
        DateTime hasta,
        Action<ReporteRequest>? configurar = null)
    {
        var request = new ReporteRequest
        {
            Persona = new Persona { Id = Guid.NewGuid(), Nombre = "Juan Pérez", Identificacion = "12345678" },
            Horario = new HorarioSemanal
            {
                Id = Guid.NewGuid(),
                Nombre = "Horario estándar",
                AlmuerzoMinutos = 60,
                Lunes = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Martes = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Miercoles = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Jueves = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Viernes = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Sabado = new HorarioDia(),
                Domingo = new HorarioDia()
            },
            Configuracion = new ConfiguracionReporte
            {
                UmbralTardanzaLeveMin = 1,
                UmbralTardanzaSeveraMin = 6,
                UmbralExcesoAlmuerzoMin = 1
            },
            FechaDesde = desde,
            FechaHasta = hasta
        };
        configurar?.Invoke(request);
        return request;
    }

    private static RegistroAsistencia Registro(DateTime fecha, string hora, string tipo)
    {
        var partes = hora.Split(':');
        return new RegistroAsistencia
        {
            FechaHora = fecha.AddHours(int.Parse(partes[0])).AddMinutes(int.Parse(partes[1])),
            Tipo = tipo
        };
    }

    [Fact]
    public void Dia_Laborable_Con_Entrada_Y_Salida_Correctas_Es_Ok()
    {
        var fecha = new DateTime(2026, 6, 1); // lunes
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        var dia = Assert.Single(resultado.Dias);
        Assert.Equal("ok", dia.Estado);
        Assert.Equal(0, dia.RetrasoMinutos);
        Assert.Equal(0, dia.SalidaAnticipadaMinutos);
        Assert.False(dia.EsFeriado);
        Assert.False(dia.EsDiaLibre);
        Assert.Equal(1, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Dia_Con_Tardanza_Leve()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:02", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("leve", resultado.Dias[0].Estado);
        Assert.Equal(2, resultado.Dias[0].RetrasoMinutos);
        Assert.Equal(1, resultado.Resumen.TardanzasLeves);
    }

    [Fact]
    public void Dia_Con_Tardanza_Severa()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:10", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("severa", resultado.Dias[0].Estado);
        Assert.Equal(10, resultado.Dias[0].RetrasoMinutos);
        Assert.Equal(1, resultado.Resumen.TardanzasSeveras);
    }

    [Fact]
    public void Dia_Sin_Registros_Es_Ausente()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha);

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ausente", resultado.Dias[0].Estado);
        Assert.Contains("No se registraron marcaciones.", resultado.Dias[0].Observaciones);
        Assert.Equal(1, resultado.Resumen.Ausencias);
    }

    [Fact]
    public void Dia_Sabado_Es_Libre()
    {
        var fecha = new DateTime(2026, 6, 6); // sábado
        var request = CrearRequestBase(fecha, fecha);

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("libre", resultado.Dias[0].Estado);
        Assert.True(resultado.Dias[0].EsDiaLibre);
        Assert.Equal(0, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Feriado_No_Cuenta_Como_Dia_Laborable()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Feriados.Add(fecha));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("feriado", resultado.Dias[0].Estado);
        Assert.True(resultado.Dias[0].EsFeriado);
        Assert.Equal(0, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Dia_Excluido_No_Afecta_Resumen()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.DiasExcluidos.Add(fecha));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("excluido", resultado.Dias[0].Estado);
        Assert.True(resultado.Dias[0].EsDiaExcluido);
        Assert.Equal(0, resultado.Resumen.TotalDias);
        Assert.Equal(0, resultado.Resumen.Ausencias);
    }

    [Fact]
    public void Secuencia_Invalida_Es_Anomala()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "12:00", "entrada")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("anomalo", resultado.Dias[0].Estado);
        Assert.Contains(resultado.Dias[0].Observaciones, o => o.Contains("Secuencia de marcaciones no válida"));
        Assert.Equal(1, resultado.Resumen.RegistrosAnomalos);
    }

    [Fact]
    public void Salida_Anticipada()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "16:30", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(30, resultado.Dias[0].SalidaAnticipadaMinutos);
        Assert.Equal(1, resultado.Resumen.SalidasAnticipadas);
    }

    [Fact]
    public void Almuerzo_Con_Exceso()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "12:00", "salida"),
                Registro(fecha, "13:10", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(70, resultado.Dias[0].AlmuerzoDuracionMinutos);
        Assert.Equal(10, resultado.Dias[0].AlmuerzoExcesoMinutos);
        Assert.Equal(1, resultado.Resumen.ExcesosAlmuerzo);
    }

    [Fact]
    public void Almuerzo_Sin_Exceso_No_Suma()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "12:00", "salida"),
                Registro(fecha, "12:45", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal(45, resultado.Dias[0].AlmuerzoDuracionMinutos);
        Assert.Equal(0, resultado.Dias[0].AlmuerzoExcesoMinutos);
        Assert.Equal(0, resultado.Resumen.ExcesosAlmuerzo);
    }

    [Fact]
    public void Justificacion_Agrega_Observacion_Y_Suma()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Justificaciones.Add(new Justificacion
            {
                Fecha = fecha,
                Tipo = "tardanza",
                Descripcion = "Tránsito"
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Contains("tardanza: Tránsito", resultado.Dias[0].Observaciones);
        Assert.Equal("tardanza: Tránsito", resultado.Dias[0].Justificacion);
        Assert.Equal(1, resultado.Resumen.Justificaciones);
    }

    [Fact]
    public void Tiempo_Neto_Considera_Almuerzo()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "12:00", "salida"),
                Registro(fecha, "13:00", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        // 9 horas brutas - 1 hora almuerzo = 8 horas = 480 minutos
        Assert.Equal(480, resultado.Dias[0].TiempoNetoMinutos);
    }

    [Fact]
    public void Normaliza_Registros_Utc_A_Local()
    {
        var fecha = new DateTime(2026, 6, 1);
        var entradaUtc = DateTime.SpecifyKind(fecha.AddHours(11), DateTimeKind.Utc); // 08:00 local si offset -3
        var salidaUtc = DateTime.SpecifyKind(fecha.AddHours(20), DateTimeKind.Utc);  // 17:00 local
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                new RegistroAsistencia { FechaHora = entradaUtc, Tipo = "entrada" },
                new RegistroAsistencia { FechaHora = salidaUtc, Tipo = "salida" }
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(0, resultado.Dias[0].RetrasoMinutos);
    }

    [Fact]
    public void Rango_De_Fechas_Genera_Un_Dia_Por_Fecha()
    {
        var desde = new DateTime(2026, 6, 1);
        var hasta = new DateTime(2026, 6, 5);
        var request = CrearRequestBase(desde, hasta);

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal(5, resultado.Dias.Count);
        Assert.Equal(desde, resultado.Dias.First().Fecha.Date);
        Assert.Equal(hasta, resultado.Dias.Last().Fecha.Date);
    }

    [Fact]
    public void Resumen_TotalDias_Excluye_Feriados_Y_Libres()
    {
        var desde = new DateTime(2026, 6, 1); // lunes
        var hasta = new DateTime(2026, 6, 7); // domingo
        var request = CrearRequestBase(desde, hasta, r =>
        {
            r.Feriados.Add(new DateTime(2026, 6, 3)); // miércoles feriado
            // sábado y domingo son libres por horario
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        // De los 7 días: 5 laborables, 1 feriado, 1 sábado libre + 1 domingo libre
        Assert.Equal(7, resultado.Dias.Count);
        Assert.Equal(4, resultado.Resumen.TotalDias); // 5 laborables - 1 feriado = 4
    }

    [Fact]
    public void Resumen_Acumula_Todos_Los_Conceptos_En_Un_Rango()
    {
        var desde = new DateTime(2026, 6, 1);
        var hasta = new DateTime(2026, 6, 5); // lunes a viernes
        var request = CrearRequestBase(desde, hasta, r =>
        {
            // Lunes: OK
            r.Registros.AddRange(new[]
            {
                Registro(desde, "08:00", "entrada"),
                Registro(desde, "17:00", "salida")
            });
            // Martes: severa
            r.Registros.AddRange(new[]
            {
                Registro(desde.AddDays(1), "08:15", "entrada"),
                Registro(desde.AddDays(1), "17:00", "salida")
            });
            // Miércoles: leve
            r.Registros.AddRange(new[]
            {
                Registro(desde.AddDays(2), "08:03", "entrada"),
                Registro(desde.AddDays(2), "17:00", "salida")
            });
            // Jueves: salida anticipada
            r.Registros.AddRange(new[]
            {
                Registro(desde.AddDays(3), "08:00", "entrada"),
                Registro(desde.AddDays(3), "16:00", "salida")
            });
            // Viernes: ausente (sin registros)
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal(1, resultado.Resumen.TardanzasSeveras);
        Assert.Equal(1, resultado.Resumen.TardanzasLeves);
        Assert.Equal(1, resultado.Resumen.SalidasAnticipadas);
        Assert.Equal(1, resultado.Resumen.Ausencias);
        Assert.Equal(5, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Secuencia_Valida_Con_Almuerzo_Con_Dos_Entradas_Y_Dos_Salidas_Es_Ok()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "12:00", "salida"),
                Registro(fecha, "13:00", "entrada"),
                Registro(fecha, "17:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(60, resultado.Dias[0].AlmuerzoDuracionMinutos);
        Assert.Equal(0, resultado.Dias[0].AlmuerzoExcesoMinutos);
    }

    [Fact]
    public void Tipo_Mezclado_Mayusculas_Y_Minusculas_Es_Aceptado()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "ENTRADA"),
                Registro(fecha, "17:00", "SALIDA")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(0, resultado.Dias[0].RetrasoMinutos);
    }

    [Fact]
    public void Justificacion_No_Contabiliza_Para_Dias_Excluidos()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
        {
            r.DiasExcluidos.Add(fecha);
            r.Justificaciones.Add(new Justificacion { Fecha = fecha, Tipo = "vacaciones", Descripcion = "No aplica" });
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("excluido", resultado.Dias[0].Estado);
        Assert.Equal(0, resultado.Resumen.Justificaciones);
        Assert.Equal(0, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Justificacion_Sobre_Dia_Ausente_Lo_Mantiene_Ausente_Pero_Cuenta()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Justificaciones.Add(new Justificacion { Fecha = fecha, Tipo = "enfermedad", Descripcion = "Certificado médico" }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ausente", resultado.Dias[0].Estado);
        Assert.Equal(1, resultado.Resumen.Ausencias);
        Assert.Equal(1, resultado.Resumen.Justificaciones);
        Assert.Equal(1, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Secuencia_Invalida_Con_Tardanza_Leve_Mantiene_Anomalo()
    {
        // Secuencia inválida (3 entradas) con tardanza leve: el estado debe quedar
        // "anomalo" porque la rama leve preserva "anomalo" si ya estaba marcado.
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:02", "entrada"),
                Registro(fecha, "12:00", "entrada"),
                Registro(fecha, "17:00", "entrada")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("anomalo", resultado.Dias[0].Estado);
        Assert.Equal(2, resultado.Dias[0].RetrasoMinutos);
        Assert.Contains(resultado.Dias[0].Observaciones, o => o.Contains("Secuencia de marcaciones no válida"));
    }

    [Fact]
    public void Secuencia_Invalida_Con_Tardanza_Severa_Sobre_Escritura_A_Severa()
    {
        // Secuencia inválida + tardanza severa: documenta el comportamiento real,
        // donde la rama severa pisa el "anomalo" detectado por la rama de secuencia.
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:10", "entrada"),
                Registro(fecha, "12:00", "entrada"),
                Registro(fecha, "17:00", "entrada")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("severa", resultado.Dias[0].Estado);
        Assert.Equal(10, resultado.Dias[0].RetrasoMinutos);
        // La observación de secuencia inválida se conserva aunque el estado sea severa.
        Assert.Contains(resultado.Dias[0].Observaciones, o => o.Contains("Secuencia de marcaciones no válida"));
    }

    [Fact]
    public void Dia_Feriado_Con_Horario_Y_Registros_Sigue_Siendo_Feriado()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
        {
            r.Feriados.Add(fecha);
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "17:00", "salida")
            });
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("feriado", resultado.Dias[0].Estado);
        Assert.True(resultado.Dias[0].EsFeriado);
        Assert.Equal(0, resultado.Resumen.TotalDias);
        Assert.Equal(0, resultado.Resumen.Ausencias);
    }

    [Fact]
    public void Dia_Libre_Con_Horario_Vacio_Es_Libre_Incluso_Sin_Registros()
    {
        var fecha = new DateTime(2026, 6, 6); // sábado, sin horario en CrearRequestBase
        var request = CrearRequestBase(fecha, fecha);

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("libre", resultado.Dias[0].Estado);
        Assert.True(resultado.Dias[0].EsDiaLibre);
        Assert.False(resultado.Dias[0].EsFeriado);
    }

    [Fact]
    public void Umbrales_Personalizados_Se_Aplican()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
        {
            r.Configuracion.UmbralTardanzaLeveMin = 10;
            r.Configuracion.UmbralTardanzaSeveraMin = 20;
            // Llega 8 minutos tarde → con umbrales 10/20 NO debe contar como tardanza
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:08", "entrada"),
                Registro(fecha, "17:00", "salida")
            });
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(8, resultado.Dias[0].RetrasoMinutos);
        Assert.Equal(0, resultado.Resumen.TardanzasLeves);
        Assert.Equal(0, resultado.Resumen.TardanzasSeveras);
    }

    [Fact]
    public void Tiempo_Neto_Sin_Almuerzo_Usa_Diferencia_Bruta()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
            r.Registros.AddRange(new[]
            {
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "12:00", "salida")
            }));

        var resultado = AnalizadorAsistencias.Analizar(request);

        // 2 registros → no se computa almuerzo
        Assert.Equal(0, resultado.Dias[0].AlmuerzoDuracionMinutos);
        // Tiempo bruto = 4h = 240min, sin almuerzo a restar
        Assert.Equal(240, resultado.Dias[0].TiempoNetoMinutos);
    }

    [Fact]
    public void Resultado_Conserva_Configuracion_Persona_Y_Fechas()
    {
        var desde = new DateTime(2026, 6, 1);
        var hasta = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(desde, hasta, r =>
        {
            r.Configuracion.Titulo = "TITULO X";
            r.Persona.Nombre = "Ana";
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("TITULO X", resultado.Configuracion.Titulo);
        Assert.Equal("Ana", resultado.Persona.Nombre);
        Assert.Equal(desde, resultado.FechaDesde);
        Assert.Equal(hasta, resultado.FechaHasta);
    }

    [Fact]
    public void Rango_Invertido_Genera_Lista_Vacia_Sin_Crashear()
    {
        var desde = new DateTime(2026, 6, 10);
        var hasta = new DateTime(2026, 6, 1); // invertido
        var request = CrearRequestBase(desde, hasta);

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Empty(resultado.Dias);
        Assert.Equal(0, resultado.Resumen.TotalDias);
    }

    [Fact]
    public void Registros_De_Otros_Dias_No_Contaminan_El_Dia_Analizado()
    {
        var fecha = new DateTime(2026, 6, 1);
        var request = CrearRequestBase(fecha, fecha, r =>
        {
            // registros en otro día no deben interferir
            r.Registros.AddRange(new[]
            {
                Registro(new DateTime(2026, 6, 2), "08:00", "entrada"),
                Registro(new DateTime(2026, 6, 2), "17:00", "salida"),
                Registro(fecha, "08:00", "entrada"),
                Registro(fecha, "17:00", "salida")
            });
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(2, resultado.Dias[0].Registros.Count);
    }

    [Fact]
    public void Varios_Dias_Con_Mezcla_De_Estados_Calcula_Resumen_Correcto()
    {
        var desde = new DateTime(2026, 6, 1);
        var hasta = new DateTime(2026, 6, 3);
        var request = CrearRequestBase(desde, hasta, r =>
        {
            // Lunes: leve
            r.Registros.AddRange(new[]
            {
                Registro(desde, "08:02", "entrada"),
                Registro(desde, "17:00", "salida")
            });
            // Martes: exceso de almuerzo
            r.Registros.AddRange(new[]
            {
                Registro(desde.AddDays(1), "08:00", "entrada"),
                Registro(desde.AddDays(1), "12:00", "salida"),
                Registro(desde.AddDays(1), "13:30", "entrada"),
                Registro(desde.AddDays(1), "17:00", "salida")
            });
            // Miércoles: sin registros (ausente)
        });

        var resultado = AnalizadorAsistencias.Analizar(request);

        Assert.Equal(3, resultado.Dias.Count);
        Assert.Equal("leve", resultado.Dias[0].Estado);
        Assert.Equal("ok", resultado.Dias[1].Estado);
        Assert.Equal("ausente", resultado.Dias[2].Estado);

        Assert.Equal(1, resultado.Resumen.TardanzasLeves);
        Assert.Equal(1, resultado.Resumen.ExcesosAlmuerzo);
        Assert.Equal(1, resultado.Resumen.Ausencias);
        Assert.Equal(3, resultado.Resumen.TotalDias);
    }
}
