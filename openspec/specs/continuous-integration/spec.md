# continuous-integration Specification

## Purpose
Verificación automática del fork en GitHub Actions: cada push/PR a `avalonia/main` ejecuta
la misma verificación que en local (`eng/Verify.ps1` en Windows, `eng/Verify-Linux.ps1` en
Linux), sobre máquina limpia. Establecida por el change 0.1 (`add-fork-ci`); ampliada con
pata Linux en el change 0.4 (`canary-multiplatform`); ampliada con el proyecto Avalonia en el change 1.1a (`hello-avalonia`).

## Requirements
### Requirement: Verificación automática de avalonia/main (Windows)
El repositorio SHALL contener un workflow de GitHub Actions (`.github/workflows/fork-ci.yml`)
que ejecute la verificación completa en un runner Windows en cada push/PR a `avalonia/main`
(excepto cambios solo en `openspec/`) y bajo demanda (`workflow_dispatch`).

#### Scenario: Push a la rama de trabajo
- **WHEN** se hace push de un commit a `avalonia/main` que toca ficheros fuera de `openspec/`
- **THEN** GitHub Actions arranca el job `verify-windows` y el commit queda marcado con el
  resultado (✓/✗)

#### Scenario: Push solo de documentación OpenSpec
- **WHEN** se hace push de un commit cuyos ficheros están todos bajo `openspec/`
- **THEN** el workflow no se ejecuta

### Requirement: CI como envoltorio fino del script local
El workflow SHALL limitarse a preparar la máquina (checkout con submódulos, instalación del
SDK .NET según `global.json`) e invocar scripts de verificación, y SHALL NOT contener lógica
propia de compilación o selección de tests.

#### Scenario: Paridad local/CI (Windows)
- **WHEN** `eng/Verify.ps1` pasa en una máquina local limpia con submódulos inicializados
- **THEN** el mismo commit pasa en el job `verify-windows`

#### Scenario: Paridad local/CI (Linux)
- **WHEN** `eng/Verify-Linux.ps1` pasa en una máquina local limpia con submódulos inicializados
- **THEN** el mismo commit pasa en el job `verify-linux`

### Requirement: Diagnóstico de fallos descargable
El workflow SHALL publicar como artifact los resultados `.trx` de los tests cuando la
verificación falle.

#### Scenario: Run fallido por tests
- **WHEN** un run falla porque uno o más unit tests no pasan
- **THEN** la página del run ofrece un artifact descargable con los `.trx` generados

### Requirement: Verificación en Linux (canary multiplataforma)
El workflow SHALL contener un job `verify-linux` que ejecute `eng/Verify-Linux.ps1` en un
runner `ubuntu-latest`: compilación de los ensamblados core multiplataforma (`net10.0`),
incluyendo el nuevo proyecto `GitExtensions.Avalonia`, y ejecución del subset `net10.0` de
`GitCommands.Tests`.

#### Scenario: PR activa ambos jobs
- **WHEN** se abre una PR contra `avalonia/main` con cambios fuera de `openspec/`
- **THEN** GitHub Actions arranca `verify-windows` y `verify-linux` en paralelo, y ambos
  deben pasar para que el check de la PR aparezca verde

#### Scenario: Job Linux compila el proyecto Avalonia
- **WHEN** se ejecuta el job `verify-linux`
- **THEN** `GitExtensions.Avalonia.csproj` se compila exitosamente en el runner Linux

#### Scenario: Job Linux publica artifacts al fallar
- **WHEN** el job `verify-linux` falla
- **THEN** la página del run ofrece un artifact con los `.trx` generados

### Requirement: Cancelación de runs superados
El workflow SHALL cancelar automáticamente los runs en curso de una rama cuando llega un
nuevo push a esa misma rama.

#### Scenario: Dos pushes consecutivos
- **WHEN** se hace push a `avalonia/main` mientras el run del push anterior sigue en curso
- **THEN** el run anterior se cancela y solo el nuevo llega a completarse

### Requirement: Script Verify-Linux.ps1 compila el proyecto Avalonia
El script `eng/Verify-Linux.ps1` SHALL incluir
`src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` en su lista de proyectos
core a compilar.

#### Scenario: Proyecto Avalonia en lista de coreProjects
- **WHEN** se inspecciona `eng/Verify-Linux.ps1`
- **THEN** la variable `$coreProjects` contiene la ruta
  `src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj`

#### Scenario: Fallo de build detiene el script
- **WHEN** la compilación de `GitExtensions.Avalonia.csproj` falla en Linux
- **THEN** el script termina con código de error distinto de cero y muestra
  "VERIFY-LINUX FAILED"
