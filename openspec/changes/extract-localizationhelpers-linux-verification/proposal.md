## Why

El issue [#19](https://github.com/madialeva/gitextensionscp/issues/19) recupera un
pendiente de la infraestructura de Fase 1.1: `GitCommands.Tests` dejó de ejecutar los dos
tests de `LocalizationHelpers` cuando la solución primaria se desacopló de `ResourceManager`.
El helper sigue viviendo en la capa de traducción WinForms, aunque su cálculo de fechas
relativas y su formato de fecha completa son reutilizables por el core portable.

Además, la verificación Linux sigue dependiendo de PowerShell (`eng/Verify-Linux.ps1`). El
desarrollo y la ejecución local de esta pata se hacen ahora desde Linux, por lo que necesita
un script POSIX que pueda ejecutarse directamente y que conserve la paridad funcional con la
verificación actual y con CI.

## What Changes

- Extraer a `GitCommands` la lógica portable de `LocalizationHelpers`: cálculo de unidad/valor
  relativo y formato de fecha completa, sin referencia a `ResourceManager`, WinForms ni la
  aplicación de presentación.
- Mantener en `ResourceManager` un adaptador pequeño que convierta el resultado portable en
  texto localizado usando `TranslatedStrings`, sin cambiar el texto visible de WinForms.
- Restaurar los dos tests retirados en `GitCommands.Tests`, adaptándolos a la API portable y a
  un formateador inglés de prueba, para que vuelvan a ejecutarse bajo `net10.0`.
- Sustituir `eng/Verify-Linux.ps1` por el script ejecutable `eng/Verify-Linux.sh`, con selección
  `Release`/`Debug`, build de `GitExtensions.slnx`, ejecución de `GitCommands.Tests`, logs TRX,
  resumen y códigos de salida equivalentes.
- Actualizar `fork-ci.yml` y la documentación operativa del script para invocar Bash en el
  runner Linux.

## Capabilities

### Modified Capabilities

- `cross-platform-core`: `LocalizationHelpers` tendrá un núcleo reutilizable en `GitCommands`
  y sus tests de cálculo volverán a formar parte de la suite `net10.0`, sin dependencias de
  `ResourceManager`.
- `local-verification`: la verificación Linux dispondrá de un script POSIX local-first que
  mantenga el contrato de build, tests, resultados TRX y códigos de error.
- `continuous-integration`: el job Linux invocará el script shell y conservará la misma
  solución y suite verificadas actualmente.

## Impact

- Se moverá y dividirá la implementación de `LocalizationHelpers` entre `GitCommands` y
  `ResourceManager`, y se actualizarán sus consumidores y tests.
- Se eliminará `eng/Verify-Linux.ps1` y se añadirá `eng/Verify-Linux.sh` con permiso de
  ejecución.
- `.github/workflows/fork-ci.yml` cambiará únicamente la invocación de la pata Linux; el job
  Windows y la solución WinForms de referencia no cambian.
- No se introducen referencias nuevas a WinForms en proyectos portables ni se cambia la
  localización completa de la aplicación, que sigue siendo trabajo posterior.
