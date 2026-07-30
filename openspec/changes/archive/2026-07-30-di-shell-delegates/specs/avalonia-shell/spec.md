## MODIFIED Requirements

### Requirement: Dependencias declaradas
El proyecto SHALL referenciar los paquetes `Avalonia`, `Avalonia.Desktop`,
`Avalonia.Themes.Fluent` (version 11.3.x) y `CommunityToolkit.Mvvm`, con las versiones
centralizadas en `Directory.Packages.props`. `Microsoft.VisualStudio.Threading` se obtiene
via la referencia global en `Directory.Build.targets`. El proyecto SHALL tener un
`ProjectReference` a `GitExtUtils` (cross-platform, sin WinForms) para acceder a
`ThreadHelper`, y un `ProjectReference` a `GitCommands` (cross-platform, `net10.0`) para
registrar sus servicios en el contenedor DI y acceder a tipos como `IGitDirectoryResolver`.

#### Scenario: Versiones en Directory.Packages.props
- **WHEN** se busca `Avalonia` en `Directory.Packages.props`
- **THEN** existe una entrada `PackageVersion` con version 11.3.x

#### Scenario: Referencias en el csproj
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `PackageReference` para `Avalonia`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent` y `CommunityToolkit.Mvvm`
- **AND** contiene `ProjectReference` a `GitExtUtils`
- **AND** contiene `ProjectReference` a `GitCommands`

#### Scenario: VS-Threading disponible via referencia global
- **WHEN** se compila el proyecto
- **THEN** `Microsoft.VisualStudio.Threading` esta disponible sin `PackageReference`
  explicito en el csproj

### Requirement: Sin dependencias de WinForms
El proyecto `GitExtensions.Avalonia` SHALL NOT referenciar `GitUI`,
`GitExtUtils.WinForms` ni ningun ensamblado con dependencia de WinForms fuera de
`GitExtUtils` y `GitCommands` (ambos cross-platform, `net10.0`, `UseWindowsForms=false`).
SHALL compilar como `net10.0` puro sin `UseWindowsForms`.

#### Scenario: Sin dependencias WinForms
- **WHEN** se inspeccionan las referencias de `GitExtensions.Avalonia.csproj`
- **THEN** no hay `ProjectReference` a `GitUI`, `GitExtUtils.WinForms` ni
  ningun proyecto con `UseWindowsForms=true`

#### Scenario: Compilacion multiplataforma
- **WHEN** se compila el proyecto
- **THEN** el TFM efectivo es `net10.0` (sin `-windows`) y `UseWindowsForms` es `false`

#### Scenario: Referencia a GitExtUtils y GitCommands permitida
- **WHEN** se inspecciona `GitExtensions.Avalonia.csproj`
- **THEN** contiene `ProjectReference` a `GitExtUtils` y `GitCommands` (ambos cross-platform)
