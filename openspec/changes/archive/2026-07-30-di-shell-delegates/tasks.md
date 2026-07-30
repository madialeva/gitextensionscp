## 1. Referencia a GitCommands

- [x] 1.1 Añadir `ProjectReference` a `..\GitCommands\GitCommands.csproj` en `GitExtensions.Avalonia.csproj`
- [x] 1.2 Verificar que el proyecto compila con la nueva referencia: `dotnet build src/app/GitExtensions.Avalonia`

## 2. Adaptador IWindow ↔ Window

- [x] 2.1 Crear `Services/AvaloniaWindowAdapter.cs`: clase que implementa `IWindow` envolviendo un `Window` de Avalonia, con `internal Window Window { get; }` para extraer el original
- [x] 2.2 Crear metodo estatico `ResolveOwner(IWindow?)` como helper que extrae el `Window` del adapter o devuelve `null`

## 3. Diálogos Avalonia para delegates

- [x] 3.1 Crear `Services/ExceptionDialog.axaml` + `.axaml.cs`: ventana modal con titulo "Error", `TextBox` multilinea readonly con el mensaje + stack trace de la excepcion, y boton "OK" que cierra el dialogo
- [x] 3.2 Crear `Services/ErrorDialog.axaml` + `.axaml.cs`: ventana modal con titulo configurable, `TextBlock` con el mensaje, y boton "OK" que cierra el dialogo (usada por `ShowError`)
- [x] 3.3 ~~Crear metodo helper estatico `DialogHelper.ShowDialog(Window owner, Window dialog)`~~ — no necesario; se usa `Window.ShowDialog(owner)` directamente

## 4. ServiceCollectionExtensions para Avalonia

- [x] 4.1 Crear `ServiceCollectionExtensions.cs` en `GitExtensions.Avalonia` con metodo `AddAvaloniaServices(this IServiceCollection services)` que:
  - Invoca `services.AddGitExtUtils()`
  - Registra `IFileSystem` (singleton `FileSystem`)
  - Registra `IGitDirectoryResolver` (singleton `GitDirectoryResolver`)
  - Invoca `services.AddGitCommands()`
- [x] 4.2 Verificar compilacion: `dotnet build src/app/GitExtensions.Avalonia`

## 5. Cablear delegates y construir contenedor DI

- [x] 5.1 En `App.axaml.cs`, despues de la inicializacion de `JoinableTaskContext` y antes de `new MainWindow()`:
  - Construir `ServiceCollection`, invocar `AddAvaloniaServices()`, `BuildServiceProvider()`
  - Asignar a `App.ServiceProvider` (propiedad estatica)
- [x] 5.2 Cablear `TaskManager.ExceptionReporter`:
  - Instalar delegate que crea `ExceptionDialog` y lo muestra con `ShowDialog()` sobre la ventana principal
- [x] 5.3 Cablear `UserMessageHandler.ShowError`:
  - Instalar delegate que crea `ErrorDialog` con el caption y texto, y lo muestra con `ShowDialog()` sobre el owner resuelto o la ventana principal
- [x] 5.4 Cablear `OsShellUtil.PickFolder`:
  - Instalar delegate que usa `IStorageProvider.OpenFolderPickerAsync` via `JoinableTaskFactory.Run` desde el hilo UI
  - Si `selectedPath` no es null, usar `TryGetFolderFromPathAsync` como `SuggestedStartLocation`
  - Devolver `folders[0].Path.LocalPath` o `null` si se cancela
- [x] 5.5 Exponer `MainWindow` como propiedad interna estatica (o capturar `desktop.MainWindow`) para que los delegates puedan usarlo como owner de dialogos

## 6. Verificacion

- [x] 6.1 Compilar proyecto Avalonia: `dotnet build src/app/GitExtensions.Avalonia`
- [x] 6.2 Compilar solucion completa: `dotnet build GitExtensions.slnx` — la app WinForms sigue compilando
- [x] 6.3 Ejecutar `eng/Verify.ps1` — 15/15 suites pasan, build limpio
- [x] 6.4 Simular build Linux: `dotnet build src/app/GitExtensions.Avalonia -f net10.0`
- [x] 6.5 Ejecutar la app Avalonia y verificar visualmente (usuario confirma: arranca correctamente)
- [x] 6.6 Grep final: `GitExtensions.Avalonia.csproj` no tiene `ProjectReference` a `GitUI`, `GitExtUtils.WinForms` ni proyectos `UseWindowsForms=true`
