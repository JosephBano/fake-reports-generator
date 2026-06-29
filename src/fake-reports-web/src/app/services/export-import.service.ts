import { Injectable } from '@angular/core';
import { AppState } from '../models/reporte.model';

@Injectable({ providedIn: 'root' })
export class ExportImportService {
  exportar(state: AppState, nombre: string = 'fake-reports-config.json'): void {
    const blob = new Blob([JSON.stringify(state, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombre;
    a.click();
    URL.revokeObjectURL(url);
  }

  importar(archivo: File): Promise<AppState> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        try {
          const state = JSON.parse(reader.result as string) as AppState;
          resolve(state);
        } catch (e) {
          reject(e);
        }
      };
      reader.onerror = reject;
      reader.readAsText(archivo);
    });
  }
}
