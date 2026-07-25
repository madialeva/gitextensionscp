## Why

La shell WinForms inicializa `JoinableTaskContext` con `WindowsFormsSynchronizationContext` creando un `new Form()` efímero. Esto da a los 323 usos de `ThreadHelper.` (`FileAndForget`, `SwitchToMainThreadAsync`) el contexto de sincronización que necesitan para volver al hilo principal. La shell Avalonia (1.1a) no tiene esta inicialización, y sin ella es imposible usar ningún código del core que dependa de `ThreadHelper`. Este cambio es el prerrequisito bloqueante para 1.1c (DI + delegates) y 1.2 (abrir repositorio).

Es la incógnita técnica de mayor riesgo de toda la Fase 1: hay que validar experimentalmente que `JoinableTaskContext` funciona con `AvaloniaSynchronizationContext`.

## What Changes

- El proyecto `GitExtensions.Avalonia` añade `PackageReference` a `Microsoft.VisualStudio.Threading`
- `App.axaml.cs` inicializa `ThreadHelper.JoinableTaskContext` en `OnFrameworkInitializationCompleted()` donde `AvaloniaSynchronizationContext` ya está instalado
- Se añade un botón de diagnóstico en `MainWindow` que ejecuta `ThreadHelper.FileAndForget` + `SwitchToMainThreadAsync` y actualiza un `TextBlock` para validar el flujo completo
- Opcionalmente se cablea `TaskManager.ExceptionReporter` para que las excepciones de fire-and-forget sean visibles
- La app WinForms no cambia — su inicialización de JTF sigue igual

## Capabilities

### New Capabilities
- `avalonia-threading`: Inicialización de `JoinableTaskContext` con `AvaloniaSynchronizationContext`, validando que `ThreadHelper.FileAndForget` y `SwitchToMainThreadAsync` funcionan correctamente en la shell Avalonia.

### Modified Capabilities
- `avalonia-shell`: El proyecto `GitExtensions.Avalonia` ahora referencia `Microsoft.VisualStudio.Threading`, inicializa `JoinableTaskContext` en `OnFrameworkInitializationCompleted()`, e incluye un botón de diagnóstico para validar el threading.

## Impact

- **NuGet**: El proyecto `GitExtensions.Avalonia` gana `PackageReference` a `Microsoft.VisualStudio.Threading` (versión ya centralizada en `Directory.Packages.props`: 17.13.61)
- **Ensamblados afectados**: `src/app/GitExtensions.Avalonia/App.axaml.cs`, `MainWindow.axaml` + `.cs`, `.csproj`
- **No afecta**: shell WinForms, core, tests — todos siguen igual
- **Riesgo**: Si `AvaloniaSynchronizationContext` no es compatible con `JoinableTaskContext`, este change lo detecta temprano y fuerza a buscar alternativas antes de añadir más dependencias
