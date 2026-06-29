import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ReportesApiService } from './reportes-api.service';
import { ReporteRequest, ResultadoAnalisis } from '../models/reporte.model';

describe('ReportesApiService', () => {
  let service: ReportesApiService;
  let httpMock: HttpTestingController;

  const requestBase: ReporteRequest = {
    persona: { id: 'p1', nombre: 'Juan', identificacion: '123' },
    horario: {
      id: 'h1', nombre: 'Estándar',
      lunes: { entrada: '08:00', salida: '17:00' },
      martes: {}, miercoles: {}, jueves: {}, viernes: {}, sabado: {}, domingo: {},
      almuerzoMinutos: 60
    },
    registros: [],
    configuracion: {
      titulo: 'T', subtitulo: 'S', institucion: 'I', disclaimer: 'D',
      colorHeader: '#000', colorOk: '#aaa', colorWarn: '#bbb', colorError: '#ccc',
      umbralTardanzaLeveMin: 1, umbralTardanzaSeveraMin: 6, umbralExcesoAlmuerzoMin: 1,
      incluirResumen: true, incluirTardanzas: true, incluirAusencias: true,
      incluirExcesosAlmuerzo: true, incluirSalidasAnticipadas: true,
      incluirRegistrosAnomalos: true, incluirCumplimientoHoras: true
    },
    feriados: [],
    diasExcluidos: [],
    justificaciones: [],
    fechaDesde: '2026-06-01',
    fechaHasta: '2026-06-30'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ReportesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('se crea', () => {
    expect(service).toBeTruthy();
  });

  describe('generarPdf', () => {
    it('hace POST a /api/reportes/persona/pdf y devuelve Blob', () => {
      const pdfBlob = new Blob(['%PDF-1.4'], { type: 'application/pdf' });

      service.generarPdf(requestBase).subscribe(blob => {
        expect(blob).toBeInstanceOf(Blob);
        expect(blob.size).toBe(pdfBlob.size);
      });

      const req = httpMock.expectOne('http://localhost:5158/api/reportes/persona/pdf');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(requestBase);
      expect(req.request.responseType).toBe('blob');
      req.flush(pdfBlob);
    });
  });

  describe('generarWord', () => {
    it('hace POST a /api/reportes/persona/word y devuelve Blob', () => {
      const docxBlob = new Blob(['PK'], { type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' });

      service.generarWord(requestBase).subscribe(blob => {
        expect(blob).toBeInstanceOf(Blob);
        expect(blob.size).toBe(docxBlob.size);
      });

      const req = httpMock.expectOne('http://localhost:5158/api/reportes/persona/word');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(requestBase);
      expect(req.request.responseType).toBe('blob');
      req.flush(docxBlob);
    });
  });

  describe('preview', () => {
    it('hace POST a /api/reportes/persona/preview y devuelve el ResultadoAnalisis', () => {
      const resultadoMock: ResultadoAnalisis = {
        persona: requestBase.persona,
        resumen: {
          totalDias: 22, ausencias: 0, tardanzasSeveras: 0, tardanzasLeves: 0,
          excesosAlmuerzo: 0, salidasAnticipadas: 0, registrosAnomalos: 0, justificaciones: 0
        },
        dias: [
          {
            fecha: '2026-06-01',
            estado: 'ok',
            llegada: '08:00:00',
            salida: '17:00:00',
            horaProgramadaEntrada: '08:00:00',
            horaProgramadaSalida: '17:00:00',
            retrasoMinutos: 0,
            almuerzoSalida: null, almuerzoRegreso: null,
            almuerzoDuracionMinutos: 0, almuerzoExcesoMinutos: 0,
            salidaAnticipadaMinutos: 0, tiempoNetoMinutos: 540,
            observaciones: [], registros: [],
            esFeriado: false, esDiaLibre: false, esDiaExcluido: false,
            justificacion: null
          }
        ],
        configuracion: requestBase.configuracion,
        fechaDesde: '2026-06-01',
        fechaHasta: '2026-06-01'
      };

      service.preview(requestBase).subscribe(r => {
        expect(r).toEqual(resultadoMock);
        expect(r.dias).toHaveLength(1);
        expect(r.resumen.totalDias).toBe(22);
      });

      const req = httpMock.expectOne('http://localhost:5158/api/reportes/persona/preview');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(requestBase);
      req.flush(resultadoMock);
    });
  });
});