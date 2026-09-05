## Why

La shell Avalonia ya arranca con Fluent, MSDI, `JoinableTaskContext` y los delegates de plataforma, pero todavía no ofrece el primer flujo útil: abrir un repositorio y mostrar que el core Git funciona bajo Avalonia. La ventana actual tampoco define la composición visual sobre la que crecerán las siguientes vistas.

Este change implementa el slice 1.2 y absorbe el trabajo pendiente del issue #20 mediante pruebas headless del arranque, DI, threading y delegates. El issue de seguimiento de este change es [#23](https://github.com/madialeva/gitextensionscp/issues/23), asociado al milestone `Fase 1 — Walking skeleton Avalonia`.

La composición visual toma como referencia la aplicación JavaFX disponible en `/home/arume/src/arume`: una ventana tipo IDE con barra superior, rail izquierdo y derecho, área central y barra inferior. La referencia guía la estructura y densidad visual, pero las acciones y datos serán propios de GitExtensions.

## What Changes

- Añadir el flujo Avalonia para seleccionar un repositorio reciente o explorar una carpeta mediante el picker existente.
- Validar la carpeta seleccionada como repositorio Git, cargar su información básica y mostrar ruta, rama actual, remotes y estado del working tree, incluyendo estados de carga, cancelación, carpeta inválida y error.
- Reemplazar la ventana vacía por una shell principal estable con title bar superior, rail de navegación izquierdo, rail de acciones derecho, área central y status bar inferior.
- Desactivar las decoraciones nativas de la ventana y proporcionar en la barra superior propia los controles funcionales de minimizar, maximizar/restaurar y cerrar, para mantener el mismo chrome visual en Windows y Linux.
- Usar acciones icon-only en los rails, con tooltips accesibles y comportamiento funcional únicamente para acciones implementadas en este change.
- Añadir pruebas headless deterministas para startup, servicios registrados, `JoinableTaskContext`/`AvaloniaSynchronizationContext` y `ExceptionReporter`, `UserMessageHandler.ShowError` y `OsShellUtil.PickFolder`, sin mostrar diálogos reales.
- Mantener la shell en `net10.0`, multiplataforma y sin referencias a WinForms; conservar la solución WinForms como referencia compilable.

## Capabilities

### New Capabilities

- `avalonia-repository-opening`: Selección, validación y apertura de repositorios recientes o elegidos mediante el selector de carpetas, con información Git básica y estados de UI.
- `avalonia-main-shell`: Composición visual IDE-like de la ventana principal con barras superior e inferior, rails laterales y acciones icon-only accesibles.
- `avalonia-headless-testing`: Cobertura headless del bootstrap Avalonia, DI, threading y delegates de plataforma sin WinForms ni diálogos reales.

### Modified Capabilities

- `avalonia-shell`: La ventana deja de ser vacía y pasa a alojar la shell persistente y las vistas del flujo de apertura de repositorio.
- `avalonia-di`: Los servicios del core se consumen desde ViewModels de la shell para resolver repositorios sin reintroducir un Service Locator en la lógica de presentación nueva.

## Impact

- `src/app/GitExtensions.Avalonia`: nuevas vistas, ViewModels, servicios de repositorio, recursos XAML y estilos de la shell principal.
- `tests/app`: nuevo proyecto o extensión de tests Avalonia headless incluido en `GitExtensions.slnx`, con dependencias portables y dobles de prueba para los delegates.
- `openspec/changes/open-repository-main-shell`: especificaciones, diseño y tareas de implementación de #23.
- La implementación reutilizará `GitCommands`, `GitExtUtils`, `RepositoryHistoryManager`, `IGitModule` y `OsShellUtil` donde sus contratos actuales encajen; no modificará la API pública de plugins.
