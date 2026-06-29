---
title: "0003 - Runbook de CI/CD"
type: runbook
status: active
date: 2026-06-29
tags: [devops, runbook, github-actions]
related: ["0001-branching-strategy.md", "0002-ci-pipelines.md"]
---

# Runbook de CI/CD

Esta guía explica cómo debuggear y operar los pipelines de GitHub Actions del proyecto fake_reports.

## Pipelines activos

| Workflow | Trigger | Jobs principales |
|---|---|---|
| `01-ci-api.yml` | PR a `main` o `push` a `main` (paths del backend) | `restore-build`, `test`, `format`, `secrets` |
| `02-ci-web.yml` | PR a `main` o `push` a `main` (paths del frontend) | `lint`, `test`, `build`, `npm audit`, `secrets` |
| `03-release.yml` | Tag `v*.*.*` o manual | build + empaquetado + GitHub Release con changelog |

## Actions pinneadas por SHA

Todas las actions externas están referenciadas por SHA de commit con el tag como comentario, para mitigar riesgo de supply-chain (tags mutables pueden ser reescritos por un mantenedor comprometido).

| Action | Tag | Comentario |
|---|---|---|
| `actions/checkout` | v6.0.3 | `9f698171ed81b15d1823a05fc7211befd50c8ae0` |
| `actions/setup-dotnet` | v5.4.0 | `26b0ec14cb23fa6904739307f278c14f94c95bf1` |
| `actions/setup-node` | v6.4.0 | `48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e` |
| `actions/setup-python` | v6.3.0 | `ece7cb06caefa5fff74198d8649806c4678c61a1` |
| `actions/cache` | v6.1.0 | `55cc8345863c7cc4c66a329aec7e433d2d1c52a9` |
| `actions/upload-artifact` | v7.0.1 | `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` |
| `gitleaks/gitleaks-action` | v3.0.0 | `e0c47f4f8be36e29cdc102c57e68cb5cbf0e8d1e` |
| `softprops/action-gh-release` | v3.0.1 | `2bb465e97f322d3cb2a965294d483e0d26a67aa9` |
| `dorny/test-reporter` | v3.0.0 | `a6ddd83ac95ff4586f5d3aceeb314d9a1841db95` |

**Cómo actualizar**: Dependabot crea PRs automáticas para `github-actions`. Para actualizar manualmente, buscar el SHA en `git ls-remote https://github.com/<owner>/<repo>.git refs/tags/<tag>` y reemplazar.

## Cómo correr todo local antes de hacer PR

### Backend

```bash
dotnet restore src/FakeReports.sln
dotnet build src/FakeReports.sln -c Release /warnaserror
dotnet test src/FakeReports.Api.Tests/FakeReports.Api.Tests.csproj \
  -c Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

dotnet list src/FakeReports.sln package --vulnerable --include-transitive
```

### Frontend

> **Requisito de Node**: Angular 22 requiere Node **>=22.22.3** o **>=24.15.0** o **>=26.0.0**. Node 20 LTS no funciona. CI usa Node 22 LTS. Si usás nvm: `nvm use 22`.

```bash
cd src/fake-reports-web
npm ci
npx prettier --check "src/**/*.{ts,html,scss,json}"
npx ng test --watch=false --code-coverage
npx ng build --configuration=production

npm audit --audit-level=high
```

### Secretos

```bash
# Instalar gitleaks local (requiere binario)
# https://github.com/gitleaks/gitleaks
gitleaks detect --source . --redact
```

## Debug por job

### `restore-build` falla

- **Causa típica**: caché NuGet corrupto.
- **Acción**: borrar `.nuget/packages` local y volver a correr.

### `test` falla por cobertura < 60%

- **Threshold**: 60% global, calculado desde `line-rate` en `coverage.cobertura.xml` (backend) o thresholds de Vitest (frontend).
- **Cómo ver el reporte**: descargar artefacto `api-coverage-report` o `web-coverage-report` del run.
- **Cómo diagnosticar local**: el script `scripts/check-coverage.py` puede correrse en local. Primero instalar dependencias:
  ```bash
  pip install -r scripts/requirements-ci.txt
  REPORT_PATH=./TestResults/<guid>/coverage.cobertura.xml COVERAGE_THRESHOLD=60 \
    python3 scripts/check-coverage.py
  ```
- **Trabajo en curso**: si estás en medio de agregar tests, el threshold puede no estar cumpliéndose. Sumar tests es la solución; bajar el threshold es la salida fácil pero debe documentarse en un ADR.

### `format` falla

