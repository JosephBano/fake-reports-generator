using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FakeReports.Api.Analisis;
using FakeReports.Api.Modelos;

namespace FakeReports.Api.Generadores.Word;

public static class ReportePorPersonaWordGenerator
{
    public static byte[] Generar(ResultadoAnalisis resultado)
    {
        var cfg = resultado.Configuracion;
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Portada
            body.Append(Parrafo("REPORTE POR PERSONA", true, 28, cfg.ColorHeader, JustificationValues.Center));
            body.Append(Parrafo(resultado.Persona.Nombre, true, 18, null, JustificationValues.Center));
            body.Append(Parrafo($"Identificación: {resultado.Persona.Identificacion}", false, 11, null, JustificationValues.Center));
            body.Append(Parrafo($"Período: {resultado.FechaDesde:dd/MM/yyyy} - {resultado.FechaHasta:dd/MM/yyyy}", false, 11, null, JustificationValues.Center));
            body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // Resumen
            if (cfg.IncluirResumen)
            {
                body.Append(Parrafo("RESUMEN GENERAL", true, 16, cfg.ColorHeader));
                var resumen = resultado.Resumen;
                var filasResumen = new List<string[]>
            {
                new[] { "Días analizados", resumen.TotalDias.ToString() },
                new[] { "Ausencias", resumen.Ausencias.ToString() },
                new[] { "Tardanzas severas", resumen.TardanzasSeveras.ToString() },
                new[] { "Tardanzas leves", resumen.TardanzasLeves.ToString() },
                new[] { "Excesos de almuerzo", resumen.ExcesosAlmuerzo.ToString() },
                new[] { "Salidas anticipadas", resumen.SalidasAnticipadas.ToString() },
                new[] { "Registros anómalos", resumen.RegistrosAnomalos.ToString() },
                new[] { "Justificaciones", resumen.Justificaciones.ToString() }
            };
                body.Append(CrearTabla(new[] { "Concepto", "Cantidad" }, filasResumen, cfg.ColorHeader, cfg.ColorOk));
            }

            // Secciones
            if (cfg.IncluirAusencias)
                AgregarSeccion(body, "AUSENCIAS", resultado.Dias.Where(d => d.Estado == "ausente"),
                    d => new[] { d.Fecha.ToString("dd/MM/yyyy"), $"{d.HoraProgramadaEntrada:hh\\:mm} - {d.HoraProgramadaSalida:hh\\:mm}", d.Justificacion ?? "" },
                    new[] { "Fecha", "Horario programado", "Justificación" }, cfg);

            if (cfg.IncluirTardanzas)
                AgregarSeccion(body, "TARDANZAS", resultado.Dias.Where(d => d.Estado is "leve" or "severa"),
                    d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.HoraProgramadaEntrada?.ToString("hh\\:mm") ?? "", d.Llegada?.ToString("hh\\:mm") ?? "", $"{d.RetrasoMinutos} min" },
                    new[] { "Fecha", "Programado", "Llegada", "Retraso" }, cfg);

