## Why

La shell Avalonia es ahora el objetivo de desarrollo activo; la capa WinForms (`GitUI` y
app) queda solo como referencia para ir portando funcionalidad. Hoy el CI sigue validando la
solución WinForms completa en cada PR y la pata Linux es un "canary" asimétrico; además la
infraestructura de test del core arrastra WinForms (`Form`, `Application.DoEvents`,
`ResourceManager` → `GitExtUtils.WinForms`) mediante multitarget y `#if WINDOWS`. Este change
da a la solución cross-platform el estatus de primera clase y deja la infra de test del core
limpia, sin código condicional.

## What Changes

- Renombrar soluciones:
  - `GitExtensions.Avalonia.slnx` → **`GitExtensions.slnx`** (solución cross-platform primaria).
  - `GitExtensions.slnx` → **`GitExtensions.WinForms.slnx`** (solo referencia; ya no se compila/testea en CI).
- `eng/Verify.ps1` (Windows) y `eng/Verify-Linux.ps1` (Linux): ambos compilan `GitExtensions.slnx`
  (cross-platform) y ejecutan `GitCommands.Tests`, simétricos. Se elimina la lista hardcodeada
  `$coreProjects` y el flag `EnableWindowsTargeting` (ya no hay pata `net10.0-windows`).
- `.github/workflows/fork-ci.yml`: ambos jobs validan la misma solución cross-platform.
- Infraestructura de test del core desacoplada de WinForms:
  - `ConfigureJoinableTaskFactoryAttribute` pierde todo WinForms (`Form`, `Application.OnThreadException`,
    STA); usa un `SingleThreadSynchronizationContext` (thread dedicado) neutro para inicializar el
    `JoinableTaskContext`, así `SwitchToMainThreadAsync` funciona en `net10.0`.
  - `CommonTestUtils` y `GitCommands.Tests` pasan a **single `net10.0`** (sin pata `-windows`),
    eliminando el `#if WINDOWS` y la referencia a `ResourceManager`/`GitExtUtils.WinForms`.
  - Se elimina `WinFormsTestHelper` (`Application.DoEvents`).
  - `GitCommands.Tests` deja de referenciar `ResourceManager`: se quitan los 2 tests de
    `LocalizationHelpers` (pertenecen a `ResourceManager`); mover `LocalizationHelpers` al core
    (decisión 0.4, opción C) queda como follow-up.
  - Los tests legacy de XML se mantienen cross-platform (`FileFormatException` →
    `InvalidDataException` para no depender de `System.IO.Packaging`).
  - `AsyncLoaderTests` se re-incluye en `net10.0` (ya no necesita el message loop WinForms).

## Capabilities

### New Capabilities

- `solution-structure`: define la coexistencia de dos soluciones — `GitExtensions.slnx`
  (cross-platform, primaria, validada por CI) y `GitExtensions.WinForms.slnx` (WinForms,
  solo referencia, no validada).

### Modified Capabilities

- `continuous-integration`: el workflow valida únicamente la solución cross-platform en
  Windows y Linux, de forma simétrica.
- `local-verification`: `eng/Verify.ps1` compila la solución cross-platform y ejecuta solo los
  test projects cross-platform.
- `cross-platform-core`: la infraestructura de test se vuelve cross-platform y `GitCommands.Tests`
  pasa ~todos sus tests en `net10.0`.

## Impact

- Ficheros: renombres de soluciones, `eng/Verify.ps1`, `eng/Verify-Linux.ps1`,
  `.github/workflows/fork-ci.yml`, `tests/CommonTestUtils/{ConfigureJoinableTaskFactoryAttribute.cs,
  SingleThreadSynchronizationContext.cs, CommonTestUtils.csproj}`,
  `tests/app/UnitTests/GitCommands.Tests/{GitCommands.Tests.csproj, Properties/AssemblyInfo.cs,
  Git/Commands/GitCommandHelpersTest.cs, UserRepositoryHistory/Legacy/*}`.
- Se elimina `tests/CommonTestUtils/WinFormsTestHelper.cs`.
- Los test projects Windows-only (`GitUI.Tests`, `ResourceManager.Tests`, `BugReporter.Tests`,
  tests de plugins, `IntegrationTests`, `Extensibility.Tests`, `GitExtUtils.Tests`) dejan de
  compilar (eran consumidores de `CommonTestUtils`/`WinFormsTestHelper`): quedan como material
  de referencia, igual que `GitExtensions.WinForms.slnx`.
- Follow-up pendiente (no en este change): mover `LocalizationHelpers` a `GitCommands` para
  recuperar sus tests cross-platform.
