## 1. Dependencia NuGet

- [x] 1.1 Añadir `PackageReference` a `Microsoft.VisualStudio.Threading` en `GitExtensions.Avalonia.csproj` — ya viene de `Directory.Build.targets` globalmente
- [x] 1.2 Restaurar paquetes y verificar compilación

## 2. Inicializar JoinableTaskContext

- [x] 2.1 Inicializar `ThreadHelper.JoinableTaskContext = new JoinableTaskContext()` en `App.OnFrameworkInitializationCompleted()`
- [x] 2.2 Añadir `using Microsoft.VisualStudio.Threading` en `App.axaml.cs`

## 3. Spike de validación — botón en MainWindow

- [x] 3.1 Añadir `StackPanel` con `TextBlock` (x:Name="StatusText") y `Button` en `MainWindow.axaml`
- [x] 3.2 Implementar `OnTestThreadingClick` en `MainWindow.axaml.cs` con FileAndForget + SwitchToMainThreadAsync + update UI
- [x] 3.3 ~~Cablear `TaskManager.ExceptionReporter`~~ — pospuesto a 1.1c; requiere referencia a GitExtUtils
- [x] 3.4 Añadir los `using` necesarios en `MainWindow.axaml.cs`

## 4. Verificación

- [x] 4.1 Compilar el proyecto (`dotnet build src/app/GitExtensions.Avalonia`)
- [x] 4.2 Compilar solución completa (`dotnet build GitExtensions.slnx`) — la app WinForms sigue funcionando
- [x] 4.3 Ejecutar `eng/Verify.ps1` — 15/15 suites pasan, build limpio
- [x] 4.4 Ejecutar la app Avalonia y pulsar el botón — verificar que el TextBlock se actualiza tras ~500ms (el usuario lo valida manualmente)
- [x] 4.5 Simular build Linux: `dotnet build src/app/GitExtensions.Avalonia -f net10.0 -p:EnableWindowsTargeting=true`
