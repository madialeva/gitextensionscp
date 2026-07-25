# avalonia-shell Specification

## Purpose
Shell Avalonia del proyecto: proyecto `GitExtensions.Avalonia` con tema Fluent que compila
en Windows y Linux. Es el punto de partida para todos los cambios de UI Avalonia en Fase 1
y siguientes. Establecida por el change 1.1a (`hello-avalonia`); ampliada con inicializacion
de JTF en el change 1.1b (`jtf-replumbing`).

## Requirements
### Requirement: Proyecto Avalonia en la solución
El repositorio SHALL contener un proyecto `src/app/GitExtensions.Avalonia` que compile como
`net10.0` con Avalonia 11.3, muestre una ventana vacia con tema Fluent, e inicialice
`ThreadHelper.JoinableTaskContext` con `AvaloniaSynchronizationContext` en
`OnFrameworkInitializationCompleted()`.

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

### Requirement: Dependencias declaradas
El proyecto SHALL referenciar los paquetes `Avalonia`, `Avalonia.Desktop`,
`Avalonia.Themes.Fluent` (version 11.3.x) y `CommunityToolkit.Mvvm`, con las versiones
centralizadas en `Directory.Packages.props`. `Microsoft.VisualStudio.Threading` se obtiene
via la referencia global en `Directory.Build.targets`. El proyecto SHALL tener un
`ProjectReference` a `GitExtUtils` (cross-platform, sin WinForms) para acceder a
`ThreadHelper`.

#### Scenario: Versiones en Directory.Packages.props
- **WHEN** se busca `Avalonia` en `Directory.Packages.props`
- **THEN** existe una entrada `PackageVersion` con version 11.3.x

#### Scenario: Referencias en el csproj
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `PackageReference` para `Avalonia`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent` y `CommunityToolkit.Mvvm`
- **AND** contiene `ProjectReference` a `GitExtUtils`

#### Scenario: VS-Threading disponible via referencia global
- **WHEN** se compila el proyecto
- **THEN** `Microsoft.VisualStudio.Threading` esta disponible sin `PackageReference`
  explicito en el csproj

### Requirement: Sin dependencias de WinForms
El proyecto `GitExtensions.Avalonia` SHALL NOT referenciar `GitCommands`, `GitUI`,
`GitExtUtils.WinForms` ni ningun ensamblado con dependencia de WinForms. La referencia
a `GitExtUtils` (cross-platform, `net10.0`, `UseWindowsForms=false`) es aceptable y
necesaria para `ThreadHelper`. SHALL compilar como `net10.0` puro sin `UseWindowsForms`.

#### Scenario: Sin dependencias WinForms
- **WHEN** se inspeccionan las referencias de `GitExtensions.Avalonia.csproj`
- **THEN** no hay `ProjectReference` a `GitCommands`, `GitUI`, `GitExtUtils.WinForms` ni
  ningun proyecto con `UseWindowsForms=true`

#### Scenario: Compilación multiplataforma
- **WHEN** se compila el proyecto
- **THEN** el TFM efectivo es `net10.0` (sin `-windows`) y `UseWindowsForms` es `false`

#### Scenario: Referencia a GitExtUtils permitida
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `ProjectReference` a `GitExtUtils` (ensamblado cross-platform)
