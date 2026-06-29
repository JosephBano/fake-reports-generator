import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FeriadosService } from './feriados.service';
import { Feriado } from '../models/reporte.model';

describe('FeriadosService', () => {
  let service: FeriadosService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(FeriadosService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('se crea', () => {
    expect(service).toBeTruthy();
  });

  describe('obtener', () => {
    it('hace GET a /api/feriados', () => {
      const mockFeriados: Feriado[] = [
        { fecha: '2026-01-01', descripcion: 'Año Nuevo' },
        { fecha: '2026-05-01', descripcion: 'Trabajo' }
      ];

      service.obtener().subscribe(f => {
        expect(f).toEqual(mockFeriados);
      });

      const req = httpMock.expectOne('http://localhost:5158/api/feriados');
      expect(req.request.method).toBe('GET');
      req.flush(mockFeriados);
    });

    it('propaga errores HTTP', () => {
      let errorCapturado: unknown;

      service.obtener().subscribe({
        next: () => { /* no debería llamarse */ },
        error: e => errorCapturado = e
      });

      const req = httpMock.expectOne('http://localhost:5158/api/feriados');
      req.flush('Error interno', { status: 500, statusText: 'Server Error' });

      expect(errorCapturado).toBeTruthy();
    });
  });

  describe('guardar', () => {
    it('hace PUT a /api/feriados con la lista', () => {
      const feriados: Feriado[] = [
        { fecha: '2026-12-25', descripcion: 'Navidad' }
      ];

      service.guardar(feriados).subscribe(f => {
        expect(f).toEqual(feriados);
      });

      const req = httpMock.expectOne('http://localhost:5158/api/feriados');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(feriados);
      req.flush(feriados);
    });

    it('PUT con lista vacía', () => {
      service.guardar([]).subscribe(f => {
        expect(f).toEqual([]);
      });

      const req = httpMock.expectOne('http://localhost:5158/api/feriados');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual([]);
      req.flush([]);
    });

    it('propaga errores HTTP', () => {
      let errorCapturado: unknown;

      service.guardar([]).subscribe({
        next: () => { /* no debería llamarse */ },
        error: e => errorCapturado = e
      });

      const req = httpMock.expectOne('http://localhost:5158/api/feriados');
      req.flush('No se pudo guardar', { status: 400, statusText: 'Bad Request' });

      expect(errorCapturado).toBeTruthy();
    });
  });
});