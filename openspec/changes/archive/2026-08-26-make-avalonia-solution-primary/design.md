## Context

Hoy hay dos soluciones: `GitExtensions.slnx` (completa, WinForms + core + Avalonia) y
`GitExtensions.Avalonia.slnx` (cross-platform). El CI (`fork-ci.yml`) llama a `eng/Verify.ps1`
(Windows) y `eng/Verify-Linux.ps1` (Linux). La infra de test del core multitargetea
(`net10.0-windows;net10.0`) con `#if WINDOWS`, usa `Form`/`Application.DoEvents` y arrastra
`ResourceManager` → `GitExtUtils.WinForms`. El desarrollo pasa a Avalonia; la capa WinForms
queda como referencia de portado.

## Goals / Non-Goals

**Goals:**
- `GitExtensions.slnx` sea la solución cross-platform canónica, validada por CI en Win+Linux.
- Los dos jobs del CI sean simétricos: misma solución, misma suite.
- La infraestructura de test del core quede sin WinForms y sin código condicional.

**Non-Goals:**
- No se elimina ni se reescribe el código WinForms de producción (queda como referencia).
- No se mueve `LocalizationHelpers` al core (decisión 0.4-C) — es follow-up.
- No se unifica el CI en un único job con matrix.

## Decisions

1. **Renombrar por swap, no esconder.** `GitExtensions.Avalonia.slnx` → `GitExtensions.slnx`
   (primaria) y `GitExtensions.slnx` → `GitExtensions.WinForms.slnx` (referencia). El nombre
   `GitExtensions.slnx` lo cogen por defecto `dotnet build`, VS Code y Rider. *Alternativa
   descartada*: `.slnx.original` — deja de ser una solución válida y no es abrible en Rider.

2. **CI simétrico sobre `net10.0` en ambos SO.** Ambos jobs compilan `GitExtensions.slnx` y
   ejecutan `GitCommands.Tests`. *Alternativa descartada*: correr `net10.0-windows` en Windows —
   revalida infra que se está dejando atrás y rompe la simetría.

3. **`Verify.ps1` con lista explícita de test projects cross-platform.** Sustituye el
   descubrimiento recursivo de `*.csproj`; hoy solo hay un proyecto de test cross-platform.

4. **Renombres con `git mv`** para que Git conserve la historia.

5. **`SingleThreadSynchronizationContext` (thread dedicado) sustituye a `Form`/STA.** El
   `JoinableTaskContext` se crea con ese contexto neutro, de modo que `SwitchToMainThreadAsync`
   marshaliza las continuaciones al thread de bombeo sin WinForms. Se elimina el
   `DenyExecutionSynchronizationContext` (guardarraíl STA/WinForms) y todo el `#if WINDOWS`.

6. **`CommonTestUtils` y `GitCommands.Tests` pasan a single `net10.0`.** Desaparecen la pata
   `-windows`, el `#if WINDOWS`, `WinFormsTestHelper` y la referencia a `ResourceManager`/
   `GitExtUtils.WinForms`. Los 2 tests de `LocalizationHelpers` se quitan (testean `ResourceManager`);
   los tests legacy de XML se conservan con `InvalidDataException` en vez de `FileFormatException`
   (evita `System.IO.Packaging`). *Alternativa descartada* (deferida): mover `LocalizationHelpers`
   al core (0.4-C) para recuperar esos tests.

7. **Los test projects WinForms dejan de compilar** (consumidores de `CommonTestUtils`/
   `WinFormsTestHelper`). Son referencia, igual que `GitExtensions.WinForms.slnx`.

## Risks / Trade-offs

- **La solución WinForms y sus test projects dejan de compilar** → es material de consulta;
  no entran en CI.
- **Se pierden 2 tests de `LocalizationHelpers.GetRelativeDateString`** → la lógica sigue viva
  en `ResourceManager`; se recuperan cuando se mueva `LocalizationHelpers` al core (follow-up).
- **El `SingleThreadSynchronizationContext` crea un thread por test** (~3.4K tests) → overhead
  despreciable frente al coste de spawnear `git.exe`; verificado: la suite completa pasa en ~2,5 min.
- **`.vscode/tasks.json`**: la tarea `watch` apunta a `GitExtensions.slnx` (ahora cross-platform,
  correcto); `build-vc`/`translate` son manuales y quedan tal cual.

## Migration Plan

1. `git mv` de las dos soluciones.
2. `SingleThreadSynchronizationContext` + `ConfigureJoinableTaskFactoryAttribute` sin WinForms.
3. `CommonTestUtils` y `GitCommands.Tests` a single `net10.0`; borrar `WinFormsTestHelper`;
   quitar referencia a `ResourceManager` y los 2 tests de `LocalizationHelpers`;
   `FileFormatException` → `InvalidDataException`.
4. `eng/Verify.ps1` / `eng/Verify-Linux.ps1` / `fork-ci.yml`.
5. Verificar: `dotnet build GitExtensions.slnx` + `eng/Verify.ps1` verde; la PR activará ambos jobs.
