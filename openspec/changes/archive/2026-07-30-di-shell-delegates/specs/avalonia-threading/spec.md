## ADDED Requirements

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
