import { TestBed } from '@angular/core/testing';
import { StorageService } from './storage.service';
import { AppState, ConfiguracionReporte } from '../models/reporte.model';
import { instalarLocalStorageMock } from '../testing/local-storage-mock';

describe('StorageService', () => {
  let service: StorageService;

  beforeEach(() => {
    instalarLocalStorageMock();
    TestBed.configureTestingModule({});
    service = TestBed.inject(StorageService);
  });

  it('se crea', () => {
    expect(service).toBeTruthy();
  });

  describe('load', () => {
    it('devuelve el estado por defecto cuando no hay nada en localStorage', () => {
      const state = service.load();

      expect(state.persona.nombre).toBe('Juan Pérez');
      expect(state.persona.identificacion).toBe('1234567890');
      expect(state.horario.nombre).toBe('Horario estándar');
      expect(state.horario.almuerzoMinutos).toBe(60);
      expect(state.horario.lunes.entrada).toBe('08:00');
      expect(state.horario.lunes.salida).toBe('17:00');
      expect(state.registros).toEqual([]);
      expect(state.diasExcluidos).toEqual([]);
      expect(state.justificaciones).toEqual([]);
    });

    it('devuelve el estado por defecto cuando el JSON guardado es inválido', () => {
      localStorage.setItem('fakeReportsState', '{ no es json valido');
      const state = service.load();
      expect(state.persona.nombre).toBe('Juan Pérez');
    });

    it('fusiona el estado guardado con el default (no rompe si faltan claves)', () => {
      const parcial: Partial<AppState> = {
        persona: { id: 'X', nombre: 'Ana', identificacion: '999' }
      };
      localStorage.setItem('fakeReportsState', JSON.stringify(parcial));

      const state = service.load();

      // Persona del JSON
      expect(state.persona.nombre).toBe('Ana');
      // Defaults para campos no presentes
      expect(state.horario.almuerzoMinutos).toBe(60);
      expect(state.registros).toEqual([]);
    });

    it('recupera exactamente lo que se guardó', () => {
      const config: ConfiguracionReporte = {
        titulo: 'TITULO X',
        subtitulo: 'S',
        institucion: 'I',
        disclaimer: 'D',
        colorHeader: '#000000',
        colorOk: '#aaaaaa',
        colorWarn: '#bbbbbb',
        colorError: '#cccccc',
        umbralTardanzaLeveMin: 5,
        umbralTardanzaSeveraMin: 15,
        umbralExcesoAlmuerzoMin: 10,
        incluirResumen: false,
        incluirTardanzas: false,
        incluirAusencias: true,
        incluirExcesosAlmuerzo: true,
        incluirSalidasAnticipadas: false,
        incluirRegistrosAnomalos: true,
        incluirCumplimientoHoras: false
      };
      const custom: AppState = {
        persona: { id: 'P1', nombre: 'María', identificacion: '555' },
        horario: {
          id: 'H1', nombre: 'Rotativo',
          lunes: { entrada: '09:00', salida: '18:00' },
          martes: {}, miercoles: {}, jueves: {}, viernes: {}, sabado: {}, domingo: {},
          almuerzoMinutos: 45
        },
        registros: [{ fechaHora: '2026-06-01T08:00', tipo: 'entrada' }],
        configuracion: config,
        diasExcluidos: ['2026-06-02'],
        justificaciones: [{ fecha: '2026-06-01', tipo: 'vacaciones', descripcion: 'Personal' }],
        fechaDesde: '2026-06-01',
        fechaHasta: '2026-06-30'
      };

      service.save(custom);
      const loaded = service.load();

      expect(loaded.persona.nombre).toBe('María');
      expect(loaded.horario.nombre).toBe('Rotativo');
      expect(loaded.registros).toHaveLength(1);
      expect(loaded.registros[0].tipo).toBe('entrada');
      expect(loaded.configuracion.titulo).toBe('TITULO X');
      expect(loaded.configuracion.umbralTardanzaLeveMin).toBe(5);
      expect(loaded.diasExcluidos).toEqual(['2026-06-02']);
      expect(loaded.justificaciones[0].descripcion).toBe('Personal');
      expect(loaded.fechaDesde).toBe('2026-06-01');
    });
  });

  describe('save', () => {
    it('persiste el estado en localStorage', () => {
      const state = service.load();
      state.persona.nombre = 'Modificado';
      service.save(state);

      const raw = localStorage.getItem('fakeReportsState');
      expect(raw).toBeTruthy();
      const parsed = JSON.parse(raw!);
      expect(parsed.persona.nombre).toBe('Modificado');
    });
  });
});