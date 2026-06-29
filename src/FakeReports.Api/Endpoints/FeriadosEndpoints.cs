using System.Text.Json;
using FakeReports.Api.Modelos;

namespace FakeReports.Api.Endpoints;

public static class FeriadosEndpoints
{
    public static IEndpointRouteBuilder MapFeriadosEndpoints(this IEndpointRouteBuilder app)
    {
        // Se obtiene del DI para poder controlar la ruta en tests
        // (ContentRoot configurable vía WebApplicationFactory).
        var env = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var rutaArchivo = Path.Combine(env.ContentRootPath, "Feriados", "feriados.json");

        var group = app.MapGroup("/api/feriados");

        group.MapGet("/", () =>
        {
            try
            {
                var feriados = CargarFeriados(rutaArchivo);
                return Results.Ok(feriados);
            }
            catch (Exception ex)
            {
                return Results.Problem($"No se pudieron leer los feriados: {ex.Message}");
            }
        });

        group.MapPut("/", (List<Feriado> feriados) =>
        {
            try
            {
                GuardarFeriados(feriados, rutaArchivo);
                return Results.Ok(feriados);
            }
            catch (Exception ex)
            {
                return Results.Problem($"No se pudieron guardar los feriados: {ex.Message}");
            }
        });

        return app;
    }

    private static List<Feriado> CargarFeriados(string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
            return new List<Feriado>();

        var json = File.ReadAllText(rutaArchivo);
        return JsonSerializer.Deserialize<List<Feriado>>(json, OpcionesJson()) ?? new List<Feriado>();
    }

    private static void GuardarFeriados(List<Feriado> feriados, string rutaArchivo)
    {
        var normalizados = feriados
            .GroupBy(f => f.Fecha.Date)
            .Select(g => g.First())
            .OrderBy(f => f.Fecha)
            .ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivo)!);
        File.WriteAllText(rutaArchivo, JsonSerializer.Serialize(normalizados, OpcionesJson()));
    }

    private static JsonSerializerOptions OpcionesJson() => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
}
