## MODIFIED Requirements

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

## ADDED Requirements

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
