namespace FakeReports.Api.Modelos;

public class Persona
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
}

public class HorarioDia
{
    public string? Entrada { get; set; }
    public string? Salida { get; set; }
}

public class HorarioSemanal
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public HorarioDia Lunes { get; set; } = new();
    public HorarioDia Martes { get; set; } = new();
    public HorarioDia Miercoles { get; set; } = new();
    public HorarioDia Jueves { get; set; } = new();
    public HorarioDia Viernes { get; set; } = new();
    public HorarioDia Sabado { get; set; } = new();
    public HorarioDia Domingo { get; set; } = new();
    public int AlmuerzoMinutos { get; set; }
}

public class RegistroAsistencia
{
    public DateTime FechaHora { get; set; }
    public string Tipo { get; set; } = "entrada";
}

public class Justificacion
{
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class ConfiguracionReporte
{
    public string Titulo { get; set; } = "SISTEMA BIOMÉTRICO";
    public string Subtitulo { get; set; } = "Control Biométrico";
    public string Institucion { get; set; } = "INSTITUCIÓN";
    public string Disclaimer { get; set; } = "Documento de uso interno. No constituye acto administrativo.";
    public string ColorHeader { get; set; } = "#1a3a5c";
    public string ColorOk { get; set; } = "#d4edda";
    public string ColorWarn { get; set; } = "#fff3cd";
    public string ColorError { get; set; } = "#f8d7da";
    public int UmbralTardanzaLeveMin { get; set; } = 1;
    public int UmbralTardanzaSeveraMin { get; set; } = 6;
    public int UmbralExcesoAlmuerzoMin { get; set; } = 1;
    public bool IncluirResumen { get; set; } = true;
    public bool IncluirTardanzas { get; set; } = true;
    public bool IncluirAusencias { get; set; } = true;
    public bool IncluirExcesosAlmuerzo { get; set; } = true;
    public bool IncluirSalidasAnticipadas { get; set; } = true;
    public bool IncluirRegistrosAnomalos { get; set; } = true;
    public bool IncluirCumplimientoHoras { get; set; } = true;
}

public class ReporteRequest
{
    public Persona Persona { get; set; } = new();
    public HorarioSemanal Horario { get; set; } = new();
    public List<RegistroAsistencia> Registros { get; set; } = new();
    public ConfiguracionReporte Configuracion { get; set; } = new();
    public List<DateTime> Feriados { get; set; } = new();
    public List<DateTime> DiasExcluidos { get; set; } = new();
    public List<Justificacion> Justificaciones { get; set; } = new();
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
}

public class Feriado
{
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}
