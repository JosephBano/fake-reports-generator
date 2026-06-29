import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
  WritableSignal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { StorageService } from '../../services/storage.service';
import { FeriadosService } from '../../services/feriados.service';
import { ReportesApiService } from '../../services/reportes-api.service';
import { ExportImportService } from '../../services/export-import.service';
import { ReportePreview } from '../../components/reporte-preview/reporte-preview';
import {
  AppState,
  ConfiguracionReporte,
  Feriado,
  HorarioSemanal,
  RegistroAsistencia,
  ReporteRequest,
  ResultadoAnalisis,
} from '../../models/reporte.model';

const DIAS_SEMANA = [
  'lunes',
  'martes',
  'miercoles',
  'jueves',
  'viernes',
  'sabado',
  'domingo',
] as const;
type DiaSemana = (typeof DIAS_SEMANA)[number];

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ReportePreview],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  protected readonly diasSemana = DIAS_SEMANA;
  protected state!: WritableSignal<AppState>;
  protected personaForm!: FormGroup;
  protected horarioForm!: FormGroup;
  protected registroForm!: FormGroup;
  protected excluirForm!: FormGroup;
  protected justificacionForm!: FormGroup;
  protected configForm!: FormGroup;
  protected rangoForm!: FormGroup;
  protected feriadoForm!: FormGroup;
  protected feriadosLista: Feriado[] = [];
  protected mensaje = signal<string>('');
  protected previewResultado = signal<ResultadoAnalisis | null>(null);
  protected cargandoPreview = signal<boolean>(false);
  protected previewRequest = computed(() => ({
    state: this.state(),
    feriados: this.feriadosLista,
  }));
  private destroyRef = inject(DestroyRef);

  constructor(
    private fb: FormBuilder,
    private storage: StorageService,
    private feriadosService: FeriadosService,
    private reportesApi: ReportesApiService,
    private exportImport: ExportImportService,
  ) {
    this.state = signal(this.storage.load());
  }

  ngOnInit(): void {
    this.crearFormularios();
    this.cargarFeriados();
    this.suscribirCambios();
  }

  private crearFormularios(): void {
    const s = this.state();

    this.personaForm = this.fb.group({
      nombre: [s.persona.nombre, Validators.required],
      identificacion: [s.persona.identificacion, Validators.required],
    });

    this.horarioForm = this.fb.group({
      nombre: [s.horario.nombre],
      almuerzoMinutos: [s.horario.almuerzoMinutos],
      ...Object.fromEntries(
        DIAS_SEMANA.map((d) => [
          d,
          this.fb.group({
            entrada: [s.horario[d].entrada || ''],
            salida: [s.horario[d].salida || ''],
          }),
        ]),
      ),
    });

    this.registroForm = this.fb.group({
      fecha: ['', Validators.required],
      hora: ['', Validators.required],
      tipo: ['entrada', Validators.required],
    });

    this.excluirForm = this.fb.group({
      fecha: ['', Validators.required],
    });

    this.justificacionForm = this.fb.group({
      fecha: ['', Validators.required],
      tipo: ['', Validators.required],
      descripcion: ['', Validators.required],
    });

    this.configForm = this.fb.group({
      titulo: [s.configuracion.titulo],
      subtitulo: [s.configuracion.subtitulo],
      institucion: [s.configuracion.institucion],
      disclaimer: [s.configuracion.disclaimer],
      colorHeader: [s.configuracion.colorHeader],
      colorOk: [s.configuracion.colorOk],
      colorWarn: [s.configuracion.colorWarn],
      colorError: [s.configuracion.colorError],
      umbralTardanzaLeveMin: [s.configuracion.umbralTardanzaLeveMin],
      umbralTardanzaSeveraMin: [s.configuracion.umbralTardanzaSeveraMin],
      umbralExcesoAlmuerzoMin: [s.configuracion.umbralExcesoAlmuerzoMin],
      incluirResumen: [s.configuracion.incluirResumen],
      incluirTardanzas: [s.configuracion.incluirTardanzas],
      incluirAusencias: [s.configuracion.incluirAusencias],
      incluirExcesosAlmuerzo: [s.configuracion.incluirExcesosAlmuerzo],
      incluirSalidasAnticipadas: [s.configuracion.incluirSalidasAnticipadas],
      incluirRegistrosAnomalos: [s.configuracion.incluirRegistrosAnomalos],
      incluirCumplimientoHoras: [s.configuracion.incluirCumplimientoHoras],
    });

    this.rangoForm = this.fb.group({
      fechaDesde: [s.fechaDesde],
      fechaHasta: [s.fechaHasta],
    });

    this.feriadoForm = this.fb.group({
      fecha: ['', Validators.required],
      descripcion: ['', Validators.required],
    });
  }

  private suscribirCambios(): void {
    const guardar = () => {
      const horario = this.horarioForm.value;
      const horarioLimpio: HorarioSemanal = {
        id: this.state().horario.id,
        nombre: horario.nombre,
        almuerzoMinutos: horario.almuerzoMinutos,
        lunes: horario.lunes,
        martes: horario.martes,
        miercoles: horario.miercoles,
        jueves: horario.jueves,
        viernes: horario.viernes,
        sabado: horario.sabado,
        domingo: horario.domingo,
      };

      const nuevo: AppState = {
        persona: { id: this.state().persona.id, ...this.personaForm.value },
        horario: horarioLimpio,
        registros: this.state().registros,
        configuracion: this.configForm.value,
        diasExcluidos: this.state().diasExcluidos,
        justificaciones: this.state().justificaciones,
        fechaDesde: this.rangoForm.value.fechaDesde,
        fechaHasta: this.rangoForm.value.fechaHasta,
      };
      this.state.set(nuevo);
      this.storage.save(nuevo);
    };

    [this.personaForm, this.horarioForm, this.configForm, this.rangoForm].forEach((f) =>
      f.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(guardar),
    );

    toObservable(this.previewRequest)
      .pipe(
        debounceTime(800),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.actualizarVistaPrevia());
  }

  private cargarFeriados(): void {
    this.feriadosService.obtener().subscribe({
      next: (f) =>
        (this.feriadosLista = f
          .map((x) => ({
            fecha: x.fecha.slice(0, 10),
            descripcion: x.descripcion,
          }))
          .sort((a, b) => a.fecha.localeCompare(b.fecha))),
      error: () => (this.feriadosLista = []),
    });
  }

  protected agregarFeriado(): void {
    if (this.feriadoForm.invalid) return;
    const v = this.feriadoForm.value;
    if (this.feriadosLista.some((f) => f.fecha === v.fecha)) {
      this.mensaje.set('Ya existe un feriado en esa fecha.');
      return;
    }
    this.feriadosLista = [...this.feriadosLista, v].sort((a, b) => a.fecha.localeCompare(b.fecha));
    this.feriadoForm.reset({ fecha: '', descripcion: '' });
    this.mensaje.set('Feriado agregado. Recordá guardar para persistir en el servidor.');
  }

  protected eliminarFeriado(index: number): void {
    this.feriadosLista = this.feriadosLista.filter((_, i) => i !== index);
  }

  protected guardarFeriados(): void {
    this.feriadosService.guardar(this.feriadosLista).subscribe({
      next: (f) => {
        this.feriadosLista = f
          .map((x) => ({
            fecha: x.fecha.slice(0, 10),
            descripcion: x.descripcion,
          }))
          .sort((a, b) => a.fecha.localeCompare(b.fecha));
        this.mensaje.set('Feriados guardados correctamente.');
      },
      error: () => this.mensaje.set('Error al guardar feriados. ¿El backend está corriendo?'),
    });
  }

  protected agregarRegistro(): void {
    if (this.registroForm.invalid) return;
    const v = this.registroForm.value;
    const fechaHora = `${v.fecha}T${v.hora}`;
    const registros = [
      ...this.state().registros,
      { fechaHora, tipo: v.tipo } as RegistroAsistencia,
    ];
    this.actualizarRegistros(registros);
    this.registroForm.reset({ tipo: 'entrada' });
  }

  protected eliminarRegistro(index: number): void {
    const registros = this.state().registros.filter((_, i) => i !== index);
    this.actualizarRegistros(registros);
  }

  private actualizarRegistros(registros: RegistroAsistencia[]): void {
    const nuevo = { ...this.state(), registros };
    this.state.set(nuevo);
    this.storage.save(nuevo);
  }

  protected excluirDia(): void {
    if (this.excluirForm.invalid) return;
    const fecha = this.excluirForm.value.fecha;
    if (!this.state().diasExcluidos.includes(fecha)) {
      const nuevo = { ...this.state(), diasExcluidos: [...this.state().diasExcluidos, fecha] };
      this.state.set(nuevo);
      this.storage.save(nuevo);
    }
    this.excluirForm.reset();
  }

  protected incluirDia(fecha: string): void {
    const nuevo = {
      ...this.state(),
      diasExcluidos: this.state().diasExcluidos.filter((d) => d !== fecha),
    };
    this.state.set(nuevo);
    this.storage.save(nuevo);
  }

  protected agregarJustificacion(): void {
    if (this.justificacionForm.invalid) return;
    const v = this.justificacionForm.value;
    const nuevo = { ...this.state(), justificaciones: [...this.state().justificaciones, v] };
    this.state.set(nuevo);
    this.storage.save(nuevo);
    this.justificacionForm.reset({ tipo: '' });
  }

  protected eliminarJustificacion(index: number): void {
    const nuevo = {
      ...this.state(),
      justificaciones: this.state().justificaciones.filter((_, i) => i !== index),
    };
    this.state.set(nuevo);
    this.storage.save(nuevo);
  }

  protected generar(tipo: 'pdf' | 'word'): void {
    const request = this.construirRequest();
    const obs =
      tipo === 'pdf' ? this.reportesApi.generarPdf(request) : this.reportesApi.generarWord(request);
    const mime =
      tipo === 'pdf'
        ? 'application/pdf'
        : 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
    const extension = tipo === 'pdf' ? 'pdf' : 'docx';

    obs.subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `reporte_${request.persona.nombre}_${new Date().toISOString().slice(0, 10)}.${extension}`;
        a.click();
        URL.revokeObjectURL(url);
        this.mensaje.set(`Reporte ${extension.toUpperCase()} generado correctamente.`);
      },
      error: (err) => {
        console.error(err);
        this.mensaje.set(`Error al generar ${tipo}. ¿El backend está corriendo?`);
      },
    });
  }

  private construirRequest(): ReporteRequest {
    const s = this.state();
    return {
      persona: s.persona,
      horario: s.horario,
      registros: s.registros,
      configuracion: s.configuracion,
      feriados: this.feriadosLista.map((f) => f.fecha),
      diasExcluidos: s.diasExcluidos,
      justificaciones: s.justificaciones,
      fechaDesde: s.fechaDesde,
      fechaHasta: s.fechaHasta,
    };
  }

  protected actualizarVistaPrevia(): void {
    this.cargandoPreview.set(true);
    this.reportesApi.preview(this.construirRequest()).subscribe({
      next: (analisis) => {
        this.previewResultado.set(analisis);
        this.cargandoPreview.set(false);
      },
      error: (err) => {
        console.error(err);
        this.cargandoPreview.set(false);
      },
    });
  }

  protected exportar(): void {
    this.exportImport.exportar(this.state());
  }

  protected importar(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    this.exportImport
      .importar(input.files[0])
      .then((state) => {
        this.state.set(state);
        this.storage.save(state);
        this.crearFormularios();
        this.mensaje.set('Configuración importada correctamente.');
      })
      .catch(() => {
        this.mensaje.set('Error al importar el archivo JSON.');
      });
  }

  protected formatearRegistro(fechaHora: string): string {
    const d = new Date(fechaHora);
    return `${d.toLocaleDateString()} ${d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  }
}
