---
title: Spec — Próxima iteración de Fake Reports
tags: [feature, spec]
status: active
created: 2026-06-25
updated: 2026-06-29
---

# Spec — Próxima iteración

Este documento agrupa las funcionalidades planificadas para la siguiente sesión de trabajo sobre `fake_reports`.

---

## 1. Edición de feriados desde el frontend

### Objetivo
Permitir al usuario administrar el calendario de feriados sin tener que editar manualmente `src/FakeReports.Api/Feriados/feriados.json`.

### Criterios de aceptación
- [x] Nueva sección "Feriados" en la página `home`.
- [x] Lista de feriados cargada desde `GET /api/feriados` al iniciar.
- [x] Formulario para agregar feriado: fecha + descripción.
- [x] Botón para eliminar un feriado existente.
- [x] Botón "Guardar feriados" que envíe `PUT /api/feriados`.
- [x] Mensaje de confirmación/error tras guardar.
- [x] Los feriados se usan automáticamente al generar reportes.

### Archivos a tocar
- `src/fake-reports-web/src/app/pages/home/home.ts`
- `src/fake-reports-web/src/app/pages/home/home.html`
- `src/fake-reports-web/src/app/pages/home/home.scss`
- `src/FakeReports.Api/Endpoints/FeriadosEndpoints.cs` (verificar manejo de PUT)

---

## 2. Vista previa del reporte

### Objetivo
Mostrar una representación visual del reporte antes de descargarlo, reduciendo iteraciones de prueba.

### Criterios de aceptación
- [x] Nuevo endpoint `POST /api/reportes/persona/preview` que devuelva un JSON con el resumen y el detalle cronológico (mismo análisis, sin generar archivo).
- [x] Nuevo componente `reporte-preview` en Angular.
- [x] Sección "Vista previa" en `home` que muestre:
  - Datos de la persona y período.
  - Tabla resumen con conteos.
  - Tabla detalle por día con colores según estado.
- [x] Botón "Actualizar vista previa" para refrescar sin descargar.
- [x] La vista previa se actualiza automáticamente al cambiar configuración relevante.

### Archivos a tocar
- `src/FakeReports.Api/Endpoints/ReportesEndpoints.cs` (agregar preview)
- `src/FakeReports.Api/Analisis/AnalizadorAsistencias.cs` (reutilizar)
- `src/fake-reports-web/src/app/services/reportes-api.service.ts`
- `src/fake-reports-web/src/app/components/reporte-preview/` (crear)
- `src/fake-reports-web/src/app/pages/home/home.*`

---

## 3. Reporte de varias personas

### Objetivo
Soportar múltiples personas y generar un reporte general que las agrupe.

### Criterios de aceptación
- [ ] El estado de la app pasa a manejar un array de personas en lugar de una sola.
- [ ] Sección "Personas" con listado, alta, edición y eliminación.
- [ ] Cada persona tiene su propio horario, registros, días excluidos y justificaciones.
- [ ] Posibilidad de seleccionar qué personas incluir en el reporte general.
- [ ] Nuevos endpoints:
  - `POST /api/reportes/general/pdf`
  - `POST /api/reportes/general/word`
- [ ] El reporte general incluye:
  - Portada general.
  - Resumen consolidado.
  - Sección por persona (portada individual + detalle).
- [ ] Migración suave del estado actual (una sola persona) al nuevo formato.

### Archivos a tocar
- `src/fake-reports-web/src/app/models/reporte.model.ts` (estructura multi-persona)
- `src/fake-reports-web/src/app/services/storage.service.ts` (migración de estado)
- `src/fake-reports-web/src/app/pages/home/home.*` (gestión de personas)
- `src/FakeReports.Api/Modelos/ReporteRequest.cs` (request general)
- `src/FakeReports.Api/Endpoints/ReportesEndpoints.cs`
- `src/FakeReports.Api/Generadores/Pdf/ReporteGeneralPdfGenerator.cs` (crear)
- `src/FakeReports.Api/Generadores/Word/ReporteGeneralWordGenerator.cs` (crear)

---

## 4. Mejoras en gestión de asistencias

### Objetivo
Agilizar la carga y edición de registros de asistencia, que es la tarea más repetitiva.

### Criterios de aceptación
- [ ] Edición inline de registros existentes (fecha/hora/tipo).
- [ ] Carga masiva mediante área de texto con formato simple (una línea por registro):
  - Ejemplo: `2026-06-25 08:00 entrada`.
- [ ] Generación automática de registros para un rango de días laborables:
  - Entrada y salida según horario.
  - Opcional: incluir almuerzo (4 registros).
  - Opcional: variar horarios ligeramente para simular realismo.
- [ ] Botón "Limpiar todos los registros" con confirmación.

### Archivos a tocar
- `src/fake-reports-web/src/app/pages/home/home.ts`
- `src/fake-reports-web/src/app/pages/home/home.html`
- `src/fake-reports-web/src/app/pages/home/home.scss`

---

## Orden sugerido de implementación

1. **Vista previa del reporte** — aporta valor inmediato y valida el análisis.
2. **Edición de feriados desde el frontend** — mejora la usabilidad con poco esfuerzo.
3. **Mejoras en gestión de asistencias** — reduce fricción al crear reportes.
4. **Reporte de varias personas** — cambio más grande; dejar para el final.

## Notas

- Mantener el principio de "sin base de datos relacional": todo el estado sigue en `localStorage` + JSON.
- Cada feature debe incluir build exitoso (`dotnet build` y `ng build`) antes de cerrar.
- Considerar agregar tests unitarios para `AnalizadorAsistencias` al tocar lógica de análisis.
