# avalonia-di Specification

## Purpose
Contenedor de inyeccion de dependencias y cableado de delegates de shell en la shell
Avalonia. El contenedor MSDI registra los servicios del core (`GitExtUtils`, `GitCommands`)
y los tres delegates de shell (`ExceptionReporter`, `ShowError`, `PickFolder`) se cablean
a dialogos nativos de Avalonia. Establecida por el change 1.1c (`di-shell-delegates`).

## Requirements
### Requirement: Contenedor MSDI en shell Avalonia
La shell Avalonia SHALL construir un `IServiceProvider` de Microsoft.Extensions.DependencyInjection en `OnFrameworkInitializationCompleted()`, registrando los servicios de `GitExtUtils` y `GitCommands`, y almacenarlo en una propiedad estatica accesible para VMs y views.

#### Scenario: El contenedor se construye durante el startup
- **WHEN** la aplicacion Avalonia completa `OnFrameworkInitializationCompleted()`
- **THEN** `App.ServiceProvider` no es null
- **AND** es una instancia de `ServiceProvider` de MSDI

#### Scenario: Servicios del core estan registrados
- **WHEN** se resuelve `IGitDirectoryResolver` del `App.ServiceProvider`
- **THEN** se obtiene una instancia no nula de `GitDirectoryResolver`

#### Scenario: Servicios de GitCommands estan registrados
- **WHEN** se resuelve `IGitExecutorProvider` del `App.ServiceProvider`
- **THEN** se obtiene una instancia no nula de `GitExecutorProvider`

### Requirement: TaskManager.ExceptionReporter cableado
La shell Avalonia SHALL instalar un handler en `TaskManager.ExceptionReporter` que muestre la excepcion demystificada en un dialogo modal Avalonia con titulo "Error" y el mensaje + stack trace en un area de texto.

#### Scenario: Excepcion de FileAndForget se muestra al usuario
- **WHEN** una tarea lanzada con `ThreadHelper.FileAndForget` lanza una excepcion
- **THEN** se abre una ventana modal con titulo "Error"
- **AND** el contenido incluye el mensaje de la excepcion y el stack trace

### Requirement: UserMessageHandler.ShowError cableado
La shell Avalonia SHALL instalar un handler en `UserMessageHandler.ShowError` que muestre un dialogo modal Avalonia con el titulo (caption) y texto proporcionados, usando el owner `IWindow` si esta disponible o la ventana principal por defecto.

#### Scenario: Mensaje de error del core se muestra al usuario
- **WHEN** el core invoca `UserMessageHandler.ShowError(owner, "mensaje", "titulo")`
- **THEN** se abre una ventana modal con titulo "titulo"
- **AND** el contenido incluye "mensaje"

#### Scenario: Owner nulo usa la ventana principal
- **WHEN** el core invoca `UserMessageHandler.ShowError(null, "mensaje", null)`
- **THEN** se abre una ventana modal con el titulo por defecto "Error"
- **AND** la ventana se muestra como hija de la ventana principal

### Requirement: OsShellUtil.PickFolder cableado
La shell Avalonia SHALL instalar un handler en `OsShellUtil.PickFolder` que abra el selector de carpetas nativo via `IStorageProvider.OpenFolderPickerAsync`, ejecutado con `JoinableTaskFactory.Run` para hacer el bridge sync→async desde el hilo UI.

#### Scenario: Seleccion de carpeta devuelve ruta
- **WHEN** el core invoca `OsShellUtil.PickFolder(owner, null)` desde el hilo UI
- **AND** el usuario selecciona una carpeta en el dialogo nativo
- **THEN** el resultado es la ruta absoluta de la carpeta seleccionada

#### Scenario: Cancelacion devuelve null
- **WHEN** el core invoca `OsShellUtil.PickFolder(owner, "C:\\some\\path")` desde el hilo UI
- **AND** el usuario cancela el dialogo
- **THEN** el resultado es null

### Requirement: Adaptador IWindow a Window de Avalonia
La shell Avalonia SHALL proporcionar un adaptador `AvaloniaWindowAdapter` que implemente `IWindow` envolviendo un `Avalonia.Controls.Window`, permitiendo resolver el `Window` original cuando los delegates reciben un owner `IWindow?`.

#### Scenario: Window se adapta a IWindow
- **WHEN** se crea `new AvaloniaWindowAdapter(window)` donde `window` es un `Window` de Avalonia
- **THEN** el adaptador implementa `IWindow`
- **AND** la propiedad `Window` devuelve la instancia original

#### Scenario: ResolveOwner extrae Window
- **WHEN** se pasa un `IWindow?` que es un `AvaloniaWindowAdapter` a `ResolveOwner`
- **THEN** devuelve el `Window` envuelto

#### Scenario: IWindow extrano devuelve null
- **WHEN** se pasa un `IWindow?` que NO es un `AvaloniaWindowAdapter` a `ResolveOwner`
- **THEN** devuelve null (se usara la ventana principal por defecto)

### Requirement: Proyecto referencia GitCommands
El proyecto `GitExtensions.Avalonia` SHALL tener un `ProjectReference` a `GitCommands` para poder registrar sus servicios y usar tipos como `IGitDirectoryResolver` e `IGitExecutorProvider`.

#### Scenario: Referencia a GitCommands en csproj
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `ProjectReference` a `..\GitCommands\GitCommands.csproj`

#### Scenario: Compilacion con GitCommands
- **WHEN** se compila `GitExtensions.Avalonia`
- **THEN** la compilacion tiene exito y los tipos de `GitCommands` estan disponibles

### Requirement: Sin referencia a GitUI
El proyecto `GitExtensions.Avalonia` SHALL NOT referenciar `GitUI` ni `GitExtUtils.WinForms` ni ningun ensamblado WinForms, manteniendo la compilacion multiplataforma `net10.0`.

#### Scenario: Sin dependencias WinForms nuevas
- **WHEN** se inspeccionan las referencias de `GitExtensions.Avalonia.csproj`
- **THEN** no hay `ProjectReference` a `GitUI`, `GitExtUtils.WinForms` ni ningun proyecto con `UseWindowsForms=true`

### Requirement: Servicios del core disponibles para la presentación Avalonia
La shell SHALL poder obtener desde su composición DI los servicios portables necesarios para abrir y describir un repositorio, sin que los ViewModels o servicios de presentación tengan que referenciar `GitUI`, `ResourceManager` ni `GitExtUtils.WinForms`.

#### Scenario: La presentación resuelve sus servicios portables
- **WHEN** se construye la vista de apertura de repositorio mediante la composición de la shell
- **THEN** sus dependencias portables se resuelven desde el proveedor DI
- **AND** no se carga ningún ensamblado WinForms

#### Scenario: La composición conserva el contrato de delegates
- **WHEN** la shell se inicializa para mostrar la vista de apertura
- **THEN** los delegates de plataforma permanecen instalados para los servicios que los necesiten
- **AND** la presentación no los reemplaza con implementaciones globales propias
