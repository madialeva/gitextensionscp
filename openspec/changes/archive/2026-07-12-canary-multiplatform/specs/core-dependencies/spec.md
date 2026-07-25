# core-dependencies Specification (delta)

## MODIFIED Requirements

### Requirement: GitExtUtils sin WinForms
El ensamblado `GitExtUtils` SHALL compilar y funcionar con `TargetFramework=net10.0` y
`UseWindowsForms=false`. La restricción se eleva de "compila sin tipos de UI" (change 0.3) a
"compila sin el runtime de Windows", verificada con el TFM neutro.

#### Scenario: Guardarraíl de compilación
- **WHEN** se compila la solución con `GitExtUtils` configurado con `TargetFramework=net10.0`
  y `UseWindowsForms=false`
- **THEN** la compilación termina sin errores

#### Scenario: Regresión de acoplamiento
- **WHEN** un cambio futuro introduce un tipo WinForms, un interop de UI, o una referencia a
  un ensamblado Windows-only en `GitExtUtils`
- **THEN** el proyecto no compila y el error señala el tipo o referencia ofensor

### Requirement: Interops y utilidades WinForms en ensamblado Windows-only
El código Windows-only extraído (interops Win32, extensiones de controles WinForms, DpiUtil,
theming GDI+, `ClipboardUtil`, `MessageBoxes`, `FontParser`, `UIExtensions`) SHALL residir
en un ensamblado separado (`GitExtUtils.WinForms`) que solo referencian las capas de UI.
`GitCommands` SHALL NOT referenciar este ensamblado tras el change 0.4.

#### Scenario: Consumidores compilan sin cambios de código
- **WHEN** se recompila la solución tras la extracción
- **THEN** los consumidores existentes (GitUI, ResourceManager, plugins, tests) compilan sin
  cambios en sus ficheros fuente (los tipos movidos conservan su namespace)

#### Scenario: El core no referencia el ensamblado WinForms
- **WHEN** se inspeccionan las referencias de proyecto de `GitCommands`
- **THEN** no existe dependencia hacia `GitExtUtils.WinForms`
