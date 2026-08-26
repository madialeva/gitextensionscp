# local-verification Specification (delta)

## MODIFIED Requirements

### Requirement: Verificación completa con un solo comando
El repositorio SHALL proveer un script `eng/Verify.ps1` que, ejecutado sin argumentos desde
cualquier directorio del repo, compile la solución cross-platform (`GitExtensions.slnx`) y
ejecute los unit tests cross-platform con `-f net10.0`, terminando con exit code 0 si y solo
si todo pasa.

#### Scenario: Repo sano
- **WHEN** se ejecuta `.\eng\Verify.ps1` sobre un repo que compila y cuyos tests pasan
- **THEN** el script termina con exit code 0 y muestra un resumen final con la build y cada
  proyecto de tests marcados como OK

#### Scenario: Fallo de compilación
- **WHEN** se ejecuta el script y la solución no compila
- **THEN** el script termina con exit code distinto de 0, muestra los errores del compilador
  y no llega a ejecutar tests

#### Scenario: Fallo de tests
- **WHEN** se ejecuta el script y al menos un unit test falla
- **THEN** el script ejecuta igualmente el resto de proyectos de test, termina con exit code
  distinto de 0 y el resumen final identifica qué proyectos fallaron

### Requirement: Alcance de tests limitado a unit tests cross-platform
El script SHALL ejecutar únicamente los proyectos de test cross-platform (`GitCommands.Tests`
y futuros proyectos de test con pata `net10.0`), con `-f net10.0`. SHALL NOT ejecutar los
proyectos de test Windows-only (`GitUI.Tests`, `ResourceManager.Tests`, `BugReporter.Tests`,
tests de plugins) ni los proyectos de `tests/app/IntegrationTests/`.

#### Scenario: Descubrimiento de proyectos
- **WHEN** el script descubre los proyectos de test
- **THEN** la lista ejecutada contiene solo proyectos de test cross-platform y ninguno
  Windows-only ni de integración

#### Scenario: Simetría con Linux
- **WHEN** se ejecuta `.\eng\Verify.ps1` en Windows
- **THEN** corre los mismos proyectos de test con `-f net10.0` que `eng/Verify-Linux.ps1`
  en Linux
