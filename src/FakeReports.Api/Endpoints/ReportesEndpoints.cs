using System.Text.RegularExpressions;
using FakeReports.Api.Analisis;
using FakeReports.Api.Generadores.Pdf;
using FakeReports.Api.Generadores.Word;
using FakeReports.Api.Modelos;

namespace FakeReports.Api.Endpoints;

public static class ReportesEndpoints
{
    public static IEndpointRouteBuilder MapReportesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reportes");

        group.MapPost("/persona/pdf", (ReporteRequest request) =>
        {
            var analisis = AnalizadorAsistencias.Analizar(request);
            var pdf = ReportePorPersonaPdfGenerator.Generar(analisis);
            var nombre = SanitizarNombreArchivo($"reporte_{request.Persona.Nombre}_{DateTime.Now:yyyyMMdd}");
            return Results.File(pdf, "application/pdf", $"{nombre}.pdf");
        });

        group.MapPost("/persona/word", (ReporteRequest request) =>
        {
            var analisis = AnalizadorAsistencias.Analizar(request);
            var word = ReportePorPersonaWordGenerator.Generar(analisis);
            var nombre = SanitizarNombreArchivo($"reporte_{request.Persona.Nombre}_{DateTime.Now:yyyyMMdd}");
            return Results.File(word, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"{nombre}.docx");
        });

        group.MapPost("/persona/preview", (ReporteRequest request) =>
        {
            var analisis = AnalizadorAsistencias.Analizar(request);
            return Results.Ok(analisis);
        });

        return app;
    }

    private static string SanitizarNombreArchivo(string nombre)
    {
        return Regex.Replace(nombre, @"[^\w\-]", "_");
    }
}
