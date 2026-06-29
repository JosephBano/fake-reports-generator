---
title: Estructura del proyecto
tags: [architecture]
status: active
created: 2026-06-24
updated: 2026-06-25
---

# Estructura del proyecto

Este documento describe la organización del generador de reportes biométricos falsos.

## Principios

- **Sin base de datos relacional:** el estado vive en el navegador (`localStorage`) y es portable mediante JSON.
- **Backend mínimo:** .NET 8 Minimal API encargado únicamente de generar PDF/DOCX y servir feriados.
- **Frontend rico:** Angular 22 standalone con formularios reactivos para configurar personas, horarios, asistencias y el reporte en una página inicial (`home`).
- **Documentación en `docs/`:** estructura Obsidian-compatible.

## Layout de carpetas

```
fake_reports/
├── README.md                          # Cómo levantar y usar el proyecto
├── .gitignore
├── docs/                              # Documentación Obsidian-compatible
│   ├── 01-architecture/
│   │   └── project-structure.md       # Este archivo
│   └── 02-decisions/
│       ├── README.md
│       └── 0001-stack-fake-reports.md # ADR de stack y alcance (accepted)
├── src/
│   ├── FakeReports.Api/               # Backend .NET 8 Minimal API
│   │   ├── FakeReports.Api.csproj
│   │   ├── Program.cs                 # Registra endpoints, CORS y licencia QuestPDF
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Feriados/
│   │   │   └── feriados.json          # Fechas festivas editables
│   │   ├── Modelos/
│   │   │   ├── ReporteRequest.cs      # Contratos de entrada (persona, horario, asistencias, etc.)
│   │   │   └── ResultadoAnalisis.cs   # Modelos de salida del análisis
│   │   ├── Analisis/
│   │   │   └── AnalizadorAsistencias.cs
│   │   ├── Generadores/
│   │   │   ├── Pdf/
│   │   │   │   └── ReportePorPersonaPdfGenerator.cs
│   │   │   └── Word/
│   │   │       └── ReportePorPersonaWordGenerator.cs
│   │   └── Endpoints/
│   │       ├── ReportesEndpoints.cs
│   │       └── FeriadosEndpoints.cs
│   └── fake-reports-web/              # Frontend Angular 22 standalone
│       ├── package.json
│       ├── angular.json
│       └── src/
│           ├── index.html
│           ├── main.ts
│           ├── styles.scss
│           └── app/
│               ├── app.config.ts
│               ├── app.routes.ts
│               ├── models/
│               │   └── reporte.model.ts
│               ├── services/
│               │   ├── storage.service.ts
│               │   ├── feriados.service.ts
│               │   ├── reportes-api.service.ts
│               │   └── export-import.service.ts
│               └── pages/
│                   └── home/
│                       ├── home.ts
│                       ├── home.html
│                       └── home.scss
```

## Capas

### Frontend

Responsable de capturar, validar y persistir la configuración del reporte.

- **`localStorage`** guarda el estado entre sesiones.
- **Import/export JSON** permite respaldar o compartir configuraciones.
- **Llamadas al backend** solo para descargar PDF/DOCX y obtener/actualizar feriados.

### Backend

Responsable de recibir el payload completo y generar documentos de alta fidelidad.

- **Modelos:** contratos de datos compartidos entre análisis y generadores.
- **Análisis:** convierte registros de asistencia en días analizados con novedades (tardanzas, ausencias, excesos de almuerzo, salidas anticipadas, registros anómalos).
- **Generadores:** crean PDF con QuestPDF y DOCX con DocumentFormat.OpenXml.
- **Endpoints:** exponen la API REST mínima.

### Feriados

Archivo `src/FakeReports.Api/Feriados/feriados.json` con array de fechas en formato `YYYY-MM-DD`. Puede editarse manualmente; en el futuro se podrá cargar/actualizar desde el frontend.

## Convenciones

- Nombres de clases y archivos en español, reflejando el dominio del negocio.
- Modelos inmutables de request/response para el intercambio frontend-backend.
- Generadores desacoplados del transporte HTTP: reciben objetos de dominio y devuelven `byte[]`.
- Normalización de zonas horarias en el backend para evitar desfasajes entre el frontend y el análisis.
