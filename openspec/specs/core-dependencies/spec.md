# core-dependencies Specification

## Purpose
Las dependencias del core (`GitCommands`): los ensamblados que el core referencia
(`GitExtUtils`, además de `GitExtensions.Extensibility`, cubierta por `plugin-api`) deben
mantenerse libres de tecnología de UI para que el core pueda retargetearse a `net10.0`
neutro y compilar en Linux/macOS. El código Windows/WinForms extraído vive en el ensamblado
separado `GitExtUtils.WinForms`, que solo consumen las capas de UI.
## Requirements
### Requirement: GitExtUtils sin WinForms
El ensamblado `GitExtUtils` SHALL contener únicamente utilidades neutras respecto a la
tecnología de UI (sin tipos de `System.Windows.Forms`, sin GDI+ más allá de
System.Drawing.Primitives, sin P/Invoke a APIs de UI de Windows) y SHALL compilar con
`UseWindowsForms=false` como verificación permanente.

#### Scenario: Guardarraíl de compilación
- **WHEN** se compila la solución con `GitExtUtils` configurado con `UseWindowsForms=false`
- **THEN** la compilación termina sin errores

#### Scenario: Regresión de acoplamiento
- **WHEN** un cambio futuro introduce un tipo WinForms o un interop de UI en `GitExtUtils`
- **THEN** el proyecto no compila y el error señala el tipo ofensor

### Requirement: Interops y utilidades WinForms en ensamblado Windows-only
El código Windows-only extraído (interops Win32, extensiones de controles WinForms, DpiUtil,
theming GDI+, `ClipboardUtil`, `MessageBoxes`, `FontParser`, `UIExtensions`) SHALL residir
en un ensamblado separado (`GitExtUtils.WinForms`) que solo referencian las capas de UI y —
temporalmente, hasta el change 0.4 — `GitCommands`.

#### Scenario: Consumidores compilan sin cambios de código
- **WHEN** se recompila la solución tras la extracción
- **THEN** los consumidores existentes (GitUI, ResourceManager, plugins, tests) compilan sin
  cambios en sus ficheros fuente (los tipos movidos conservan su namespace)

### Requirement: Utilidades de threading neutras permanecen en la base
Las utilidades de threading que `GitCommands` consume (`ThreadHelper` — núcleo,
`TaskManager`, `CancellationTokenSequence`, `ExclusiveTaskRunner`) SHALL permanecer en
`GitExtUtils` sin dependencia de WinForms; las extensiones de esas utilidades que operan
sobre `Control` SHALL moverse al ensamblado Windows-only.

#### Scenario: El core no ve WinForms a través del threading
- **WHEN** `GitCommands` usa `ThreadHelper`/`TaskManager`/`CancellationTokenSequence`
- **THEN** ninguna de esas APIs expone ni requiere tipos WinForms

### Requirement: Paridad funcional
La extracción SHALL NOT cambiar el comportamiento de la aplicación WinForms.

#### Scenario: Verificación completa
- **WHEN** se ejecuta `eng/Verify.ps1` tras completar el change
- **THEN** la build está limpia y todos los proyectos de unit tests pasan

#### Scenario: Smoke test de zonas sensibles
- **WHEN** se arranca la app y se recorren las zonas que tocan lo movido: theming (tema
  claro/oscuro), escalado DPI, copiar al portapapeles desde la parrilla de commits
- **THEN** todo funciona igual que antes del change
