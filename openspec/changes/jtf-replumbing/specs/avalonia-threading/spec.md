## ADDED Requirements

### Requirement: JoinableTaskContext inicializado con AvaloniaSynchronizationContext
La shell Avalonia SHALL inicializar `ThreadHelper.JoinableTaskContext` en
`OnFrameworkInitializationCompleted()` capturando `AvaloniaSynchronizationContext` como el
contexto de sincronización del hilo principal.

#### Scenario: JTC se inicializa durante el startup
- **WHEN** la aplicación Avalonia completa `OnFrameworkInitializationCompleted()`
- **THEN** `ThreadHelper.JoinableTaskContext` no es null
- **AND** `ThreadHelper.JoinableTaskContext.IsOnMainThread` es true

#### Scenario: JTC captura el SynchronizationContext de Avalonia
- **WHEN** se crea `new JoinableTaskContext()` en `OnFrameworkInitializationCompleted()`
- **THEN** `SynchronizationContext.Current` es una instancia de `AvaloniaSynchronizationContext`

### Requirement: FileAndForget y SwitchToMainThreadAsync funcionales
El sistema SHALL permitir lanzar trabajo asíncrono con `ThreadHelper.FileAndForget` y
volver al hilo principal con `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`.

#### Scenario: FileAndForget ejecuta trabajo en thread pool
- **WHEN** se invoca `ThreadHelper.FileAndForget(async () => { await Task.Delay(100); })`
- **THEN** la tarea se completa sin excepciones

#### Scenario: SwitchToMainThreadAsync vuelve al hilo UI
- **WHEN** una tarea lanzada con `FileAndForget` llama a
  `await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`
- **THEN** la continuación se ejecuta en el hilo principal
- **AND** `ThreadHelper.JoinableTaskContext.IsOnMainThread` es true en la continuación

#### Scenario: El botón de test actualiza la UI desde el hilo principal
- **WHEN** el usuario pulsa el botón "Probar ThreadHelper"
- **THEN** tras ~500ms el `TextBlock` muestra un timestamp actualizado

### Requirement: Dependencia Microsoft.VisualStudio.Threading
El proyecto `GitExtensions.Avalonia` SHALL referenciar `Microsoft.VisualStudio.Threading`
con la versión centralizada en `Directory.Packages.props`.

#### Scenario: PackageReference presente
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `PackageReference` a `Microsoft.VisualStudio.Threading`

### Requirement: ExceptionReporter mínimo para diagnóstico
El sistema SHALL cablear `TaskManager.ExceptionReporter` a un delegado que al menos
escriba las excepciones a `Debug.WriteLine` para diagnóstico durante el desarrollo.

#### Scenario: Excepción en FileAndForget se registra
- **WHEN** una tarea lanzada con `FileAndForget` lanza una excepción
- **THEN** la excepción se escribe a la salida de debug vía `Debug.WriteLine`
