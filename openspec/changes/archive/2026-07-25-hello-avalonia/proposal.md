## Why

La Fase 0 ha preparado el core (`GitCommands`, `GitExtUtils`, `Extensibility`) para ser multiplataforma y el cambio 1.0 ha migrado el contenedor DI a MSDI. Es el momento de crear el primer proyecto Avalonia: un esqueleto mínimo que arranque, muestre una ventana vacía con tema Fluent y compile en Windows y Linux. Este proyecto será la base sobre la que se construirá toda la shell Avalonia en cambios posteriores (1.1b, 1.1c, 1.2, …).

## What Changes

- Nuevo proyecto `src/app/GitExtensions.Avalonia` con TFM `net10.0` (multiplataforma)
- `App.axaml` + `App.axaml.cs` con tema Fluent (claro/oscuro vía `ThemeVariant`)
- `MainWindow.axaml` + `MainWindow.axaml.cs` — ventana vacía con título "GitExtensions"
- Referencias NuGet: `Avalonia` 11.3, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `CommunityToolkit.Mvvm` (aunque no se use aún, queda instalado para los cambios siguientes)
- El proyecto se añade a `GitExtensions.slnx`
- `eng/Verify.ps1` compila el nuevo proyecto (Windows)
- CI de Linux (`Verify-Linux.ps1` + `fork-ci.yml`) compila el nuevo proyecto en `ubuntu-latest`

## Capabilities

### New Capabilities
- `avalonia-shell`: Proyecto `GitExtensions.Avalonia` — shell Avalonia con tema Fluent y ventana vacía, compilando en Windows y Linux. Es el punto de partida para todos los cambios de UI Avalonia en Fase 1 y siguientes.

### Modified Capabilities
- `continuous-integration`: Los scripts de verificación (`Verify.ps1`, `Verify-Linux.ps1`) y el workflow `fork-ci.yml` compilan el nuevo proyecto `GitExtensions.Avalonia`, tanto en Windows como en Linux.

## Impact

- **NuGet**: `Directory.Packages.props` gana `Avalonia` 11.3, `Avalonia.Desktop` 11.3, `Avalonia.Themes.Fluent` 11.3, `CommunityToolkit.Mvvm` (versión estable compatible con .NET 10)
- **Ensamblados nuevos**: `src/app/GitExtensions.Avalonia/` (proyecto, App.axaml, MainWindow.axaml, .csproj)
- **Solución**: `GitExtensions.slnx` gana una entrada para el nuevo proyecto
- **CI**: `Verify.ps1` compila el nuevo proyecto; `Verify-Linux.ps1` añade `GitExtensions.Avalonia.csproj` a su lista de proyectos a compilar
- **No es breaking**: la app WinForms y todos los tests existentes siguen funcionando sin cambios
