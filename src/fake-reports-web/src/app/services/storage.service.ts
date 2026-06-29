import { Injectable } from '@angular/core';
import {
  AppState,
  ConfiguracionReporte,
  HorarioSemanal,
  Persona,
  RegistroAsistencia,
  Justificacion,
} from '../models/reporte.model';

const STORAGE_KEY = 'fakeReportsState';

const defaultConfig: ConfiguracionReporte = {
  titulo: 'SISTEMA BIOMÉTRICO',
  subtitulo: 'Control Biométrico',
  institucion: 'INSTITUCIÓN',
  disclaimer: 'Documento de uso interno. No constituye acto administrativo.',
  colorHeader: '#1a3a5c',
  colorOk: '#d4edda',
  colorWarn: '#fff3cd',
  colorError: '#f8d7da',
  umbralTardanzaLeveMin: 1,
  umbralTardanzaSeveraMin: 6,
  umbralExcesoAlmuerzoMin: 1,
  incluirResumen: true,
  incluirTardanzas: true,
  incluirAusencias: true,
  incluirExcesosAlmuerzo: true,
  incluirSalidasAnticipadas: true,
  incluirRegistrosAnomalos: true,
  incluirCumplimientoHoras: true,
};

const defaultHorario = (nombre: string): HorarioSemanal => ({
  id: crypto.randomUUID(),
  nombre,
  lunes: { entrada: '08:00', salida: '17:00' },
  martes: { entrada: '08:00', salida: '17:00' },
  miercoles: { entrada: '08:00', salida: '17:00' },
  jueves: { entrada: '08:00', salida: '17:00' },
  viernes: { entrada: '08:00', salida: '17:00' },
  sabado: {},
  domingo: {},
  almuerzoMinutos: 60,
});

const defaultState = (): AppState => ({
  persona: { id: crypto.randomUUID(), nombre: 'Juan Pérez', identificacion: '1234567890' },
  horario: defaultHorario('Horario estándar'),
  registros: [],
  configuracion: defaultConfig,
  diasExcluidos: [],
  justificaciones: [],
  fechaDesde: new Date().toISOString().slice(0, 10),
  fechaHasta: new Date().toISOString().slice(0, 10),
});

@Injectable({ providedIn: 'root' })
export class StorageService {
  load(): AppState {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return defaultState();
    try {
      return { ...defaultState(), ...JSON.parse(raw) };
    } catch {
      return defaultState();
    }
  }

  save(state: AppState): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }
}
