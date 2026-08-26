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
El proyecto `GitCommands.Tests` SHALL compilar y pasar todos sus tests como `net10.0` (Windows
y Linux), sin depender de WinForms ni de `ResourceManager`/`GitExtUtils.WinForms`. La
infraestructura de test cross-platform (`ConfigureJoinableTaskFactory` + un
`SingleThreadSynchronizationContext` neutro) SHALL inicializar el `JoinableTaskContext` de modo
que `SwitchToMainThreadAsync` funcione sin message loop de WinForms.

#### Scenario: CI Linux ejecuta los tests
- **WHEN** un push/PR activa el job `verify-linux`
- **THEN** el script compila la solución cross-platform (`GitExtensions.slnx`) y ejecuta
  `GitCommands.Tests`, pasando todos los tests

#### Scenario: Misma lógica, dos sistemas operativos
- **WHEN** el mismo commit se comprueba en `verify-windows` y `verify-linux`
- **THEN** los tests comunes producen el mismo resultado en ambas plataformas

### Requirement: Tests del core sin dependencias de WinForms
`GitCommands.Tests` y `CommonTestUtils` SHALL ser proyectos `net10.0` puros (sin pata
`net10.0-windows`, sin `#if WINDOWS`, sin `UseWindowsForms`) y SHALL NOT referenciar
`ResourceManager`, `GitExtUtils.WinForms` ni ningún ensamblado Windows-only.

#### Scenario: Referencias de GitCommands.Tests
- **WHEN** se inspecciona `GitCommands.Tests.csproj`
- **THEN** no existe ningún `ProjectReference` a `ResourceManager`, `GitExtUtils.WinForms` ni
  `GitUI`, y el `TargetFramework` es `net10.0`

#### Scenario: Sin código condicional en CommonTestUtils
- **WHEN** se busca `#if WINDOWS` en `tests/CommonTestUtils`
- **THEN** no se encuentra ninguna ocurrencia
