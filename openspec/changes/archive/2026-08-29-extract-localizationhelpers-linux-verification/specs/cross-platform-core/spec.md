# cross-platform-core Specification (delta)

## MODIFIED Requirements

### Requirement: GitCommands no depende de ResourceManager para fechas
El proyecto `GitCommands` SHALL contener la lógica portable para calcular la unidad, el valor y
el signo de una fecha relativa, y SHALL exponer el formato de fecha completa sin depender de
`ResourceManager`, WinForms o proyectos Windows-only. La conversión de esos valores a texto
localizado SHALL permanecer en la capa de traducción.

#### Scenario: Cálculo portable de fecha relativa
- **WHEN** `GitCommands` calcula una fecha relativa
- **THEN** devuelve la misma unidad, valor, signo y resultado de `displayWeeks` que el
  comportamiento existente
- **AND** no carga ni referencia `ResourceManager`

#### Scenario: Presentación localizada fuera del core
- **WHEN** `ResourceManager` presenta una fecha relativa
- **THEN** traduce la unidad y el valor calculados usando sus `TranslatedStrings`
- **AND** el texto visible existente se conserva

#### Scenario: Formato de fecha completa portable
- **WHEN** el core formatea un `DateTimeOffset`
- **THEN** devuelve el formato general de la fecha local sin dependencia de UI

### Requirement: Tests de fechas relativas ejecutables en net10.0
La suite `GitCommands.Tests` SHALL contener cobertura para fechas relativas pasadas y futuras,
incluyendo segundos, minutos, horas, días, semanas, meses y años, y SHALL ejecutar esa cobertura
bajo `net10.0` sin referenciar `ResourceManager`.

#### Scenario: Casos históricos restaurados
- **WHEN** se ejecutan los dos tests recuperados del helper
- **THEN** pasan bajo `net10.0` y cubren fechas pasadas y futuras con el formateador inglés de
  prueba

#### Scenario: Proyecto de tests sin dependencia Windows
- **WHEN** se inspeccionan las referencias de `GitCommands.Tests`
- **THEN** no existe referencia a `ResourceManager`, `GitExtUtils.WinForms` ni `GitUI`
