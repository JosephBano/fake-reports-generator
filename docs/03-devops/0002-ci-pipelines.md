---
title: "0002 - Pipelines de CI con GitHub Actions"
type: adr
status: accepted
date: 2026-06-29
deciders: equipo fake_reports
tags: [devops, ci, github-actions, dotnet, angular]
related: ["[[0001-branching-strategy]]", "[[technical-debt]]"]
---

# ADR 0002 — Pipelines de CI con GitHub Actions

## Contexto
Necesitamos validación automática en cada PR para mantener `main` deployable. El proyecto tiene backend .NET 8 y frontend Angular 22, con trabajo paralelo en pruebas unitarias.

## Decisión
Adoptamos GitHub Actions con workflows separados por stack:

- `01-ci-api.yml` — backend .NET: restore + build (warnaserror), test (xUnit + coverlet), coverage gate 60%, format check, security audit
- `02-ci-web.yml` — frontend Angular: install + lint (ESLint), format (Prettier), test (Karma+Jasmine headless), build prod, coverage gate 60%, security audit (npm audit)
- `04-release.yml` — versionado y release automático en tags `v*.*.*`

Triggers:
- `pull_request` a `main`: corre CI completa
- `push` a `main`: corre CI + publica artefactos de build
- `push` tags `v*`: dispara release

## Convenciones
- Cache agresivo para NuGet y npm
- Matrix OS: ubuntu-latest (extensible en el futuro)
- Sin secretos en logs
- Matriz de checks públicos en job summary (coverage, vulnerabilidades)

## Consecuencias
Positivas:
- Feedback rápido por stack (PRs del backend no rompen CI del frontend si fallan jobs independientes)
- Build artefactos disponibles para deploy futuro
- Gates de calidad explícitos (coverage, security, format)

Negativas:
- Coste de minutos de GitHub Actions (mitigado: cache y solo en PR/push)
- Mantenimiento de 3 workflows (mitigado: separación clara por responsabilidad)

## Referencias
- [[0001-branching-strategy]] — estrategia de branching que define los triggers y checks requeridos
- [[technical-debt]] — E2E, deploy y publicación pendientes