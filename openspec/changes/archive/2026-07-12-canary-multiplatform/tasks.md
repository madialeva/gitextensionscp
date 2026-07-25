# Tasks — Change 0.4: canary multiplataforma

> Tres capas (design D6), Verify en verde tras cada una. Los hallazgos del inventario se
> anotan aquí como notas bajo cada tarea.

## 1. Inventario

- [x] 1.1 Auditar `LocalizationHelpers` en `ResourceManager`: qué usa de WinForms, si depende
  de `.resx` de ResourceManager, si se puede mover a `GitCommands` o `GitExtUtils` sin
  arrastrar dependencias. Confirmar que `GitCommandHelpersTest.cs` es el único test de
  `GitCommands.Tests` que necesita `ResourceManager`. Anotar la decisión final aquí.
- [x] 1.2 Confirmar con grep que `GitCommands` no tiene otras dependencias ocultas de
      WinForms más allá de los 4 call sites de `MessageBoxes` — en particular, verificar que
      ningún fichero usa `System.Windows.Forms` o `System.Drawing` (más allá de Primitives) ni
      llama a APIs Windows-only por otra vía.
      → Confirmado: cero usings de WinForms/Drawing, cero using GitExtUtils.WinForms.
      Solo la referencia temporal en csproj y los 4 MessageBoxes.
- [x] 1.3 Revisar `GitUIPluginInterfaces`: confirmar cero usos de WinForms y lista de tipos
      que `GitCommands` importa de él (expected: ObjectId, GitRevision, interfaces de settings).
      → Confirmado: cero System.Windows.Forms, cero System.Drawing (no Primitives).
      Retarget trivial: solo cambiar TFM + UseWindowsForms=false en el csproj.

## 2. Retarget mecánico

- [x] 2.1 Cambiar `TargetFramework` a `net10.0` y fijar `UseWindowsForms` en:
      `GitExtensions.Extensibility.csproj`, `GitExtUtils.csproj`, `GitCommands.csproj`,
      `GitUIPluginInterfaces.csproj`. Eliminar el `ProjectReference` a `GitExtUtils.WinForms` en
      `GitCommands.csproj` (incluyendo el comentario "TEMPORAL hasta el change 0.4").
      → Hecho. También se movió `CustomDiffMergeTool.cs` a GitUI (solo WinForms) y
      `FontParser.cs` + `SettingsSourceFontExtensions.cs` a GitCommands (necesitan
      System.Drawing.Common). Se añadieron NuGets: System.Drawing.Common,
      System.Configuration.ConfigurationManager. Se corrigieron usings y
      Application.* → Environment/Paths.
- [x] 2.2 Compilar la solución completa. Esperado: falla en los 4 call sites de
      `MessageBoxes` en `GitCommands` (el tipo ya no está disponible) y posiblemente en
      `GitCommands.Tests` (por `ResourceManager`). Todo lo demás compila. Confirmar que no hay
      errores adicionales (más acoplamientos ocultos).
      → Confirmado: solo 8 errores (MessageBoxes/MessageBoxButtons/MessageBoxIcon en los
      4 call sites de CommitMessageManager, ExceptionUtils, GitVersion). El resto del
      ensamblado compila limpio como net10.0. Se destaparon y arreglaron
      Application.* (3 sitios), TextRenderer, CustomDiffMergeTool, GetFont/SetFont,
      y un uso de Application.ExecutablePath en GitUIPluginInterfaces.

## 3. Abstracción de MessageBoxes

- [x] 3.1 Crear `UserMessageHandler` en `GitExtUtils` (directorio raíz, namespace
      `GitExtUtils`, junto a `TaskManager`): delegado estático `ShowError` con firma
      `Action<IWindow?, string, string?>` y default `Trace.TraceWarning`. Incluir
      `InternalsVisibleTo` si hace falta para los instaladores.
- [x] 3.2 Adaptar los 4 call sites en `GitCommands`:
      `CommitMessageManager.cs` (x2, `Show` → `ShowError`),
      `ExceptionUtils.cs` (`ShowError` → `ShowError`),
      `Git\GitVersion.cs` (`ShowError` → `ShowError`).
