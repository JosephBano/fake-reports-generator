import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReportePreview } from './reporte-preview';
import { DiaAnalizado, ResultadoAnalisis } from '../../models/reporte.model';

function crearResultadoBase(): ResultadoAnalisis {
  return {
    persona: { id: 'p1', nombre: 'Juan Pérez', identificacion: '12345678' },
    resumen: {
      totalDias: 22, ausencias: 1, tardanzasSeveras: 2, tardanzasLeves: 3,
      excesosAlmuerzo: 1, salidasAnticipadas: 0, registrosAnomalos: 0, justificaciones: 0
    },
    dias: [],
    configuracion: {} as ResultadoAnalisis['configuracion'],
    fechaDesde: '2026-06-01',
    fechaHasta: '2026-06-30'
  };
}

function crearDia(estado: string, overrides: Partial<DiaAnalizado> = {}): DiaAnalizado {
  return {
    fecha: '2026-06-01',
    estado,
    llegada: null, salida: null,
    horaProgramadaEntrada: null, horaProgramadaSalida: null,
    retrasoMinutos: 0,
    almuerzoSalida: null, almuerzoRegreso: null,
    almuerzoDuracionMinutos: 0, almuerzoExcesoMinutos: 0,
    salidaAnticipadaMinutos: 0, tiempoNetoMinutos: 0,
    observaciones: [], registros: [],
    esFeriado: false, esDiaLibre: false, esDiaExcluido: false,
    justificacion: null,
    ...overrides
  };
}

