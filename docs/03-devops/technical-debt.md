---
title: "Deuda técnica — DevOps"
type: technical-debt
status: open
date: 2026-06-29
tags: [devops, deuda]
related: ["0002-ci-pipelines.md", "0003-cicd-runbook.md"]
---

# Deuda técnica — DevOps

## Pendiente para próximas iteraciones

### 1. E2E con Playwright
- **Contexto**: existe `.playwright-mcp/` en el repo (se usa Playwright localmente), pero no hay workflow de integración continua que automatice los e2e.
- **Trabajo a hacer**: crear `.github/workflows/03-integration.yml` (o extender `02-ci-web.yml` con un job `e2e`) que levante backend + frontend en background y corra tests de Playwright.
- **Estimación**: M (4–6h)
- **Bloquea**: nada por ahora.

### 2. Deploy a staging
- **Contexto**: no hay entorno de deploy definido. El proyecto es una herramienta local de generación de PDFs, pero podría tener un entorno compartido para QA.
- **Trabajo a hacer**: crear workflow de deploy con target a definir (Azure Container Apps / Docker / on-prem vía SSH).
- **Estimación**: L (1 día)
- **Bloquea**: definir target de deploy primero.

### 3. Publicación de paquetes NuGet
- **Contexto**: las dependencias internas (QuestPDF, DocumentFormat.OpenXml) son NuGet privadas pero podrían publicarse versiones own del backend.
- **Trabajo a hacer**: configurar `dotnet pack` + publish a NuGet o feed privado en el workflow de release.
- **Estimación**: S (2h)
- **Bloquea**: definir feed privado primero.

### 4. Endurecer gate de `dotnet format` en CI ✅ CERRADO 2026-06-29
- **Contexto**: el job `format` corría en modo warning (`continue-on-error: true`) hasta que se mergee un commit `chore: apply dotnet format` y se habilite el gate estricto. El `.editorconfig` raíz ya está definido.
- **Trabajo hecho**:
  - Aplicado `dotnet format src/FakeReports.sln` (modificó 3 archivos: `ReportePorPersonaWordGenerator.cs`, `FeriadosEndpointsTests.cs`, `ReportesEndpointsTests.cs`).
  - Removido `continue-on-error: true` y el `set +e` / `exit 0` del job.
  - Reemplazado el script multi-línea por uno explícito con `set -euo pipefail`.
  - Tests siguen pasando: 48/48 verdes.
- **Verificado**: `dotnet format src/FakeReports.sln --verify-no-changes --verbosity normal` retorna exit 0 localmente.

### 5. Auditoría de seguridad — items importantes pendientes
- **Contexto**: la auditoría 2026-06-29 (ver `0003-cicd-runbook.md`) identificó 9 items "importantes" que decidimos dejar fuera del setup inicial. Aplicar cuando crezca el equipo.
- **Items**:
  1. CODEOWNERS con escape hatch (segundo owner o equipo `@istpet-dev/backend`).
  2. CODEOWNERS con separación de duties para `.github/**`.
  3. ~~Dependabot `groups:` para agrupar minor/patch en PRs únicas.~~ ✅ Cerrado: Dependabot ya las agrupa nativamente.
  4. Auto-merge de PRs de Dependabot condicional a CI verde (después de activar branch protection) — pendiente de activar "Allow auto-merge" en Settings.
  5. Eliminar `npm ci` redundante en `02-ci-web.yml` consolidando audit dentro del job `test`.
  6. Considerar `set -euo pipefail` en steps de shell ya presente (✓ aplicado) — auditar más steps.
  7. Sanitizar subject de commit en changelog (`printf` puede romper con caracteres especiales).
- **Estimación**: M (4h)
- **Bloquea**: ninguno individualmente; bloquea auto-merge cuando crezca el equipo.

### 6. Test de regresión XXE en `check-coverage.py` ✅ CERRADO 2026-06-29
- **Contexto**: el script `scripts/check-coverage.py` está protegido contra XXE / billion-laughs vía `defusedxml`. Agregado un test de regresión.
- **Trabajo hecho**: creado `tests/test_check_coverage.py` con 8 casos (incluido `test_billion_laughs_payload_does_not_hang`). También:
  - Creado `tests/__init__.py` para que `python3 -m unittest discover` encuentre los tests.
  - Sumado step en `01-ci-api.yml` job `test` que corre los tests antes del coverage gate real.
  - Arreglado bug menor en `check-coverage.py`: los `::error::` ahora van a stderr (no stdout), helpers `_notice()` y `_err()` para consistencia.
