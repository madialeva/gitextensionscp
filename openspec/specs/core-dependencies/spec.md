# core-dependencies Specification

## Purpose
Las dependencias del core (`GitCommands`): los ensamblados que el core referencia
(`GitExtUtils`, además de `GitExtensions.Extensibility`, cubierta por `plugin-api`) deben
mantenerse libres de tecnología de UI para que el core pueda targetear `net10.0` neutro y
compilar y pasar tests en Linux/macOS. El código Windows/WinForms extraído vive en los
ensamblados separados `GitExtUtils.WinForms` (interops, utilidades de UI) y
`ResourceManager`/`GitUI` (shell WinForms), que solo consumen las capas de UI.

## Requirements
### Requirement: GitExtUtils compila y funciona sin Windows
El ensamblado `GitExtUtils` SHALL targetear `net10.0` (sin `-windows`) con
`UseWindowsForms=false` como verificación permanente. Las notificaciones de UI desde el core
(errores, avisos) SHALL usar delegates instalables por la shell (patrón
`UserMessageHandler.ShowError`, mismo que `TaskManager.ExceptionReporter`), sin depender de
tipos WinForms.

#### Scenario: Guardarraíl de compilación multiplataforma
- **WHEN** se compila `GitExtUtils` con `TargetFramework=net10.0` y `UseWindowsForms=false`
  en cualquier sistema operativo soportado por .NET
- **THEN** la compilación termina sin errores

#### Scenario: Regresión de acoplamiento
- **WHEN** un cambio futuro introduce un tipo WinForms, un interop de UI, o una referencia a
  un ensamblado Windows-only en `GitExtUtils`
- **THEN** el proyecto no compila y el error señala el tipo o referencia ofensor

### Requirement: Interops y utilidades WinForms en ensamblado Windows-only
El código Windows-only extraído (interops Win32, extensiones de controles WinForms, DpiUtil,
theming GDI+, `ClipboardUtil`, `MessageBoxes`) SHALL residir en un ensamblado separado
(`GitExtUtils.WinForms`) que solo referencian las capas de UI. `GitCommands` SHALL NOT
referenciar este ensamblado.

#### Scenario: Consumidores compilan sin cambios de código
- **WHEN** se recompila la solución tras la extracción
- **THEN** los consumidores existentes (GitUI, ResourceManager, plugins, tests) compilan sin
  cambios en sus ficheros fuente (los tipos movidos conservan su namespace)

#### Scenario: El core no referencia el ensamblado WinForms
- **WHEN** se inspeccionan las referencias de proyecto de `GitCommands`
- **THEN** no existe dependencia hacia `GitExtUtils.WinForms`

### Requirement: Utilidades de threading y notificaciones neutras en la base
Las utilidades de threading que `GitCommands` consume (`ThreadHelper` — núcleo,
`TaskManager`, `CancellationTokenSequence`, `ExclusiveTaskRunner`) SHALL permanecer en
`GitExtUtils` sin dependencia de WinForms; las extensiones sobre `Control` SHALL residir en
el ensamblado Windows-only. Los avisos al usuario desde el core SHALL usar delegates
estáticos (`UserMessageHandler.ShowError`) instalados por la shell.

#### Scenario: El core no ve WinForms a través del threading
- **WHEN** `GitCommands` usa `ThreadHelper`/`TaskManager`/`CancellationTokenSequence`
- **THEN** ninguna de esas APIs expone ni requiere tipos WinForms

#### Scenario: Core emite aviso sin shell
- **WHEN** el core emite un aviso en un entorno donde la shell no ha instalado el delegate
  (tests unitarios, app headless)
- **THEN** el aviso se registra en trace sin producir excepciones ni bloqueos

### Requirement: Paridad funcional
Los cambios de desacoplamiento SHALL NOT cambiar el comportamiento de la aplicación WinForms.

#### Scenario: Verificación completa en Windows
- **WHEN** se ejecuta `eng/Verify.ps1` tras cualquier change en esta capability
- **THEN** la build está limpia y los 15 proyectos de unit tests pasan

#### Scenario: Verificación en Linux
- **WHEN** se ejecuta `eng/Verify-Linux.sh` en un runner Linux
- **THEN** los ensamblados core compilan y los tests de `GitCommands` (subset `net10.0`) pasan