            if (cfg.IncluirExcesosAlmuerzo)
                AgregarSeccion(body, "EXCESOS DE ALMUERZO", resultado.Dias.Where(d => d.AlmuerzoExcesoMinutos > 0),
                    d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.AlmuerzoSalida?.ToString("hh\\:mm") ?? "", d.AlmuerzoRegreso?.ToString("hh\\:mm") ?? "", $"{d.AlmuerzoDuracionMinutos} min", $"{d.AlmuerzoExcesoMinutos} min" },
                    new[] { "Fecha", "Salida", "Regreso", "Duración", "Exceso" }, cfg);

            if (cfg.IncluirSalidasAnticipadas)
                AgregarSeccion(body, "SALIDAS ANTICIPADAS", resultado.Dias.Where(d => d.SalidaAnticipadaMinutos > 0),
                    d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.Salida?.ToString("hh\\:mm") ?? "", d.HoraProgramadaSalida?.ToString("hh\\:mm") ?? "", $"{d.SalidaAnticipadaMinutos} min" },
                    new[] { "Fecha", "Salida real", "Programada", "Adelanto" }, cfg);

            if (cfg.IncluirRegistrosAnomalos)
                AgregarSeccion(body, "REGISTROS ANÓMALOS", resultado.Dias.Where(d => d.Estado == "anomalo"),
                    d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.Registros.Count.ToString(), string.Join("; ", d.Observaciones) },
                    new[] { "Fecha", "# Reg.", "Detalle" }, cfg);

            // Detalle cronológico
            body.Append(Parrafo("DETALLE CRONOLÓGICO", true, 16, cfg.ColorHeader));
            var filasDetalle = resultado.Dias
                .Where(d => !d.EsDiaExcluido)
                .Select(d => new[]
                {
                d.Fecha.ToString("dd/MM/yyyy"),
                d.Llegada?.ToString("hh\\:mm") ?? "-",
                d.Salida?.ToString("hh\\:mm") ?? "-",
                d.Estado.ToUpperInvariant(),
                $"{d.TiempoNetoMinutos / 60}h {d.TiempoNetoMinutos % 60}m",
                string.Join("; ", d.Observaciones)
                }).ToList();
            body.Append(CrearTabla(
                new[] { "Fecha", "Entrada", "Salida", "Estado", "Neto", "Observaciones" },
                filasDetalle, cfg.ColorHeader, cfg.ColorOk));

            // Footer disclaimer
            body.Append(new Paragraph());
            body.Append(Parrafo(cfg.Disclaimer, false, 9, "808080", JustificationValues.Center));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }
        stream.Position = 0;
        return stream.ToArray();
    }

    private static Paragraph Parrafo(string texto, bool negrita, int tamaño, string? color, JustificationValues? alineacion = null)
    {
        var run = new Run();
        var props = new RunProperties { Bold = negrita ? new Bold() : null, FontSize = new FontSize { Val = (tamaño * 2).ToString() } };
        if (color != null)
            props.Color = new Color { Val = color.Replace("#", "") };
        run.Append(props);
        run.Append(new Text(texto) { Space = SpaceProcessingModeValues.Preserve });

        var para = new Paragraph(run);
        if (alineacion.HasValue)
        {
            var pProps = new ParagraphProperties();
            pProps.Justification = new Justification { Val = alineacion.Value };
            para.PrependChild(pProps);
        }
        return para;
    }

    private static Table CrearTabla(string[] encabezados, List<string[]> filas, string colorHeader, string colorFondo)
    {
        var table = new Table();
        table.Append(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }
            )
        ));

        var headerRow = new TableRow();
        foreach (var h in encabezados)
        {
            headerRow.Append(Celda(h, colorHeader, true));
        }
        table.Append(headerRow);

        foreach (var fila in filas)
        {
            var row = new TableRow();
            foreach (var celda in fila)
            {
                row.Append(Celda(celda, colorFondo, false));
            }
            table.Append(row);
        }

        return table;
    }

    private static TableCell Celda(string texto, string fondo, bool blanco)
    {
        var cell = new TableCell();
        cell.Append(new TableCellProperties(
            new Shading { Val = ShadingPatternValues.Clear, Fill = fondo.Replace("#", "") },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        ));
        var para = new Paragraph();
        var run = new Run();
        var props = new RunProperties
        {
            Bold = blanco ? new Bold() : null,
            Color = blanco ? new Color { Val = "FFFFFF" } : null
        };
        run.Append(props);
        run.Append(new Text(texto) { Space = SpaceProcessingModeValues.Preserve });
        para.Append(run);
        cell.Append(para);
        return cell;
    }

    private static void AgregarSeccion(Body body, string titulo, IEnumerable<DiaAnalizado> dias, Func<DiaAnalizado, string[]> filasFn, string[] encabezados, ConfiguracionReporte cfg)
    {
        var lista = dias.ToList();
        if (lista.Count == 0) return;

        body.Append(new Paragraph());
        body.Append(Parrafo(titulo, true, 14, cfg.ColorHeader));
        body.Append(CrearTabla(encabezados, lista.Select(filasFn).ToList(), cfg.ColorHeader, cfg.ColorOk));
    }
}
