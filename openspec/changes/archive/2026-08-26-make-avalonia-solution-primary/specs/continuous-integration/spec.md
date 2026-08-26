# continuous-integration Specification (delta)

## MODIFIED Requirements

### Requirement: Verificación automática de avalonia/main (Windows)
El repositorio SHALL contener un workflow de GitHub Actions (`.github/workflows/fork-ci.yml`)
que ejecute la verificación de la solución cross-platform (`GitExtensions.slnx`) en un runner
Windows en cada push/PR a `avalonia/main` (excepto cambios solo en `openspec/`) y bajo demanda
(`workflow_dispatch`).

#### Scenario: Push a la rama de trabajo
- **WHEN** se hace push de un commit a `avalonia/main` que toca ficheros fuera de `openspec/`
- **THEN** GitHub Actions arranca el job `verify-windows` y el commit queda marcado con el
  resultado (✓/✗)

#### Scenario: Push solo de documentación OpenSpec
- **WHEN** se hace push de un commit cuyos ficheros están todos bajo `openspec/`
- **THEN** el workflow no se ejecuta

### Requirement: Verificación en Linux (canary multiplataforma)
El workflow SHALL contener un job `verify-linux` que ejecute `eng/Verify-Linux.ps1` en un
runner `ubuntu-latest`: compilación de la solución cross-platform `GitExtensions.slnx` y
ejecución del subset `net10.0` de `GitCommands.Tests`, de forma simétrica con el job Windows.

#### Scenario: PR activa ambos jobs
- **WHEN** se abre una PR contra `avalonia/main` con cambios fuera de `openspec/`
- **THEN** GitHub Actions arranca `verify-windows` y `verify-linux` en paralelo, y ambos
  deben pasar para que el check de la PR aparezca verde

#### Scenario: Job Linux compila el proyecto Avalonia
- **WHEN** se ejecuta el job `verify-linux`
- **THEN** `GitExtensions.slnx` (que incluye `GitExtensions.Avalonia.csproj`) se compila
  exitosamente en el runner Linux

#### Scenario: Job Linux publica artifacts al fallar
- **WHEN** el job `verify-linux` falla
- **THEN** la página del run ofrece un artifact con los `.trx` generados

### Requirement: Script Verify-Linux.ps1 compila el proyecto Avalonia
El script `eng/Verify-Linux.ps1` SHALL compilar la solución cross-platform `GitExtensions.slnx`
(que incluye `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj`) en lugar de una
lista hardcodeada de proyectos core.

#### Scenario: Proyecto Avalonia en lista de coreProjects
- **WHEN** se inspecciona `eng/Verify-Linux.ps1`
- **THEN** la variable `$solution` apunta a `GitExtensions.slnx` (que incluye
  `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj`) y no existe una lista
  `$coreProjects` hardcodeada

#### Scenario: Fallo de build detiene el script
- **WHEN** la compilación de `GitExtensions.slnx` falla en Linux
- **THEN** el script termina con código de error distinto de cero y muestra
  "VERIFY-LINUX FAILED"

## ADDED Requirements

### Requirement: La solución WinForms queda fuera del CI
El workflow `fork-ci.yml` y los scripts `eng/Verify.ps1` y `eng/Verify-Linux.ps1` SHALL NOT
compilar ni testear la solución WinForms (`GitExtensions.WinForms.slnx`) ni ningún proyecto
Windows-only (`GitUI`, la app `GitExtensions`, `GitExtUtils.WinForms`, `ResourceManager`,
`BugReporter`, externals o plugins). Ambos jobs SHALL validar la misma solución cross-platform.

#### Scenario: Ambos jobs compilan la solución cross-platform
- **WHEN** se ejecutan `verify-windows` y `verify-linux`
- **THEN** ambos compilan `GitExtensions.slnx` y ejecutan `GitCommands.Tests` con `-f net10.0`

#### Scenario: Sin proyectos Windows-only en CI
- **WHEN** se inspecciona `fork-ci.yml`, `eng/Verify.ps1` y `eng/Verify-Linux.ps1`
- **THEN** ninguno referencia `GitExtensions.WinForms.slnx`, `GitUI.csproj` ni
  `GitExtensions.csproj` (la app WinForms)
