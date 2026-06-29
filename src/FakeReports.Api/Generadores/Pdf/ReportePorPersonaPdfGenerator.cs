using FakeReports.Api.Analisis;
using FakeReports.Api.Modelos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FakeReports.Api.Generadores.Pdf;

public static class ReportePorPersonaPdfGenerator
{
    public static byte[] Generar(ResultadoAnalisis resultado)
    {
        var cfg = resultado.Configuracion;
        var c = new Colores(cfg);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(cfg.Titulo).Bold().FontSize(16).FontColor(c.Header);
                        col.Item().Text($"{cfg.Subtitulo} · {cfg.Institucion}").FontSize(11).FontColor(c.SubHeader);
                    });
                    row.AutoItem().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span(cfg.Disclaimer).FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" · Página ");
                    x.CurrentPageNumber();
                });

                page.Content().Column(col =>
                {
                    // Portada
                    col.Item().Height(2, Unit.Centimetre);
                    col.Item().AlignCenter().Text("REPORTE POR PERSONA").Bold().FontSize(22).FontColor(c.Header);
                    col.Item().AlignCenter().Text(resultado.Persona.Nombre).FontSize(14);
                    col.Item().AlignCenter().Text($"Identificación: {resultado.Persona.Identificacion}").FontSize(11);
                    col.Item().AlignCenter().Text($"Período: {resultado.FechaDesde:dd/MM/yyyy} - {resultado.FechaHasta:dd/MM/yyyy}").FontSize(11);
                    col.Item().Height(1, Unit.Centimetre);
                    col.Item().LineHorizontal(2).LineColor(c.Header);
                    col.Item().PageBreak();

                    // Resumen
                    if (cfg.IncluirResumen)
                    {
                        col.Item().Text("RESUMEN GENERAL").Bold().FontSize(14).FontColor(c.Header);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Días analizados").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.TotalDias.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Ausencias").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.Ausencias.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Tard. severas").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.TardanzasSeveras.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Tard. leves").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.TardanzasLeves.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Exc. almuerzo").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.ExcesosAlmuerzo.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Sal. anticipadas").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.SalidasAnticipadas.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Anómalos").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.RegistrosAnomalos.ToString());
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text("Justificaciones").SemiBold();
                            table.Cell().ColumnSpan(2).Element(e => c.CellStyle(e, c.Ok)).Text(resultado.Resumen.Justificaciones.ToString());
                        });
                        col.Item().Height(0.5f, Unit.Centimetre);
                    }

                    // Secciones detalladas
                    if (cfg.IncluirAusencias)
                        AgregarTablaDias(col, "AUSENCIAS", resultado.Dias.Where(d => d.Estado == "ausente"), c,
                            d => new[] { d.Fecha.ToString("dd/MM/yyyy"), $"{d.HoraProgramadaEntrada:hh\\:mm} - {d.HoraProgramadaSalida:hh\\:mm}", d.Justificacion ?? "" },
                            new[] { "Fecha", "Horario programado", "Justificación" });

                    if (cfg.IncluirTardanzas)
                        AgregarTablaDias(col, "TARDANZAS", resultado.Dias.Where(d => d.Estado is "leve" or "severa"), c,
                            d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.HoraProgramadaEntrada?.ToString("hh\\:mm") ?? "", d.Llegada?.ToString("hh\\:mm") ?? "", $"{d.RetrasoMinutos} min" },
                            new[] { "Fecha", "Programado", "Llegada", "Retraso" });

                    if (cfg.IncluirExcesosAlmuerzo)
                        AgregarTablaDias(col, "EXCESOS DE ALMUERZO", resultado.Dias.Where(d => d.AlmuerzoExcesoMinutos > 0), c,
                            d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.AlmuerzoSalida?.ToString("hh\\:mm") ?? "", d.AlmuerzoRegreso?.ToString("hh\\:mm") ?? "", $"{d.AlmuerzoDuracionMinutos} min", $"{d.AlmuerzoExcesoMinutos} min" },
                            new[] { "Fecha", "Salida", "Regreso", "Duración", "Exceso" });

                    if (cfg.IncluirSalidasAnticipadas)
                        AgregarTablaDias(col, "SALIDAS ANTICIPADAS", resultado.Dias.Where(d => d.SalidaAnticipadaMinutos > 0), c,
                            d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.Salida?.ToString("hh\\:mm") ?? "", d.HoraProgramadaSalida?.ToString("hh\\:mm") ?? "", $"{d.SalidaAnticipadaMinutos} min" },
                            new[] { "Fecha", "Salida real", "Programada", "Adelanto" });

                    if (cfg.IncluirRegistrosAnomalos)
                        AgregarTablaDias(col, "REGISTROS ANÓMALOS", resultado.Dias.Where(d => d.Estado == "anomalo"), c,
                            d => new[] { d.Fecha.ToString("dd/MM/yyyy"), d.Registros.Count.ToString(), string.Join(", ", d.Observaciones) },
                            new[] { "Fecha", "# Reg.", "Detalle" });

                    // Detalle cronológico
                    col.Item().Text("DETALLE CRONOLÓGICO").Bold().FontSize(14).FontColor(c.Header);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(c.HeaderStyle).Text("Fecha").Bold();
                            header.Cell().Element(c.HeaderStyle).Text("Entrada");
                            header.Cell().Element(c.HeaderStyle).Text("Salida");
                            header.Cell().Element(c.HeaderStyle).Text("Estado");
                            header.Cell().Element(c.HeaderStyle).Text("Neto");
                            header.Cell().Element(c.HeaderStyle).Text("Observaciones");
                        });
                        foreach (var dia in resultado.Dias.Where(d => !d.EsDiaExcluido))
                        {
                            var bg = dia.Estado switch
                            {
                                "ausente" or "severa" or "anomalo" => c.Error,
                                "leve" or "salida_anticipada" => c.Warn,
                                _ => "#FFFFFF"
                            };
                            table.Cell().Element(e => c.CellStyle(e, bg)).Text(dia.Fecha.ToString("dd/MM/yyyy"));
                            table.Cell().Element(e => c.CellStyle(e, bg)).Text(dia.Llegada?.ToString("hh\\:mm") ?? "-");
                            table.Cell().Element(e => c.CellStyle(e, bg)).Text(dia.Salida?.ToString("hh\\:mm") ?? "-");
                            table.Cell().Element(e => c.CellStyle(e, bg)).Text(dia.Estado.ToUpperInvariant());
                            table.Cell().Element(e => c.CellStyle(e, bg)).Text($"{dia.TiempoNetoMinutos / 60}h {dia.TiempoNetoMinutos % 60}m");
                            table.Cell().Element(e => c.CellStyle(e, bg)).Text(string.Join("; ", dia.Observaciones));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void AgregarTablaDias(ColumnDescriptor col, string titulo, IEnumerable<DiaAnalizado> dias, Colores c, Func<DiaAnalizado, string[]> filas, string[] encabezados)
    {
        var lista = dias.ToList();
        if (lista.Count == 0) return;

        col.Item().Text(titulo).Bold().FontSize(13).FontColor(c.Header);
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in encabezados)
                    columns.RelativeColumn();
            });
            table.Header(header =>
            {
                foreach (var h in encabezados)
                    header.Cell().Element(c.HeaderStyle).Text(h).Bold();
            });
            foreach (var dia in lista)
            {
                var row = filas(dia);
                foreach (var celda in row)
                    table.Cell().Element(e => c.CellStyle(e, Colors.White)).Text(celda);
            }
        });
        col.Item().Height(0.5f, Unit.Centimetre);
    }

    private class Colores
    {
        public string Header { get; }
        public string SubHeader { get; }
        public string Ok { get; }
        public string Warn { get; }
        public string Error { get; }

        public Colores(ConfiguracionReporte cfg)
        {
            Header = cfg.ColorHeader;
            SubHeader = "#2e6da4";
            Ok = cfg.ColorOk;
            Warn = cfg.ColorWarn;
            Error = cfg.ColorError;
        }

        public IContainer HeaderStyle(IContainer container) =>
            container.Background(Header).Padding(4).Border(0.5f).BorderColor(Colors.Grey.Medium);

        public IContainer CellStyle(IContainer container, string background) =>
            container.Background(background).Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1);
    }
}
