import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Feriado } from '../models/reporte.model';

const API_URL = 'http://localhost:5158/api';

@Injectable({ providedIn: 'root' })
export class FeriadosService {
  constructor(private http: HttpClient) {}

  obtener(): Observable<Feriado[]> {
    return this.http.get<Feriado[]>(`${API_URL}/feriados`);
  }

  guardar(feriados: Feriado[]): Observable<Feriado[]> {
    return this.http.put<Feriado[]>(`${API_URL}/feriados`, feriados);
  }
}