- **Verificado localmente**: `python3 -m unittest discover -v` → `Ran 8 tests in 0.172s — OK`.

### 7. Vulnerabilidades High transitivas en NuGet (System.* 4.3.0)
- **Contexto**: `dotnet list src/FakeReports.sln package --vulnerable --include-transitive` encontró 2 vulnerabilidades High (al 2026-06-29):
  - `System.Net.Http 4.3.0` (GHSA-7jgj-8wvc-jh57)
  - `System.Text.RegularExpressions 4.3.0` (GHSA-cmhx-cq75-c4mj)
- **Causa probable**: paquetes viejos de .NET Framework legados que entran como dependencias transitivas a través de la cadena NuGet. No son código nuestro.
- **Decisión actual**: confiamos en Dependabot security updates (no hay job dedicado en CI). Las CVEs deberían generar PRs automáticas en GitHub.
- **Trabajo a hacer**:
  1. Verificar que Dependabot security updates está activado en Settings → Code security → Dependabot.
  2. Si Dependabot no las levanta, bumpear manualmente las dependencias o aceptar el riesgo con un ADR.
- **Estimación**: S (1h) si Dependabot ya las levantó; M (4h) si hay que bumpear manualmente.
- **Bloquea**: nada crítico inmediato (es una herramienta local de PDFs, no servidor público).

### 8. Prettier `style: apply prettier` (no se había aplicado nunca) ✅ CERRADO 2026-06-29
- **Contexto**: el repo nunca había sido formateado con Prettier. Al activar el gate `prettier --check` en CI, 19 archivos generaron errores.
- **Trabajo hecho**: aplicado `npx prettier --write "src/**/*.{ts,html,scss,json}"` en un commit posterior. Diff: 19 archivos modificados (solo whitespace y reformateo).
- **Verificación**: Prettier local pasa. CI debería pasar el job `Lint (Prettier)` en el próximo run.

### 9. Angular 22 + Vitest 4 — flag `--code-coverage` removido ✅ CERRADO 2026-06-29
- **Contexto**: el CLI `ng test` de Angular 22 con builder `@angular/build:unit-test` no acepta el flag `--code-coverage`. La cobertura se configura en `angular.json` → `target.test.options.coverage: true`.
- **Trabajo hecho**:
  - Configurado `coverage: true` en `angular.json` test target con reporters `[lcov, text-summary]`.
  - Agregada dep `@vitest/coverage-v8@^4.0.0` como devDep (Vitest 4 la requiere explícitamente).
  - Removido `--code-coverage` del comando `npx ng test` en `02-ci-web.yml`.
  - Corregido path del artefacto de cobertura (`coverage/fake-reports-web/lcov.info` con subdirectorio).
- **Verificado**: `npx ng test --watch=false` genera `lcov.info` y muestra `Coverage summary` en consola. 36/36 tests verdes.
- **Nota**: la cobertura actual del frontend es ~25% lines. El gate duro del 60% queda como decisión pendiente (no activé script para leerla en el workflow porque faltaba el flag).

### 10. Test .NET rojo: `Normaliza_Registros_Utc_A_Local` (dependía de TZ ambiental) ✅ CERRADO 2026-06-29
- **Contexto**: el test asumía que `NormalizarZonasHorarias` convierte UTC → hora local de Argentina (UTC-3). Pero el código usaba `ToLocalTime()` que depende de la TZ del proceso, no del dominio. En runner Ubuntu UTC, el test fallaba porque `ToLocalTime` era no-op.
- **Causa raíz**: implementación ambiental-dependiente, no contrato de negocio.
- **Trabajo hecho**:
  - `AnalizadorAsistencias.NormalizarZonasHorarias` ahora usa `TimeZoneInfo.CreateCustomTimeZone("Argentina UTC-3", TimeSpan.FromHours(-3), ...)` con offset fijo.
  - Se eligió `CreateCustomTimeZone` sobre `FindSystemTimeZoneById("America/Argentina/Buenos_Aires")` porque no depende de que el paquete `tzdata` esté instalado en el runner.
  - Comentario inline documenta por qué se toma esta decisión.
  - `dotnet format` aplicado al archivo modificado (sigue cumpliendo el strict gate).
- **Verificado**: `dotnet test` → 48/48 verde localmente. Los 23 tests que ya pasaban (rama `Unspecified`) siguen pasando porque no se tocaron.

## Referencias
- [[0002-ci-pipelines]] — pipeline CI actual al que se suma esta deuda
- [[0003-cicd-runbook]] — runbook con tabla de actions pinneadas y troubleshooting
