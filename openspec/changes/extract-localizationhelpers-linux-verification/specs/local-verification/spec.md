# local-verification Specification (delta)

## MODIFIED Requirements

### Requirement: Verificación Linux ejecutable con Bash
El repositorio SHALL proveer `eng/Verify-Linux.sh`, ejecutable con Bash desde cualquier
directorio del repositorio, que acepte `Release` (por defecto) o `Debug`, compile
`GitExtensions.slnx` y ejecute `GitCommands.Tests` en `net10.0`.

#### Scenario: Ejecución Linux por defecto
- **WHEN** se ejecuta `bash eng/Verify-Linux.sh` en un repositorio sano
- **THEN** compila la solución, ejecuta `GitCommands.Tests`, genera el TRX y termina con exit
  code 0

#### Scenario: Ejecución Linux en Debug
- **WHEN** se ejecuta `bash eng/Verify-Linux.sh Debug`
- **THEN** build y tests usan la configuración `Debug` y escriben resultados bajo
  `artifacts/Debug/TestResults`

#### Scenario: Ejecución desde otro directorio
- **WHEN** se ejecuta `bash /ruta/al/repo/eng/Verify-Linux.sh` con un directorio de trabajo
  distinto al root
- **THEN** el script resuelve el root mediante su propia ubicación y completa la verificación

#### Scenario: Fallo de compilación
- **WHEN** la compilación de `GitExtensions.slnx` falla
- **THEN** el script no ejecuta tests, muestra `VERIFY-LINUX FAILED` y termina con exit code
  distinto de cero

#### Scenario: Fallo de tests
- **WHEN** `GitCommands.Tests` falla
- **THEN** el script muestra el fallo, conserva los resultados TRX y termina con exit code
  distinto de cero

#### Scenario: Descubrimiento sin tests
- **WHEN** `dotnet test` termina correctamente pero el TRX informa cero tests totales o ejecutados
- **THEN** el script trata la verificación como fallida y termina con exit code distinto de cero

### Requirement: El script Linux conserva el alcance de la verificación
El script SHALL compilar la solución cross-platform `GitExtensions.slnx` y ejecutar solo
`GitCommands.Tests` en `net10.0`; SHALL NOT compilar ni testear `GitExtensions.WinForms.slnx` ni
proyectos Windows-only.

#### Scenario: Alcance cross-platform
- **WHEN** se inspeccionan los comandos del script
- **THEN** no aparecen la solución WinForms, `GitUI` ni la aplicación WinForms
