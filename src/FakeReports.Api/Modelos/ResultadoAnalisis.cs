namespace FakeReports.Api.Modelos;

public class DiaAnalizado
{
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "ok";
    public TimeSpan? Llegada { get; set; }
    public TimeSpan? Salida { get; set; }
    public TimeSpan? HoraProgramadaEntrada { get; set; }
    public TimeSpan? HoraProgramadaSalida { get; set; }
    public int RetrasoMinutos { get; set; }
    public TimeSpan? AlmuerzoSalida { get; set; }
    public TimeSpan? AlmuerzoRegreso { get; set; }
    public int AlmuerzoDuracionMinutos { get; set; }
    public int AlmuerzoExcesoMinutos { get; set; }
    public int SalidaAnticipadaMinutos { get; set; }
    public int TiempoNetoMinutos { get; set; }
    public List<string> Observaciones { get; set; } = new();
    public List<RegistroAsistencia> Registros { get; set; } = new();
    public bool EsFeriado { get; set; }
    public bool EsDiaLibre { get; set; }
    public bool EsDiaExcluido { get; set; }
    public string? Justificacion { get; set; }
}

public class ResumenReporte
{
    public int TotalDias { get; set; }
    public int Ausencias { get; set; }
    public int TardanzasSeveras { get; set; }
    public int TardanzasLeves { get; set; }
    public int ExcesosAlmuerzo { get; set; }
    public int SalidasAnticipadas { get; set; }
    public int RegistrosAnomalos { get; set; }
    public int Justificaciones { get; set; }
}

public class ResultadoAnalisis
{
    public Persona Persona { get; set; } = new();
    public ResumenReporte Resumen { get; set; } = new();
    public List<DiaAnalizado> Dias { get; set; } = new();
    public ConfiguracionReporte Configuracion { get; set; } = new();
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
}