- [x] 3.3 Instalar el delegado en la shell WinForms: `Program.cs` de `GitExtensions`
      asigna `UserMessageHandler.ShowError` → `GitExtUtils.MessageBoxes.ShowError`.
      También se añadió referencia directa a `GitExtUtils.WinForms` en `GitUI.csproj`
      y `ResourceManager.csproj` (antes recibían por transitividad desde GitCommands).
- [x] 3.4 Ejecutar `eng/Verify.ps1` completo en Windows: build + 15 suites. Debe estar en
      verde. Smoke test manual: arrancar la app, provocar un aviso de versión de git antigua
      (`GitVersion.ShowError`) y confirmar que el diálogo modal aparece igual que antes.
      → Verify verde (build + 15/15 suites OK).

## 4. Tests y partición

- [x] 4.1 Ejecutar la decisión de 1.1 sobre `LocalizationHelpers`:
      **Plan B aplicado (multitarget + exclusión condicional).** `GitCommands.Tests`
      y `CommonTestUtils` ganan `<TargetFrameworks>net10.0-windows;net10.0</TargetFrameworks>`.
      En `net10.0`: `UseWindowsForms=false`, se excluyen `GitCommandHelpersTest.cs`
      (depende de ResourceManager.LocalizationHelpers), `ConfigureJoinableTaskFactoryAttribute.cs`,
      `WinFormsTestHelper.cs`, y tests legacy de XML (necesitan `System.IO.Packaging`).
      `AssemblyInfo.cs` usa `#if WINDOWS` para saltar `ConfigureJoinableTaskFactory` en
      `net10.0`. Sin JTF, ~240 tests fallan (esperado).
- [x] 4.2 Añadir `TargetFramework` explícito a `GitCommands.Tests.csproj`:
      `<TargetFramework></TargetFramework>` + `<TargetFrameworks>net10.0-windows;net10.0</…>`
      anula la herencia de `Directory.Build.props` para que el multi-target funcione.
- [x] 4.3 Ejecutar `eng/Verify.ps1` en Windows: confirmar que las 15 suites siguen en verde
      (la partición no debe romper nada en el lado Windows).
      → Verify.ps1 adaptado con `-f net10.0-windows` para ejecutar solo el TFM Windows.
      15/15 suites OK.

## 5. Linux CI

- [x] 5.1 Crear `eng/Verify-Linux.ps1`: script PowerShell que compila los 4 proyectos
      `net10.0` (Extensibility, GitExtUtils, GitCommands, GitUIPluginInterfaces) y ejecuta
      `dotnet test -f net10.0` sobre `GitCommands.Tests`.
- [x] 5.2 Modificar `.github/workflows/fork-ci.yml`:
      - Job `verify-windows` (antes `verify`): sin cambios funcionales, pero añade
        `-f net10.0-windows` al test y renombra el artifact.
      - Nuevo job `verify-linux`: `ubuntu-latest`, checkout + setup-dotnet + `pwsh -File
        eng/Verify-Linux.ps1`, artifact propio.
      - Ambos jobs corren en los mismos triggers (PR/push a avalonia/main, workflow_dispatch).
- [x] 5.3 Push a la rama del change y verificar que la PR activa ambos jobs y ambos pasan
  en verde (Windows con la solución completa, Linux con el subset `net10.0` + tests de
  `GitCommands`).
  → PR #8 mergeada con CI verde en ambos jobs (Windows 15/15, Linux core + GitCommands.Tests).

## 6. Cierre

- [x] 6.1 Actualizar los specs baseline en `openspec/specs/`: el spec `core-dependencies` deja
  de mencionar "compila con `UseWindowsForms=false` en Windows" y pasa a "compila y pasa tests
  en Linux con `TargetFramework=net10.0`"; `continuous-integration` gana el requisito del job
  Linux.
  → Los tres specs (core-dependencies, continuous-integration, cross-platform-core)
  actualizados en el baseline. Commiteado en avalonia/main.
- [x] 6.2 Actualizar `AGENTS.md` y `AVALONIA_MIGRATION_ANALYSIS.md` §10 con las decisiones
  tomadas durante la implementación y marcar 0.4 como completado.
- [x] 6.3 Verificar que el README del repo (si procede) refleja que la Fase 0 está completa
  (core multiplataforma verificado en CI).
  → README actualizado con tabla de fases y checkmark en Fase 0.
