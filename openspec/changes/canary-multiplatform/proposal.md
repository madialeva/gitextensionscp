# Change 0.4: canary multiplataforma — los ensamblados core compilan y pasan tests en Linux

## Why

Los changes 0.2 y 0.3 consiguieron que los tres ensamblados del core (`Extensibility`,
`GitExtUtils`, `GitCommands`) compilen con `UseWindowsForms=false`, pero los tres siguen
heredando el TFM `net10.0-windows` del `Directory.Build.props`. El "canary" cierra la Fase 0
probando que el core no solo compila sin WinForms en el papel, sino que corre en un sistema
operativo sin Windows: retarget a `net10.0` neutro, tests de `GitCommands` pasando en un runner
Linux en CI. Es la comprobación definitiva de que las dependencias del core están limpias —
si se nos coló algún acoplamiento invisible, el compilador en Linux lo señala. Tras esto, el
core está listo para que la Fase 1 construya encima la shell Avalonia.

## What Changes

- **BREAKING** `Extensibility`, `GitExtUtils` y `GitCommands` sobrescriben `TargetFramework` a
  `net10.0` (sin `-windows`), en lugar de heredar `net10.0-windows` del repo. `GitCommands`
  además fija `UseWindowsForms=false`.
- **BREAKING** Se elimina la referencia temporal `GitCommands → GitExtUtils.WinForms` (deuda
  explícita del 0.3). Los 4 call sites de `MessageBoxes` en `GitCommands` se reconectan a una
  abstracción neutra (delegate o interfaz) que la shell instala.
- `GitUIPluginInterfaces` también retargetea a `net10.0` con `UseWindowsForms=false`, porque
  `GitCommands` lo referencia y no puede depender de un ensamblado `-windows`. El proyecto no
  usa WinForms hoy (cero menciones de `System.Windows.Forms`) y el cambio es trivial.
- Se añade una pata `linux` al workflow de CI: compila los ensamblados retargeteados (o la
  solución parcial con ellos) y ejecuta los tests de `GitCommands` que no dependen de
  `ResourceManager`/WinForms. Los tests acoplados se filtran o se mueven a un proyecto
  Windows-only.
- La app WinForms sigue compilando y funcionando exactamente igual: los tres ensamblados
  pasan a ser `net10.0` y eso es compatible hacia arriba con los consumidores
  `net10.0-windows`; las abstracciones de notificación al usuario se instalan en el arranque
  de la shell (mismo patrón que `TaskManager.ExceptionReporter` en el 0.3).

## Capabilities

### New Capabilities

- `cross-platform-core`: Los ensamblados `Extensibility`, `GitExtUtils` y `GitCommands`
  compilan y pasan sus tests en un sistema operativo no Windows, demostrando que el core
  está libre de dependencias de plataforma.

### Modified Capabilities

- `core-dependencies`: Se eleva el guardarraíl: donde antes se verificaba "compila con
  `UseWindowsForms=false`", ahora se verifica "compila con TFM `net10.0` (sin `-windows`) y
  los tests de `GitCommands` pasan en Linux". Se elimina la referencia temporal
  `GitCommands → GitExtUtils.WinForms`.
- `continuous-integration`: El workflow de CI gana una pata Linux (`ubuntu-latest`) que
  compila y ejecuta los tests multiplataforma, junto a la pata Windows existente que sigue
  verificando la solución completa.

## Impact

- **Proyectos**: `Extensibility`, `GitExtUtils`, `GitCommands` y `GitUIPluginInterfaces`
  cambian de TFM (de heredar `net10.0-windows` a fijar `net10.0`). `GitCommands` gana
  `UseWindowsForms=false` y pierde la referencia a `GitExtUtils.WinForms`.
- **Código**: ~4 call sites de `MessageBoxes` en `GitCommands` se adaptan a una abstracción
  neutra; se añade un punto de instalación en la shell WinForms (similar a
  `TaskManager.ExceptionReporter`). Posible partición de `GitCommands.Tests` para separar
  tests Windows-only.
- **CI**: `.github/workflows/fork-ci.yml` gana un job `linux` (matrix o job separado) y
  posiblemente un nuevo script `eng/Verify-Linux.ps1` o parámetro del existente.
- **Riesgo medio-bajo**: el retarget en sí es cambiar cuatro líneas de csproj; la parte de
  diseño real es la abstracción de notificaciones al usuario y la partición de tests.
