# cross-platform-core Specification

## Purpose
Los ensamblados del core (`GitExtensions.Extensibility`, `GitExtUtils`, `GitCommands`) son
multiplataforma: compilan sin dependencias de Windows y sus tests pasan en un sistema operativo
no Windows, demostrando que el core está listo para una shell que no sea WinForms.

## ADDED Requirements

### Requirement: Core assemblies target net10.0 without Windows
Los proyectos `GitExtensions.Extensibility`, `GitExtUtils` y `GitCommands` SHALL especificar
`TargetFramework` como `net10.0` (sin el sufijo `-windows`) y SHALL compilar con
`UseWindowsForms=false`. El proyecto `GitUIPluginInterfaces` — dependencia transitiva de
`GitCommands` — SHALL aplicar el mismo retarget.

#### Scenario: Compilación en Windows sin SDK de Windows Forms
- **WHEN** se compila cualquiera de los cuatro ensamblados en un entorno que tiene el SDK de
  .NET 10 pero no el subsistema Windows Forms
- **THEN** la compilación termina sin errores

#### Scenario: Compilación en Linux
- **WHEN** se ejecuta `dotnet build` sobre los cuatro proyectos en un sistema operativo Linux
  con el SDK de .NET 10 instalado
- **THEN** la compilación termina sin errores

### Requirement: GitCommands no referencia ensamblados Windows-only
El proyecto `GitCommands` SHALL NOT contener referencias de proyecto a ensamblados que
requieran Windows (`GitExtUtils.WinForms`, `GitUI`, `ResourceManager`). La referencia temporal
a `GitExtUtils.WinForms` (documentada como deuda del change 0.3) SHALL ser eliminada.

#### Scenario: Referencia temporal eliminada
- **WHEN** se inspecciona `GitCommands.csproj`
- **THEN** no existe ningún `ProjectReference` a `GitExtUtils.WinForms.csproj`

#### Scenario: Guardarraíl de dependencias
- **WHEN** se compila `GitCommands` con `TargetFramework=net10.0` y `UseWindowsForms=false`
- **THEN** el compilador no produce errores relacionados con tipos WinForms o referencias
  Windows-only

### Requirement: Notificaciones de usuario desacopladas de WinForms
El core SHALL NOT depender de `MessageBoxes` (clase del ensamblado WinForms) para comunicar
errores o advertencias al usuario. En su lugar, SHALL usar un mecanismo de callback instalable
por la shell, con un comportamiento por defecto neutro (no-op o trace).

#### Scenario: Shell instala el callback
- **WHEN** la shell WinForms arranca e instala el callback de notificaciones (mapeándolo a
  `MessageBoxes.ShowError`)
- **THEN** las llamadas de aviso que el core emite desde `GitVersion`, `CommitMessageManager`
  y `ExceptionUtils` se muestran como cuadros de diálogo modales igual que antes del change

#### Scenario: Core se ejecuta sin shell
- **WHEN** el core emite un aviso en un entorno donde ninguna shell ha instalado el callback
  (p.ej. tests unitarios, aplicación headless, o antes de que la UI esté disponible)
- **THEN** el aviso no produce excepciones ni bloqueos; como mucho se registra en trace

### Requirement: Tests de GitCommands pasan en Linux
El proyecto `GitCommands.Tests` (o un subconjunto significativo que excluya solo los tests con
dependencias Windows-only) SHALL compilar y pasar en un sistema operativo Linux en CI, como
evidencia de que el core funciona sin el runtime de Windows.

#### Scenario: CI Linux ejecuta los tests
- **WHEN** un push a `avalonia/main` o una PR activa el workflow de CI
- **THEN** el job Linux compila sin errores los proyectos retargeteados y todos los tests de
  `GitCommands` que no requieren WinForms pasan (resultado verde)

#### Scenario: Mismos tests, dos sistemas operativos
- **WHEN** el mismo commit se comprueba en el job Windows y en el job Linux
- **THEN** los tests comunes (los que no dependen de WinForms) producen el mismo resultado en
  ambas plataformas
