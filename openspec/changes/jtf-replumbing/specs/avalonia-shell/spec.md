## MODIFIED Requirements

### Requirement: Proyecto Avalonia en la solución
El repositorio SHALL contener un proyecto `src/app/GitExtensions.Avalonia` que compile como
`net10.0` con Avalonia 11.3 y muestre una ventana vacía con tema Fluent. El proyecto SHALL
incluir inicialización de `JoinableTaskContext` con `AvaloniaSynchronizationContext` y un
botón de diagnóstico para validar el threading.

#### Scenario: El proyecto compila en Windows
- **WHEN** se ejecuta `dotnet build` sobre `GitExtensions.Avalonia.csproj` en Windows
- **THEN** la compilación termina sin errores

#### Scenario: El proyecto compila en Linux
- **WHEN** se ejecuta `dotnet build` sobre `GitExtensions.Avalonia.csproj` en Linux
- **THEN** la compilación termina sin errores

#### Scenario: El proyecto está en la solución
- **WHEN** se abre `GitExtensions.slnx`
- **THEN** `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` aparece listado

### Requirement: Dependencias NuGet declaradas
El proyecto SHALL referenciar los paquetes `Avalonia`, `Avalonia.Desktop`,
`Avalonia.Themes.Fluent` (versión 11.3.x), `CommunityToolkit.Mvvm` y
`Microsoft.VisualStudio.Threading`, con las versiones centralizadas en
`Directory.Packages.props`.

#### Scenario: Versiones en Directory.Packages.props
- **WHEN** se busca `Microsoft.VisualStudio.Threading` en `Directory.Packages.props`
- **THEN** existe una entrada `PackageVersion` con versión 17.13.61

#### Scenario: Referencias en el csproj
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `PackageReference` para `Avalonia`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent`, `CommunityToolkit.Mvvm` y `Microsoft.VisualStudio.Threading`

## ADDED Requirements

### Requirement: Botón de diagnóstico de threading
`MainWindow` SHALL incluir un botón de prueba y un `TextBlock` que permitan validar
visualmente el funcionamiento de `ThreadHelper.FileAndForget` y
`SwitchToMainThreadAsync`.

#### Scenario: TextBlock muestra estado inicial
- **WHEN** la aplicación se inicia
- **THEN** el `TextBlock` muestra un mensaje indicando que se pulse el botón para probar

#### Scenario: Botón ejecuta FileAndForget con SwitchToMainThreadAsync
- **WHEN** el usuario pulsa el botón
- **THEN** tras un delay, el `TextBlock` se actualiza desde el hilo principal con un
  timestamp, demostrando que el thread switching funciona
