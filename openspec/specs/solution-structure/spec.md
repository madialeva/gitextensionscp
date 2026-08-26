# solution-structure Specification

## Purpose
Define la coexistencia de dos soluciones en el repositorio — una cross-platform primaria
validada por CI y una WinForms de solo referencia — para que el desarrollo Avalonia tenga una
solución de primera clase sin perder el código WinForms que se va portando.

## Requirements

### Requirement: Solución cross-platform primaria
El repositorio SHALL contener un fichero de solución `GitExtensions.slnx` en la raíz que liste
únicamente proyectos cross-platform: los ensamblados `net10.0` (`GitExtensions.Extensibility`,
`GitExtUtils`, `GitCommands`, `GitUIPluginInterfaces`, `GitExtensions.Avalonia`) y los
proyectos de test con pata `net10.0` (`CommonTestUtils`, `GitCommands.Tests`). SHALL NOT listar
proyectos Windows-only (`GitUI`, la app `GitExtensions`, `GitExtUtils.WinForms`,
`ResourceManager`, `BugReporter`, externals, plugins o el instalador WiX).

#### Scenario: Listado de proyectos
- **WHEN** se inspeccionan los proyectos de `GitExtensions.slnx`
- **THEN** figuran `GitExtensions.Avalonia` y los ensamblados core `net10.0`
- **AND** no figura ningún proyecto con TFM `-windows`

### Requirement: Solución WinForms de solo referencia
El repositorio SHALL conservar un fichero `GitExtensions.WinForms.slnx` en la raíz que contenga
la solución WinForms completa (app `GitExtensions`, `GitUI`, `GitExtUtils.WinForms`,
`ResourceManager`, externals y plugins) como material de consulta para el portado. Esta
solución SHALL NOT ser compilada ni testeada por `eng/Verify.ps1`, `eng/Verify-Linux.ps1` ni
por el workflow de CI (`fork-ci.yml`).

#### Scenario: La solución de referencia existe
- **WHEN** se inspecciona la raíz del repositorio
- **THEN** existe `GitExtensions.WinForms.slnx` listando los proyectos WinForms (p. ej. `GitUI`)

#### Scenario: Fuera del CI
- **WHEN** se inspeccionan `eng/Verify.ps1`, `eng/Verify-Linux.ps1` y `.github/workflows/fork-ci.yml`
- **THEN** ninguno referencia `GitExtensions.WinForms.slnx`
