# cross-platform-core Specification

## Purpose
Los ensamblados del core (`GitExtensions.Extensibility`, `GitExtUtils`, `GitCommands`,
`GitUIPluginInterfaces`) son multiplataforma: targetean `net10.0` sin dependencias de
Windows y sus tests pasan en Linux en CI, demostrando que el core está listo para shells
que no sean WinForms (Avalonia, etc.).

## Requirements
### Requirement: Core assemblies target net10.0 without Windows
Los proyectos `GitExtensions.Extensibility`, `GitExtUtils`, `GitCommands` y
`GitUIPluginInterfaces` SHALL especificar `TargetFramework` como `net10.0` (sin el sufijo
`-windows`) y SHALL compilar con `UseWindowsForms=false`.

#### Scenario: Compilación en Linux
- **WHEN** se ejecuta `dotnet build` sobre cualquiera de los cuatro proyectos en Linux
- **THEN** la compilación termina sin errores

#### Scenario: Guardarraíl permanente
- **WHEN** un cambio futuro introduce una dependencia de WinForms en cualquiera de los
  cuatro ensamblados
- **THEN** el proyecto no compila y el error señala el tipo o referencia ofensor

### Requirement: GitCommands no referencia ensamblados Windows-only
El proyecto `GitCommands` SHALL NOT contener referencias de proyecto a ensamblados que
requieran Windows (`GitExtUtils.WinForms`, `ResourceManager`, `GitUI`).

#### Scenario: Inspección de referencias
- **WHEN** se inspecciona `GitCommands.csproj`
- **THEN** no existe ningún `ProjectReference` a ensamblados con TFM `-windows`

### Requirement: Notificaciones de usuario desacopladas de WinForms
El core SHALL usar delegates estáticos instalables por la shell (`UserMessageHandler.ShowError`)
para comunicar errores o advertencias al usuario, con un comportamiento por defecto neutro
(no-op vía `Trace`).

#### Scenario: Shell WinForms instala el callback
- **WHEN** la shell WinForms arranca e instala `UserMessageHandler.ShowError` mapeándolo a
  `MessageBoxes.ShowError`
- **THEN** los avisos del core se muestran como diálogos modales igual que antes del change 0.4

#### Scenario: Core se ejecuta sin shell
- **WHEN** el core emite un aviso en un entorno sin shell (tests unitarios, headless)
- **THEN** el aviso no produce excepciones y se registra en trace

### Requirement: Tests de GitCommands pasan en Linux
El proyecto `GitCommands.Tests` SHALL compilar y pasar un subset significativo de tests
(>90%) en Linux, excluyendo únicamente los tests con dependencias de infraestructura
WinForms (`ConfigureJoinableTaskFactory`, `ResourceManager.LocalizationHelpers`).

#### Scenario: CI Linux ejecuta los tests
- **WHEN** un push/PR activa el job `verify-linux`
- **THEN** el script compila los ensamblados core (`net10.0`) y ejecuta `GitCommands.Tests`
  con `-f net10.0`, pasando >90% de los tests

#### Scenario: Misma lógica, dos sistemas operativos
- **WHEN** el mismo commit se comprueba en `verify-windows` y `verify-linux`
- **THEN** los tests comunes producen el mismo resultado en ambas plataformas