- **Estado actual (desde 2026-06-29)**: gate estricto (`continue-on-error` removido). El job falla si hay archivos que no cumplen el `.editorconfig` raíz.
- **Cómo arreglar**:
  ```bash
  dotnet format src/FakeReports.sln
  git add -A
  git commit -m "style: apply dotnet format"
  ```
- Después de que el CI corra de nuevo con el formato aplicado, el gate vuelve a pasar.

### `npm audit` falla

- **Frontend**: `npm audit --audit-level=high` muestra el detalle.
- **Backend NuGet**: NO hay job dedicado. Dependabot cubre las vulnerabilidades de NuGet automáticamente vía Security Advisories (Settings → Code security → Dependabot security updates).
- **Cómo resolver**:
  - Frontend: bumpear a la versión segura y commitear `chore(deps): ...`.
  - Backend: mergear la PR de Dependabot que aparece apenas se publique la CVE. Si no hay PR automática, bumpear manual.

### `secrets` falla (gitleaks)

- **NUNCA commitear secretos al repo**.
- Si gitleaks detecta un secreto falso positivo, sumar la regla a `.gitleaksignore` (no commiteado todavía, crear cuando aparezca el primer caso).
- **Sobre `GITLEAKS_LICENSE`**: la acción funciona sin license para repos públicos. Si el repo pasa a privado y se quiere más de 100k detecciones/mes, comprar licencia y setear el secret `GITLEAKS_LICENSE` en Settings → Secrets. Si la secret devuelve empty string, la action la ignora sin fallar — no remover la línea `env: GITLEAKS_LICENSE` por accidente.

### `lint-web` falla (Prettier)

- **Cómo arreglar**:
  ```bash
  cd src/fake-reports-web
  npx prettier --write "src/**/*.{ts,html,scss,json}"
  git add -A
  git commit -m "style: apply prettier"
  ```

### Tests rojos del frontend (`storage.service.spec.ts`)

- **Estado actual (jun 2026)**: 8 tests rojos por bug conocido en `localStorage.clear()` que asume `localStorage` global en jsdom. Estos están siendo arreglados por separado.
- **No agregar shims de `localStorage` en CI** sin coordinación. Mientras los rojos estén, el job `test-web` va a fallar hasta que se arregle la causa raíz.

## Release workflow

### Cómo disparar un release

1. Mergear todos los PRs acumulados a `main`.
2. Local:
   ```bash
   git checkout main
   git pull
   # Confirmar que la CI está verde en main
   git tag vX.Y.Z -m "Release vX.Y.Z"
   git push origin vX.Y.Z
   ```
3. GitHub Actions dispara `03-release.yml` automáticamente.
4. La release aparece en la sección "Releases" del repo con changelog + artefactos adjuntos.

### Validación de tag

El workflow valida que el tag matchee `^v\d+\.\d+\.\d+(-[a-z]+\.\d+)?$` antes de continuar. Tags que no cumplan (ej. `v1.2.3-anything`) caen en error.

### Convención de versionado (SemVer)

- `feat: ...` → bump **minor** (v0.1.0 → v0.2.0).
- `fix: ...` → bump **patch** (v0.1.0 → v0.1.1).
- `BREAKING CHANGE:` en el body del commit → bump **major**.

## Activar branch protection

Una vez que la CI esté verde en `main`, activar **Settings → Branches → Add rule** para `main`:

- ✅ Require a pull request before merging
- ✅ Require approvals: 1
- ✅ Dismiss stale pull request approvals when new commits are pushed
- ✅ Require status checks to pass before merging → seleccionar:
  - `Restore + Build`
  - `Tests + Coverage`
  - `Format check (.editorconfig)`
  - `Lint (Prettier)`
  - `Tests (Vitest + jsdom)`
  - `Build (production)`
  - `npm vulnerability audit`
  - `Secret scan` (job de backend y frontend, hay 2 con este nombre — seleccionar cualquiera; ambos corren)
- ✅ Require conversation resolution before merging
- ✅ Require linear history
- ✅ Include administrators
- ✅ Require review from Code Owners

## Costos de GitHub Actions

- Repo público: minutos ilimitados.
- Repo privado: Free tier = 2000 min/mes. Estimación actual del repo: ~5 min por CI run en cada PR (sin contar release). Holgadamente dentro del free tier.

> Verificar la visibilidad del repo en Settings → General. Si es privado, monitorear el uso mensual en Settings → Billing → Plans and usage.

## Deuda técnica registrada

Ver [technical-debt.md](./technical-debt.md) en este mismo directorio.
