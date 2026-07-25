# continuous-integration Specification (delta)

## ADDED Requirements

### Requirement: Verificación en Linux
El workflow de CI SHALL contener un job que ejecute la verificación de los ensamblados
multiplataforma en un runner Linux (`ubuntu-latest`): compilación de los proyectos con TFM
`net10.0` (`Extensibility`, `GitExtUtils`, `GitCommands`, `GitUIPluginInterfaces`) y ejecución
de los tests de `GitCommands` que no requieren Windows. Este job SHALL ejecutarse en los mismos
eventos que el job Windows (push/PR a `avalonia/main` excepto cambios solo en `openspec/`, y
disparo manual) y en paralelo con él.

#### Scenario: PR activa ambos jobs
- **WHEN** se abre una PR contra `avalonia/main` con cambios fuera de `openspec/`
- **THEN** GitHub Actions arranca dos jobs en paralelo: `verify-windows` y `verify-linux`, y
  ambos deben pasar para que el check de la PR aparezca verde

#### Scenario: Job Linux ejecuta script específico
- **WHEN** el job `verify-linux` se ejecuta
- **THEN** invoca `eng/Verify-Linux.ps1`, que compila el subset de proyectos `net10.0` y
  ejecuta `dotnet test` sobre `GitCommands.Tests`

#### Scenario: Solo cambios en documentación
- **WHEN** se hace push de un commit cuyos ficheros están todos bajo `openspec/`
- **THEN** ni el job Windows ni el job Linux se ejecutan

### Requirement: Diagnóstico de fallos en Linux
El job Linux SHALL publicar como artifact los resultados `.trx` de los tests cuando la
verificación falle, con el mismo criterio de retención que el job Windows.

#### Scenario: Run de Linux fallido por tests
- **WHEN** el job Linux falla porque uno o más tests de `GitCommands` no pasan
- **THEN** la página del run ofrece un artifact descargable con los `.trx` generados
