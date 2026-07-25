# avalonia-threading Specification

## Purpose
Inicialización de `JoinableTaskContext` con `AvaloniaSynchronizationContext` en la shell
Avalonia, validando que `ThreadHelper.FileAndForget` y `SwitchToMainThreadAsync` funcionan
correctamente. Establecida por el change 1.1b (`jtf-replumbing`).

## Requirements
### Requirement: JoinableTaskContext inicializado con AvaloniaSynchronizationContext
La shell Avalonia SHALL inicializar `ThreadHelper.JoinableTaskContext` en
`OnFrameworkInitializationCompleted()` capturando `AvaloniaSynchronizationContext` como el
contexto de sincronizacion del hilo principal.

#### Scenario: JTC se inicializa durante el startup
- **WHEN** la aplicacion Avalonia completa `OnFrameworkInitializationCompleted()`
- **THEN** `ThreadHelper.JoinableTaskContext` no es null
- **AND** `ThreadHelper.JoinableTaskContext.IsOnMainThread` es true

#### Scenario: JTC captura el SynchronizationContext de Avalonia
- **WHEN** se crea `new JoinableTaskContext()` en `OnFrameworkInitializationCompleted()`
- **THEN** `SynchronizationContext.Current` es una instancia de `AvaloniaSynchronizationContext`

### Requirement: FileAndForget y SwitchToMainThreadAsync funcionales
El sistema SHALL permitir lanzar trabajo asincrono con `ThreadHelper.FileAndForget` y
volver al hilo principal con `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`.

#### Scenario: FileAndForget ejecuta trabajo en thread pool
- **WHEN** se invoca `ThreadHelper.FileAndForget(async () => { await Task.Delay(100); })`
- **THEN** la tarea se completa sin excepciones

#### Scenario: SwitchToMainThreadAsync vuelve al hilo UI
- **WHEN** una tarea lanzada con `FileAndForget` llama a
  `await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`
- **THEN** la continuacion se ejecuta en el hilo principal
- **AND** `ThreadHelper.JoinableTaskContext.IsOnMainThread` es true en la continuacion

### Requirement: Acceso interno a GitExtUtils
El proyecto `GitExtensions.Avalonia` SHALL tener acceso a miembros internos de `GitExtUtils`
via `InternalsVisibleTo` para poder asignar `ThreadHelper.JoinableTaskContext`.

#### Scenario: InternalsVisibleTo declarado
- **WHEN** se inspecciona `GitExtUtils/Properties/AssemblyInfo.cs`
- **THEN** contiene `[assembly: InternalsVisibleTo("GitExtensions.Avalonia")]`

#### Scenario: ThreadHelper.JoinableTaskContext asignable
- **WHEN** el codigo de `App.axaml.cs` asigna `ThreadHelper.JoinableTaskContext = new JoinableTaskContext()`
- **THEN** la compilacion tiene exito