describe('ReportePreview', () => {
  let component: ReportePreview;
  let fixture: ComponentFixture<ReportePreview>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportePreview]
    }).compileComponents();

    fixture = TestBed.createComponent(ReportePreview);
    const resultado = crearResultadoBase();
    resultado.dias = [
      crearDia('ok'),
      crearDia('leve'),
      crearDia('severa'),
      crearDia('anomalo'),
      crearDia('ausente'),
      crearDia('feriado'),
      crearDia('libre'),
      crearDia('excluido')
    ];
    fixture.componentRef.setInput('resultado', resultado);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('se crea', () => {
    expect(component).toBeTruthy();
  });

  it('renderiza los datos de la persona y del período en el header', () => {
    const html: HTMLElement = fixture.nativeElement;
    const header = html.querySelector('.preview-header');
    expect(header?.textContent).toContain('Juan Pérez');
    expect(header?.textContent).toContain('12345678');
  });

  it('renderiza los conteos del resumen', () => {
    const html: HTMLElement = fixture.nativeElement;
    const resumen = html.querySelector('.resumen-table')?.textContent ?? '';
    expect(resumen).toContain('22'); // totalDias
    expect(resumen).toContain('1');  // ausencias
    expect(resumen).toContain('2');  // tardanzas severas
    expect(resumen).toContain('3');  // tardanzas leves
  });

  it('renderiza una fila por cada día del análisis', () => {
    const html: HTMLElement = fixture.nativeElement;
    const filas = html.querySelectorAll('.detalle-table tbody tr');
    expect(filas.length).toBe(8);
  });

  it('asigna la clase CSS correcta según el estado de cada día', () => {
    const html: HTMLElement = fixture.nativeElement;
    const filas = html.querySelectorAll('.detalle-table tbody tr');

    expect(filas[0].classList.contains('estado-ok')).toBe(true);
    expect(filas[1].classList.contains('estado-leve')).toBe(true);
    expect(filas[2].classList.contains('estado-severa')).toBe(true);
    expect(filas[3].classList.contains('estado-anomalo')).toBe(true);
    expect(filas[4].classList.contains('estado-ausente')).toBe(true);
    expect(filas[5].classList.contains('estado-feriado')).toBe(true);
    expect(filas[6].classList.contains('estado-libre')).toBe(true);
    expect(filas[7].classList.contains('estado-excluido')).toBe(true);
  });

  describe('claseFila', () => {
    it('mapea todos los estados conocidos', () => {
      expect(component.claseFila(crearDia('ok'))).toBe('estado-ok');
      expect(component.claseFila(crearDia('leve'))).toBe('estado-leve');
      expect(component.claseFila(crearDia('severa'))).toBe('estado-severa');
      expect(component.claseFila(crearDia('anomalo'))).toBe('estado-anomalo');
      expect(component.claseFila(crearDia('ausente'))).toBe('estado-ausente');
      expect(component.claseFila(crearDia('feriado'))).toBe('estado-feriado');
      expect(component.claseFila(crearDia('libre'))).toBe('estado-libre');
      expect(component.claseFila(crearDia('excluido'))).toBe('estado-excluido');
    });

    it('devuelve string vacío para estado desconocido', () => {
      expect(component.claseFila(crearDia('desconocido'))).toBe('');
    });
  });

  describe('etiquetaEstado', () => {
    it('devuelve la etiqueta legible para cada estado', () => {
      expect(component.etiquetaEstado(crearDia('ok'))).toBe('OK');
      expect(component.etiquetaEstado(crearDia('leve'))).toBe('Tardanza leve');
      expect(component.etiquetaEstado(crearDia('severa'))).toBe('Tardanza severa');
      expect(component.etiquetaEstado(crearDia('anomalo'))).toBe('Anómalo');
      expect(component.etiquetaEstado(crearDia('ausente'))).toBe('Ausente');
      expect(component.etiquetaEstado(crearDia('feriado'))).toBe('Feriado');
      expect(component.etiquetaEstado(crearDia('libre'))).toBe('Día libre');
      expect(component.etiquetaEstado(crearDia('excluido'))).toBe('Excluido');
    });

    it('devuelve el estado crudo cuando no hay etiqueta mapeada', () => {
      expect(component.etiquetaEstado(crearDia('xyz'))).toBe('xyz');
    });
  });

  describe('formatearHora', () => {
    it('devuelve "-" para null', () => {
      expect(component.formatearHora(null)).toBe('-');
    });

    it('devuelve "-" para string vacío', () => {
      expect(component.formatearHora('')).toBe('-');
    });

    it('recorta a HH:MM cuando la hora trae segundos', () => {
      expect(component.formatearHora('08:00:00')).toBe('08:00');
      expect(component.formatearHora('17:30:45')).toBe('17:30');
    });

    it('devuelve HH:MM sin cambios cuando ya está en ese formato', () => {
      expect(component.formatearHora('08:00')).toBe('08:00');
    });
  });

  describe('formatearFecha', () => {
    it('formatea la fecha con toLocaleDateString', () => {
      const f = component.formatearFecha('2026-06-01');
      // El formato exacto depende del locale de jsdom, pero al menos debe
      // contener el año 2026 y los dígitos 6/01/1/06.
      expect(f).toMatch(/2026/);
    });
  });

  it('muestra el exceso de almuerzo cuando aplica', () => {
    const r = crearResultadoBase();
    r.dias = [
      crearDia('ok', {
        almuerzoDuracionMinutos: 75,
        almuerzoExcesoMinutos: 15
      })
    ];
    fixture.componentRef.setInput('resultado', r);
    fixture.detectChanges();

    const html: HTMLElement = fixture.nativeElement;
    const fila = html.querySelector('.detalle-table tbody tr') as HTMLElement;
    expect(fila.textContent).toContain('75 min');
    expect(fila.textContent).toContain('+15');
  });

  it('renderiza "-" cuando no hay registros para mostrar almuerzo', () => {
    const r = crearResultadoBase();
    r.dias = [crearDia('ok', { almuerzoDuracionMinutos: 0, almuerzoExcesoMinutos: 0 })];
    fixture.componentRef.setInput('resultado', r);
    fixture.detectChanges();

    const html: HTMLElement = fixture.nativeElement;
    const fila = html.querySelector('.detalle-table tbody tr') as HTMLElement;
    // La celda de almuerzo contiene el guion cuando no hay datos
    const celdas = fila.querySelectorAll('td');
    const celdaAlmuerzo = celdas[5];
    expect(celdaAlmuerzo.textContent?.trim()).toBe('-');
  });

  it('renderiza las observaciones concatenadas con " · "', () => {
    const r = crearResultadoBase();
    r.dias = [
      crearDia('leve', {
        retrasoMinutos: 5,
        observaciones: ['Tardanza de 5 minutos', 'Tránsito']
      })
    ];
    fixture.componentRef.setInput('resultado', r);
    fixture.detectChanges();

    const html: HTMLElement = fixture.nativeElement;
    const fila = html.querySelector('.detalle-table tbody tr') as HTMLElement;
    expect(fila.textContent).toContain('Tardanza de 5 minutos · Tránsito');
  });
});