using System.Net;
using System.Net.Http.Json;
using FakeReports.Api.Modelos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FakeReports.Api.Tests;

/// <summary>
/// Tests de integración de ReportesEndpoints (Feature 2 — Vista previa del reporte).
///
/// Verifica los 3 endpoints del grupo:
///   - POST /api/reportes/persona/preview  → JSON
///   - POST /api/reportes/persona/pdf     → application/pdf (binario)
///   - POST /api/reportes/persona/word    → docx (zip)
/// </summary>
public class ReportesEndpointsTests
{
    private static readonly DateTime Fecha = new(2026, 6, 1); // lunes

    private static WebApplicationFactory<Program> CrearFactory()
    {
        // El content root temporal no es necesario para estos endpoints
        // (no tocan disco), pero configuramos un directorio dedicado para
        // mantener aisladas las pruebas.
        var dir = Path.Combine(Path.GetTempPath(), "FakeReports.Tests.Reportes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseContentRoot(dir);
            b.UseEnvironment("Development");
        });
    }

    private static ReporteRequest CrearRequestBase()
    {
        return new ReporteRequest
        {
            Persona = new Persona { Id = Guid.NewGuid(), Nombre = "Juan Pérez", Identificacion = "12345678" },
            Horario = new HorarioSemanal
            {
                Id = Guid.NewGuid(),
                Nombre = "Estándar",
                AlmuerzoMinutos = 60,
                Lunes = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Martes = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Miercoles = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Jueves = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Viernes = new HorarioDia { Entrada = "08:00", Salida = "17:00" },
                Sabado = new HorarioDia(),
                Domingo = new HorarioDia()
            },
            Configuracion = new ConfiguracionReporte(),
            Registros = new List<RegistroAsistencia>
            {
                new() { FechaHora = Fecha.AddHours(8), Tipo = "entrada" },
                new() { FechaHora = Fecha.AddHours(17), Tipo = "salida" }
            },
            FechaDesde = Fecha,
            FechaHasta = Fecha
        };
    }

    // ---------- /preview ----------

    [Fact]
    public async Task Preview_Devuelve_200_Y_ResultadoAnalisis_Valido()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var request = CrearRequestBase();

        var response = await client.PostAsJsonAsync("/api/reportes/persona/preview", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<ResultadoAnalisis>();
        Assert.NotNull(resultado);
        Assert.Equal(request.Persona.Nombre, resultado!.Persona.Nombre);
        Assert.Equal(request.Persona.Identificacion, resultado.Persona.Identificacion);
        Assert.Single(resultado.Dias);
        Assert.Equal("ok", resultado.Dias[0].Estado);
        Assert.Equal(1, resultado.Resumen.TotalDias);
        Assert.Equal(0, resultado.Resumen.Ausencias);
    }

    [Fact]
    public async Task Preview_Incluye_Feriados_Y_Excluidos_En_El_Resultado()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var request = CrearRequestBase();
        request.Feriados = new List<DateTime> { Fecha };
        request.DiasExcluidos = new List<DateTime> { Fecha.AddDays(2) };
        request.FechaHasta = Fecha.AddDays(2);

        var resultado = await client
            .PostAsJsonAsync("/api/reportes/persona/preview", request)
            .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<ResultadoAnalisis>().Result);

        Assert.NotNull(resultado);
        Assert.Equal(3, resultado!.Dias.Count);
        Assert.True(resultado.Dias[0].EsFeriado);
        Assert.Equal("feriado", resultado.Dias[0].Estado);
        Assert.True(resultado.Dias[2].EsDiaExcluido);
        Assert.Equal("excluido", resultado.Dias[2].Estado);
    }

    [Fact]
    public async Task Preview_Preserva_Fechas_Y_Configuracion_Del_Request()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var request = CrearRequestBase();
        request.Configuracion.Titulo = "TITULO_CUSTOM";
        request.FechaDesde = Fecha;
        request.FechaHasta = Fecha.AddDays(7);

        var resultado = await client
            .PostAsJsonAsync("/api/reportes/persona/preview", request)
            .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<ResultadoAnalisis>().Result);

        Assert.NotNull(resultado);
        Assert.Equal("TITULO_CUSTOM", resultado!.Configuracion.Titulo);
        Assert.Equal(Fecha, resultado.FechaDesde.Date);
        Assert.Equal(Fecha.AddDays(7), resultado.FechaHasta.Date);
    }

    // ---------- /pdf ----------

    [Fact]
    public async Task Pdf_Devuelve_200_Y_Bytes_Con_Header_PDF()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reportes/persona/pdf", CrearRequestBase());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        // Magic bytes de un PDF
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task Pdf_Tiene_Content_Disposition_Con_Nombre_De_Archivo()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reportes/persona/pdf", CrearRequestBase());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentDisposition);
        Assert.NotNull(response.Content.Headers.ContentDisposition!.FileName);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition.FileName!.Trim('"'));
    }

    // ---------- /word ----------

    [Fact]
    public async Task Word_Devuelve_200_Y_Bytes_Con_Header_PK_Docx()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reportes/persona/word", CrearRequestBase());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        // DOCX es un ZIP → magic bytes 'PK'
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public async Task Word_Tiene_Content_Disposition_Con_Nombre_De_Archivo()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reportes/persona/word", CrearRequestBase());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentDisposition);
        Assert.NotNull(response.Content.Headers.ContentDisposition!.FileName);
        Assert.EndsWith(".docx", response.Content.Headers.ContentDisposition.FileName!.Trim('"'));
    }

    [Fact]
    public async Task Nombre_De_Archivo_Sanitiza_Caracteres_Especiales()
    {
        using var factory = CrearFactory();
        using var client = factory.CreateClient();

        var request = CrearRequestBase();
        request.Persona.Nombre = "Juan/ Pérez\\Con:Saltos";

        var response = await client.PostAsJsonAsync("/api/reportes/persona/pdf", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fileName = response.Content.Headers.ContentDisposition!.FileName!.Trim('"');
        // No debe contener ninguno de los caracteres problemáticos
        Assert.DoesNotContain("/", fileName);
        Assert.DoesNotContain("\\", fileName);
        Assert.DoesNotContain(":", fileName);
    }
}
