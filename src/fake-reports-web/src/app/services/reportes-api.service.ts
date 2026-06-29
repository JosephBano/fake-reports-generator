import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReporteRequest, ResultadoAnalisis } from '../models/reporte.model';

const API_URL = 'http://localhost:5158/api';

@Injectable({ providedIn: 'root' })
export class ReportesApiService {
  constructor(private http: HttpClient) {}

  generarPdf(request: ReporteRequest): Observable<Blob> {
    return this.http.post(`${API_URL}/reportes/persona/pdf`, request, { responseType: 'blob' });
  }

  generarWord(request: ReporteRequest): Observable<Blob> {
    return this.http.post(`${API_URL}/reportes/persona/word`, request, { responseType: 'blob' });
  }

  preview(request: ReporteRequest): Observable<ResultadoAnalisis> {
    return this.http.post<ResultadoAnalisis>(`${API_URL}/reportes/persona/preview`, request);
  }
}
