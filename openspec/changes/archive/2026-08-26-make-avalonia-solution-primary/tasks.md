## 1. Renombrar soluciones

- [x] 1.1 `git mv GitExtensions.slnx GitExtensions.WinForms.slnx`
- [x] 1.2 `git mv GitExtensions.Avalonia.slnx GitExtensions.slnx`
- [x] 1.3 Actualizar el comentario de cabecera de `GitExtensions.slnx`

## 2. Reescritura de eng/Verify.ps1

- [x] 2.1 Apuntar el build a la solución cross-platform (`$solution = 'GitExtensions.slnx'`)
- [x] 2.2 Sustituir el descubrimiento recursivo por una lista explícita de test projects
  cross-platform (hoy solo `GitCommands.Tests`)
- [x] 2.3 Actualizar cabecera y comentarios del script

## 3. Reescritura de eng/Verify-Linux.ps1

- [x] 3.1 Cambiar `$solution` a `GitExtensions.slnx` y eliminar `$coreProjects`
- [x] 3.2 Eliminar el flag `EnableWindowsTargeting` (ya no hay pata `net10.0-windows`)

## 4. Workflow de CI

- [x] 4.1 Actualizar nombres/comentarios de `fork-ci.yml` (quitar "canary"; reflejar simetría)

## 5. Infraestructura de test cross-platform

- [x] 5.1 Crear `SingleThreadSynchronizationContext` (thread dedicado de bombeo) en `CommonTestUtils`
- [x] 5.2 `ConfigureJoinableTaskFactoryAttribute` sin WinForms (fuera `Form`, `Application`,
  `#if WINDOWS`, STA); inicializa el `JoinableTaskContext` con el contexto neutro
- [x] 5.3 `CommonTestUtils.csproj` a single `net10.0`; borrar `WinFormsTestHelper.cs`
- [x] 5.4 `GitCommands.Tests.csproj` a single `net10.0`; quitar referencia a `ResourceManager`
  y los `Compile Remove` condicionales
- [x] 5.5 `GitCommandHelpersTest.cs`: quitar los 2 tests de `LocalizationHelpers` y el
  `using ResourceManager`
- [x] 5.6 Tests legacy de XML: `FileFormatException` → `InvalidDataException` (evita
  `System.IO.Packaging`)
- [x] 5.7 `Properties/AssemblyInfo.cs`: aplicar `[assembly: ConfigureJoinableTaskFactory]` sin
  guarda `#if WINDOWS`

## 6. Verificación

- [x] 6.1 `dotnet build GitExtensions.slnx` en Windows compila con 0 errores (sin WinForms)
- [x] 6.2 `eng/Verify.ps1` ejecuta `GitCommands.Tests` en verde (3456 pasados, 0 fallos,
  1 omitido), incluyendo `AsyncLoaderTests`
- [x] 6.3 `openspec validate` sin errores
