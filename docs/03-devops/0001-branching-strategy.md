---
title: "0001 - Estrategia de branching y releases"
type: adr
status: accepted
date: 2026-06-29
deciders: equipo fake_reports
tags: [devops, branching, github-flow]
related: ["[[0002-ci-pipelines]]"]
---

# ADR 0001 — Estrategia de branching y releases

## Contexto
El proyecto fake_reports es un fullstack chico (Angular 22 + .NET 8 Minimal API) mantenido por un equipo pequeño. Necesitamos una estrategia de branching que sea simple pero que permita validar cambios automáticamente antes de llegar a producción.

## Decisión
Adoptamos **GitHub Flow** con squash merge:

- `main` es la única rama de larga vida, siempre deployable
- Todo trabajo se hace en ramas cortas: `feat/[scope]-[slug]`, `fix/[scope]-[slug]`, `chore/[scope]-[slug]`, `docs/[scope]-[slug]`
- Merge a `main` solo vía PR con checks verdes + 1 aprobación
- Commits siguen **Conventional Commits** para versionado automático
- Releases se taggean desde `main` con SemVer (`vX.Y.Z`)

## Consecuencias
Positivas:
- Simple, documentado, alineado con defaults de GitHub
- Compatible con branch protection y required checks
- Permite versionado automático vía Conventional Commits

Negativas:
- No hay rama de release prolongada (mitigado: tags + changelog automático)
- Requiere disciplina de PRs pequeños

## Branch protection aplicada
- Require PR + 1 aprobación
- Require status checks: build-api, test-api, lint-web, test-web, build-web, coverage, security-audit
- Require linear history (squash)
- Include administrators

## Referencias
- [[0002-ci-pipelines]] — pipelines de CI que aplican los checks de branch protection