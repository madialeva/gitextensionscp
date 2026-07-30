## Why

La shell Avalonia (1.1a) compila y arranca con `JoinableTaskContext` inicializado (1.1b), pero no tiene contenedor DI ni los delegates de shell cableados. Sin DI la inyección por constructor no funciona (bloquea 1.2+); sin delegates, `UserMessageHandler.ShowError`, `OsShellUtil.PickFolder` y `TaskManager.ExceptionReporter` escriben solo a trace en lugar de interactuar con el usuario. Es el último prerrequisito de infraestructura antes de construir vistas con lógica real.

## What Changes

- Configurar el contenedor MSDI en `App.axaml.cs` con `IServiceCollection` + `BuildServiceProvider()`, registrando servicios de `GitExtUtils` y `GitCommands` (sin `GitUI` WinForms)
- Referenciar `GitCommands` desde `GitExtensions.Avalonia.csproj` (el proyecto ya referencia `GitExtUtils`)
- Cablear `TaskManager.ExceptionReporter` → delegate que muestra un diálogo Avalonia con el error
- Cablear `UserMessageHandler.ShowError` → delegate que muestra un diálogo Avalonia con mensaje/caption
- Cablear `OsShellUtil.PickFolder` → delegate que usa `IStorageProvider.OpenFolderPickerAsync` de Avalonia
- Almacenar `IServiceProvider` para consumo desde VMs y views (patrón Service Locator como transición; la inyección por constructor llegará en 1.2)
- Mantener la app WinForms compilando y funcionando (los delegates son estáticos; WinForms instala los suyos en su `Program.cs`)

## Capabilities

### New Capabilities
- `avalonia-di`: Contenedor MSDI en la shell Avalonia con registro de servicios del core, cableado de delegates de shell (`ExceptionReporter`, `ShowError`, `PickFolder`) a diálogos Avalonia

### Modified Capabilities
- `avalonia-shell`: El proyecto `GitExtensions.Avalonia` añade referencia a `GitCommands`, inicializa MSDI y expone `IServiceProvider` para VMs/views
- `avalonia-threading`: `TaskManager.ExceptionReporter` se cablea a un diálogo Avalonia (antes solo escribía a trace)

## Impact

- `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj`: nuevo `ProjectReference` a `GitCommands`
- `src/app/GitExtensions.Avalonia/App.axaml.cs`: construcción del contenedor MSDI + cableado de delegates
- `src/app/GitExtensions.Avalonia/Services/`: nuevos ficheros con los handlers de delegates
- `openspec/specs/avalonia-di/`: nueva spec
- `openspec/specs/avalonia-shell/spec.md`: requisitos ampliados
- `openspec/specs/avalonia-threading/spec.md`: requisito de `ExceptionReporter` ampliado
- App WinForms y CI sin cambios — solo se añade código en la shell Avalonia
