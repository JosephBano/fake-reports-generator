# Fake Reports

Generador de reportes biométricos falsos. App fullstack simple que imita la estructura de reportes de `biometric_sistem_reports`, permitiendo configurar personas, horarios, asistencias, días excluidos, justificaciones y exportar a PDF y Word.

## Stack

| Capa | Tecnología |
|---|---|
| Frontend | Angular 22 standalone |
| Backend | .NET 8 Minimal API |
| Estado | `localStorage` del navegador + import/export JSON |
| Feriados | Archivo `feriados.json` editable |
| PDF | QuestPDF (licencia Community) |
| Word | DocumentFormat.OpenXml |

## Estructura

```
fake_reports/
├── docs/                      # ADR y arquitectura
├── src/
│   ├── FakeReports.Api/       # Backend .NET
│   └── fake-reports-web/      # Frontend Angular
└── README.md
```

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

## Cómo ejecutar

### 1. Backend

```bash
cd src/FakeReports.Api
dotnet run
```

Por defecto escucha en `http://localhost:5158`.

### 2. Frontend

En otra terminal:

```bash
cd src/fake-reports-web
npx ng serve
```

Abre `http://localhost:4200`.

## Uso

1. Completa los datos de la persona.
2. Define el período del reporte.
3. Configura el horario semanal.
4. Agrega registros de asistencia (entrada/salida).
5. Excluye días que no deben aparecer.
6. Agrega justificaciones si es necesario.
7. Personaliza la configuración del reporte (título, colores, secciones, umbrales).
8. Presiona **Generar PDF** o **Generar Word**.

Toda la configuración se guarda automáticamente en el navegador. Podés exportarla/importarla como JSON desde la barra superior.

## Feriados

Editá el archivo `src/FakeReports.Api/Feriados/feriados.json` o usá la sección "Feriados" en el frontend para agregar, eliminar y guardar feriados desde la interfaz. Los feriados se marcan como días no laborables en el reporte.

## Validación

- Backend: `dotnet build src/FakeReports.Api`
- Frontend: `npx ng build --no-watch` (dentro de `src/fake-reports-web`)

## Decisiones técnicas

Ver `docs/02-decisions/0001-stack-fake-reports.md`.

## Decisiones técnicas

Ver `docs/02-decisions/0001-stack-fake-reports.md`.
