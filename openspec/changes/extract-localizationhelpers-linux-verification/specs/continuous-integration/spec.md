# continuous-integration Specification (delta)

## MODIFIED Requirements

### Requirement: Verificación Linux invoca el script POSIX
El workflow `fork-ci.yml` SHALL ejecutar `eng/Verify-Linux.sh` con Bash en el runner
`ubuntu-latest`, después de preparar el checkout recursivo y el SDK indicado por `global.json`.
El job SHALL conservar la publicación de resultados TRX cuando falle.

#### Scenario: Job Linux usa Bash
- **WHEN** se ejecuta el job `verify-linux`
- **THEN** invoca `bash eng/Verify-Linux.sh` y no requiere `pwsh` para la verificación

#### Scenario: CI conserva la paridad local
- **WHEN** `bash eng/Verify-Linux.sh` pasa localmente en Linux sobre el mismo commit
- **THEN** la invocación equivalente del job `verify-linux` puede completar build y tests

#### Scenario: Fallo Linux publica diagnóstico
- **WHEN** el script Bash falla durante build o tests
- **THEN** el job queda fallido y ofrece los ficheros `.trx` bajo
  `artifacts/Release/TestResults`

### Requirement: CI Linux mantiene la solución y suite cross-platform
El job Linux SHALL seguir compilando `GitExtensions.slnx` y ejecutando `GitCommands.Tests` en
`net10.0`, sin compilar ni testear la solución WinForms ni proyectos Windows-only.

#### Scenario: Selección de proyectos sin cambios de alcance
- **WHEN** se ejecuta el job Linux
- **THEN** la solución compilada es `GitExtensions.slnx` y la suite ejecutada es
  `GitCommands.Tests`
