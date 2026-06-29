export interface HorarioDia {
  entrada?: string; // HH:MM
  salida?: string;  // HH:MM
}

export interface HorarioSemanal {
  id: string;
  nombre: string;
  lunes: HorarioDia;
  martes: HorarioDia;
  miercoles: HorarioDia;
  jueves: HorarioDia;
  viernes: HorarioDia;
  sabado: HorarioDia;
  domingo: HorarioDia;
  almuerzoMinutos: number;
}

export interface Persona {
  id: string;
  nombre: string;
  identificacion: string;
}

export interface RegistroAsistencia {
  fechaHora: string; // ISO
  tipo: 'entrada' | 'salida';
}

export interface Justificacion {
  fecha: string; // YYYY-MM-DD
  tipo: string;
  descripcion: string;
}

export interface ConfiguracionReporte {
  titulo: string;
  subtitulo: string;
  institucion: string;
  disclaimer: string;
  colorHeader: string;
  colorOk: string;
  colorWarn: string;
  colorError: string;
  umbralTardanzaLeveMin: number;
  umbralTardanzaSeveraMin: number;
  umbralExcesoAlmuerzoMin: number;
  incluirResumen: boolean;
  incluirTardanzas: boolean;
  incluirAusencias: boolean;
  incluirExcesosAlmuerzo: boolean;
  incluirSalidasAnticipadas: boolean;
  incluirRegistrosAnomalos: boolean;
  incluirCumplimientoHoras: boolean;
}

export interface Feriado {
  fecha: string;
  descripcion: string;
}

export interface ReporteRequest {
  persona: Persona;
  horario: HorarioSemanal;
  registros: RegistroAsistencia[];
  configuracion: ConfiguracionReporte;
  feriados: string[];
  diasExcluidos: string[];
  justificaciones: Justificacion[];
  fechaDesde: string;
  fechaHasta: string;
}

export interface AppState {
  persona: Persona;
  horario: HorarioSemanal;
  registros: RegistroAsistencia[];
  configuracion: ConfiguracionReporte;
  diasExcluidos: string[];
  justificaciones: Justificacion[];
  fechaDesde: string;
  fechaHasta: string;
}

export interface ResumenReporte {
  totalDias: number;
  ausencias: number;
  tardanzasSeveras: number;
  tardanzasLeves: number;
  excesosAlmuerzo: number;
  salidasAnticipadas: number;
  registrosAnomalos: number;
  justificaciones: number;
}

export interface DiaAnalizado {
  fecha: string;
  estado: string;
  llegada: string | null;
  salida: string | null;
  horaProgramadaEntrada: string | null;
  horaProgramadaSalida: string | null;
  retrasoMinutos: number;
  almuerzoSalida: string | null;
  almuerzoRegreso: string | null;
  almuerzoDuracionMinutos: number;
  almuerzoExcesoMinutos: number;
  salidaAnticipadaMinutos: number;
  tiempoNetoMinutos: number;
  observaciones: string[];
  registros: RegistroAsistencia[];
  esFeriado: boolean;
  esDiaLibre: boolean;
  esDiaExcluido: boolean;
  justificacion: string | null;
}

export interface ResultadoAnalisis {
  persona: Persona;
  resumen: ResumenReporte;
  dias: DiaAnalizado[];
  configuracion: ConfiguracionReporte;
  fechaDesde: string;
  fechaHasta: string;
}
