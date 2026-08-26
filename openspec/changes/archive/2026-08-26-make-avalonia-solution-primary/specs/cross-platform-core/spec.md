# cross-platform-core Specification (delta)

## MODIFIED Requirements

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

## ADDED Requirements

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
