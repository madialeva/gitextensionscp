# avalonia-threading Specification

## Purpose
Inicialización de `JoinableTaskContext` con `AvaloniaSynchronizationContext` en la shell
Avalonia, validando que `ThreadHelper.FileAndForget` y `SwitchToMainThreadAsync` funcionan
correctamente. Establecida por el change 1.1b (`jtf-replumbing`); ampliada con el cableado
de `ExceptionReporter` a dialogo Avalonia en el change 1.1c (`di-shell-delegates`).

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

### Requirement: TaskManager.ExceptionReporter cableado a dialogo Avalonia
La shell Avalonia SHALL instalar un handler en `TaskManager.ExceptionReporter` que muestre
la excepcion demystificada en un dialogo modal Avalonia, en lugar del comportamiento por
defecto que solo escribe a `Trace.TraceError`.

#### Scenario: Excepcion de FileAndForget se muestra al usuario
- **WHEN** una tarea lanzada con `ThreadHelper.FileAndForget` lanza una excepcion
- **THEN** se abre una ventana modal con titulo "Error" mostrando el mensaje y stack trace

#### Scenario: El handler se instala durante el startup
- **WHEN** la aplicacion completa `OnFrameworkInitializationCompleted()`
- **THEN** `TaskManager.ExceptionReporter` referencia un delegate que muestra un dialogo Avalonia
