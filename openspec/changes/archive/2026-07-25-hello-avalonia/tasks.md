## 1. Paquetes NuGet

- [x] 1.1 Añadir `Avalonia` v11.3.18 a `Directory.Packages.props`
- [x] 1.2 Añadir `Avalonia.Desktop` v11.3.18 a `Directory.Packages.props`
- [x] 1.3 Añadir `Avalonia.Themes.Fluent` v11.3.18 a `Directory.Packages.props`
- [x] 1.4 Añadir `CommunityToolkit.Mvvm` (última estable) a `Directory.Packages.props`
- [x] 1.5 Restaurar paquetes y verificar que no hay conflictos de versión

## 2. Crear el proyecto GitExtensions.Avalonia

- [x] 2.1 Crear directorio `src/app/GitExtensions.Avalonia/`
- [x] 2.2 Crear `GitExtensions.Avalonia.csproj`:
  - `<TargetFramework>net10.0</TargetFramework>` (sobrescribe el global `net10.0-windows`)
  - `<UseWindowsForms>false</UseWindowsForms>` (el proyecto es Avalonia, no WinForms)
  - `<OutputType>WinExe</OutputType>`
  - `PackageReference` a `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `CommunityToolkit.Mvvm`
  - `AvaloniaResource` para `App.axaml`
  - Sin `ProjectReference` al core (se añadirán en 1.1c)
- [x] 2.3 Crear `App.axaml` con `FluentTheme` y `RequestedThemeVariant="Dark"`
- [x] 2.4 Crear `App.axaml.cs` con `OnFrameworkInitializationCompleted` que muestre `MainWindow`
- [x] 2.5 Crear `MainWindow.axaml` — ventana vacía con `Title="GitExtensions"`, 800x600
- [x] 2.6 Crear `MainWindow.axaml.cs` — code-behind vacío (solo `InitializeComponent()`)
- [x] 2.7 Verificar que el proyecto compila en Windows: `dotnet build src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj`

## 3. Añadir a la solución

- [x] 3.1 Añadir `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` a `GitExtensions.slnx` en la carpeta `/src/app/`
- [x] 3.2 Verificar que `dotnet build GitExtensions.slnx` compila la solución completa (incluyendo el nuevo proyecto)

## 4. Actualizar CI

- [x] 4.1 Añadir `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` a la lista `$coreProjects` en `eng/Verify-Linux.ps1`
- [x] 4.2 Verificar que el script Linux compila el nuevo proyecto: ejecutar `Verify-Linux.ps1` (o simular el paso de build)

## 5. Verificación final

- [x] 5.1 Ejecutar `eng/Verify.ps1` completo — build + 15/15 suites de tests pasan
- [x] 5.2 Ejecutar `eng/Verify-Linux.ps1` (simulado localmente) — el proyecto Avalonia compila en `net10.0`
- [x] 5.3 Confirmar que `fork-ci.yml` pasa en ambas plataformas (PR CI)
- [x] 5.4 Grep final: `GitExtensions.Avalonia.csproj` no tiene `ProjectReference` a ensamblados WinForms
