using System.Net;
using System.Net.Http.Json;
using FakeReports.Api.Modelos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FakeReports.Api.Tests;

/// <summary>
/// Tests de integración de FeriadosEndpoints (Feature 1).
///
/// Cada test crea su propio WebApplicationFactory con un directorio temporal
/// aislado para no contaminar `src/FakeReports.Api/Feriados/feriados.json`.
/// El endpoint usa `IWebHostEnvironment.ContentRootPath` para resolver la ruta.
/// </summary>
public class FeriadosEndpointsTests
{
    private static (HttpClient Client, string ContentRoot) CrearClienteConFeriados(IEnumerable<Feriado> feriadosIniciales)
    {
        var dir = Path.Combine(Path.GetTempPath(), "FakeReports.Tests.Feriados_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "Feriados"));

        var opts = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        File.WriteAllText(
            Path.Combine(dir, "Feriados", "feriados.json"),
            System.Text.Json.JsonSerializer.Serialize(feriadosIniciales, opts));

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseContentRoot(dir);
            b.UseEnvironment("Development");
        });

        return (factory.CreateClient(), dir);
    }

    private static (HttpClient Client, string ContentRoot) CrearClienteVacio()
        => CrearClienteConFeriados(Array.Empty<Feriado>());

    [Fact]
    public async Task Get_Con_Archivo_Vacio_Devuelve_Lista_Vacia()
    {
        var (client, dir) = CrearClienteVacio();
        try
        {
            var feriados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(feriados);
            Assert.Empty(feriados!);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Get_Con_Archivo_Poblado_Devuelve_Feriados_Existentes()
    {
        var semilla = new List<Feriado>
        {
            new() { Fecha = new DateTime(2026, 1, 1), Descripcion = "Año Nuevo" },
            new() { Fecha = new DateTime(2026, 5, 1), Descripcion = "Día del Trabajo" }
        };
        var (client, dir) = CrearClienteConFeriados(semilla);
        try
        {
            var feriados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(feriados);
            Assert.Equal(2, feriados!.Count);
            Assert.Contains(feriados, f => f.Descripcion == "Año Nuevo");
            Assert.Contains(feriados, f => f.Descripcion == "Día del Trabajo");
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Put_Reemplaza_Lista_Y_Se_Persiste()
    {
        var (client, dir) = CrearClienteVacio();
        try
        {
            var nuevaLista = new List<Feriado>
            {
                new() { Fecha = new DateTime(2026, 11, 2), Descripcion = "Muertos" },
                new() { Fecha = new DateTime(2026, 12, 25), Descripcion = "Navidad" }
            };

            var putResp = await client.PutAsJsonAsync("/api/feriados", nuevaLista);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

            var guardados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(guardados);
            Assert.Equal(2, guardados!.Count);
            Assert.Equal("Muertos", guardados[0].Descripcion); // ordenada ascendente
            Assert.Equal("Navidad", guardados[1].Descripcion);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Put_Con_Lista_Vacia_Deja_El_Archivo_Vacio()
    {
        var semilla = new List<Feriado>
        {
            new() { Fecha = new DateTime(2026, 5, 1), Descripcion = "Trabajo" }
        };
        var (client, dir) = CrearClienteConFeriados(semilla);
        try
        {
            var putResp = await client.PutAsJsonAsync("/api/feriados", new List<Feriado>());
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

            var guardados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(guardados);
            Assert.Empty(guardados!);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Put_Deduplica_Por_Fecha_Y_Ordena_Ascendente()
    {
        var (client, dir) = CrearClienteVacio();
        try
        {
            var lista = new List<Feriado>
            {
                new() { Fecha = new DateTime(2026, 7, 28), Descripcion = "Independencia" },
                new() { Fecha = new DateTime(2026, 5, 1), Descripcion = "Trabajo" },
                new() { Fecha = new DateTime(2026, 7, 28), Descripcion = "Duplicado mismo día" },
                new() { Fecha = new DateTime(2026, 1, 1), Descripcion = "Año Nuevo" }
            };

            var putResp = await client.PutAsJsonAsync("/api/feriados", lista);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

            var guardados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(guardados);
            Assert.Equal(3, guardados!.Count);
            Assert.Equal(new DateTime(2026, 1, 1), guardados[0].Fecha);
            Assert.Equal(new DateTime(2026, 5, 1), guardados[1].Fecha);
            Assert.Equal(new DateTime(2026, 7, 28), guardados[2].Fecha);
            // El primero en aparecer con esa fecha sobrevive
            Assert.Equal("Independencia", guardados[2].Descripcion);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Put_Deduplica_Por_Fecha_Cuando_Hay_Duplicados()
    {
        // Cuando hay dos feriados con la misma fecha (con o sin hora),
        // `GuardarFeriados` los deduplica por `Fecha.Date` y conserva el primero
        // con su hora original. Este test documenta ese comportamiento.
        var (client, dir) = CrearClienteVacio();
        try
        {
            var lista = new List<Feriado>
            {
                new() { Fecha = new DateTime(2026, 7, 28, 10, 30, 0), Descripcion = "Mañana" },
                new() { Fecha = new DateTime(2026, 7, 28, 22, 15, 0), Descripcion = "Noche" }
            };

            var putResp = await client.PutAsJsonAsync("/api/feriados", lista);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

            var guardados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(guardados);
            Assert.Single(guardados!);
            // Se conserva el primero (Mañana) tal cual, con su hora original
            Assert.Equal("Mañana", guardados[0].Descripcion);
            Assert.Equal(2026, guardados[0].Fecha.Year);
            Assert.Equal(7, guardados[0].Fecha.Month);
            Assert.Equal(28, guardados[0].Fecha.Day);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Put_Retorna_Lo_Que_Recibio_No_Normaliza_En_Respuesta()
    {
        // El endpoint hace `return Results.Ok(feriados)` con la lista de entrada.
        // La normalización/orden/dedupe solo se aplica al guardar en disco.
        var (client, dir) = CrearClienteVacio();
        try
        {
            var entrada = new List<Feriado>
            {
                new() { Fecha = new DateTime(2026, 12, 31), Descripcion = "Fin de año" },
                new() { Fecha = new DateTime(2026, 1, 1), Descripcion = "Año Nuevo" }
            };

            var respuesta = await client.PutAsJsonAsync("/api/feriados", entrada);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

            var devuelto = await respuesta.Content.ReadFromJsonAsync<List<Feriado>>();
            Assert.NotNull(devuelto);
            Assert.Equal(2, devuelto!.Count);
            // Se devuelve en el orden original
            Assert.Equal(new DateTime(2026, 12, 31), devuelto[0].Fecha);
            Assert.Equal(new DateTime(2026, 1, 1), devuelto[1].Fecha);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Put_Ordena_En_Disco_Aunque_La_Entrada_Venga_Desordenada()
    {
        var (client, dir) = CrearClienteVacio();
        try
        {
            var entrada = new List<Feriado>
            {
                new() { Fecha = new DateTime(2026, 12, 31), Descripcion = "Fin de año" },
                new() { Fecha = new DateTime(2026, 1, 1), Descripcion = "Año Nuevo" }
            };

            await client.PutAsJsonAsync("/api/feriados", entrada);

            var guardados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(guardados);
            Assert.Equal(2, guardados!.Count);
            Assert.Equal(new DateTime(2026, 1, 1), guardados[0].Fecha);
            Assert.Equal(new DateTime(2026, 12, 31), guardados[1].Fecha);
        }
        finally { Limpiar(dir); }
    }

    [Fact]
    public async Task Archivo_Creado_Al_Primer_Put_Cuando_No_Existia()
    {
        var (client, dir) = CrearClienteVacio();
        try
        {
            // Borramos el archivo para asegurar el caso "archivo no existe al inicio"
            File.Delete(Path.Combine(dir, "Feriados", "feriados.json"));

            var lista = new List<Feriado>
            {
                new() { Fecha = new DateTime(2026, 7, 28), Descripcion = "Independencia" }
            };

            var putResp = await client.PutAsJsonAsync("/api/feriados", lista);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

            // Ahora GET debe devolver la lista que acabamos de escribir
            var guardados = await client.GetFromJsonAsync<List<Feriado>>("/api/feriados");
            Assert.NotNull(guardados);
            Assert.Single(guardados!);
            Assert.Equal("Independencia", guardados[0].Descripcion);
        }
        finally { Limpiar(dir); }
    }

    private static void Limpiar(string dir)
    {
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* swallow */ }
        }
    }
}
