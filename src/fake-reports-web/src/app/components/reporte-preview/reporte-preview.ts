import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DiaAnalizado, ResultadoAnalisis } from '../../models/reporte.model';

@Component({
  selector: 'app-reporte-preview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reporte-preview.html',
  styleUrl: './reporte-preview.scss',
})
export class ReportePreview {
  @Input({ required: true }) resultado!: ResultadoAnalisis;

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleDateString();
  }

  formatearHora(hora: string | null): string {
    if (!hora) return '-';
    return hora.slice(0, 5);
  }

  claseFila(dia: DiaAnalizado): string {
    switch (dia.estado) {
      case 'ok':
        return 'estado-ok';
      case 'leve':
        return 'estado-leve';
      case 'severa':
        return 'estado-severa';
      case 'anomalo':
        return 'estado-anomalo';
      case 'ausente':
        return 'estado-ausente';
      case 'feriado':
        return 'estado-feriado';
      case 'libre':
        return 'estado-libre';
      case 'excluido':
        return 'estado-excluido';
      default:
        return '';
    }
  }

  etiquetaEstado(dia: DiaAnalizado): string {
    const map: Record<string, string> = {
      ok: 'OK',
      leve: 'Tardanza leve',
      severa: 'Tardanza severa',
      anomalo: 'Anómalo',
      ausente: 'Ausente',
      feriado: 'Feriado',
      libre: 'Día libre',
      excluido: 'Excluido',
    };
    return map[dia.estado] ?? dia.estado;
  }
}
