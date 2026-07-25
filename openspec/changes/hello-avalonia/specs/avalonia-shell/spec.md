## ADDED Requirements

### Requirement: Proyecto Avalonia en la solución
El repositorio SHALL contener un proyecto `src/app/GitExtensions.Avalonia` que compile como
`net10.0` con Avalonia 11.3 y muestre una ventana vacía con tema Fluent.

#### Scenario: El proyecto compila en Windows
- **WHEN** se ejecuta `dotnet build` sobre `GitExtensions.Avalonia.csproj` en Windows
- **THEN** la compilación termina sin errores

#### Scenario: El proyecto compila en Linux
- **WHEN** se ejecuta `dotnet build` sobre `GitExtensions.Avalonia.csproj` en Linux
- **THEN** la compilación termina sin errores

#### Scenario: El proyecto está en la solución
- **WHEN** se abre `GitExtensions.slnx`
- **THEN** `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` aparece listado

### Requirement: Tema Fluent con claro/oscuro
La aplicación SHALL usar `FluentTheme` como tema predeterminado, con soporte para variantes
clara y oscura vía `ThemeVariant`.

#### Scenario: Tema oscuro por defecto
- **WHEN** la aplicación se inicia
- **THEN** el tema aplicado es la variante oscura de Fluent

#### Scenario: Ventana con título correcto
- **WHEN** la aplicación se inicia
- **THEN** la ventana principal muestra el título "GitExtensions"

### Requirement: Dependencias NuGet declaradas
El proyecto SHALL referenciar los paquetes `Avalonia`, `Avalonia.Desktop`,
`Avalonia.Themes.Fluent` (versión 11.3.x) y `CommunityToolkit.Mvvm`, con las versiones
centralizadas en `Directory.Packages.props`.

#### Scenario: Versiones en Directory.Packages.props
- **WHEN** se busca `Avalonia` en `Directory.Packages.props`
- **THEN** existe una entrada `PackageVersion` con versión 11.3.x

#### Scenario: Referencias en el csproj
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `PackageReference` para `Avalonia`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent` y `CommunityToolkit.Mvvm`

### Requirement: Sin dependencias del core ni WinForms
El proyecto `GitExtensions.Avalonia` SHALL NOT referenciar `GitCommands`, `GitUI`,
`GitExtUtils`, `GitExtensions.Extensibility` ni ningún ensamblado con dependencia de
WinForms. SHALL compilar como `net10.0` puro sin `UseWindowsForms`.

#### Scenario: Sin dependencias WinForms
- **WHEN** se inspeccionan las referencias de `GitExtensions.Avalonia.csproj`
- **THEN** no hay `ProjectReference` a `GitCommands`, `GitUI`, `GitExtUtils.WinForms` ni
  ningún proyecto con `UseWindowsForms=true`

#### Scenario: Compilación multiplataforma
- **WHEN** se compila el proyecto
- **THEN** el TFM efectivo es `net10.0` (sin `-windows`) y `UseWindowsForms` es `false`
