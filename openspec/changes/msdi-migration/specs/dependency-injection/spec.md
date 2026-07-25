## ADDED Requirements

### Requirement: Contenedor MSDI único
Todos los ensamblados del proyecto SHALL registrar y resolver servicios usando
`Microsoft.Extensions.DependencyInjection` (`IServiceCollection` para registro,
`IServiceProvider` para resolución). El contenedor `System.ComponentModel.Design.ServiceContainer`
SHALL NOT usarse en código de producción ni tests.

#### Scenario: Registro de servicios vía IServiceCollection
- **WHEN** la shell (WinForms o Avalonia) construye el contenedor DI
- **THEN** todos los servicios del core (`GitExtUtils`, `GitCommands`) y de la shell se registran
  mediante métodos de extensión sobre `IServiceCollection`

#### Scenario: Resolución de servicios sin cambios en consumidores
- **WHEN** un consumidor resuelve un servicio con `GetRequiredService<T>()` o `GetService<T>()`
- **THEN** las llamadas compilan y funcionan sin modificaciones, ya que el `IServiceProvider` de
  MSDI implementa `IServiceProvider`

#### Scenario: No hay ServiceContainer residual
- **WHEN** se busca `System.ComponentModel.Design.ServiceContainer` o `new ServiceContainer()`
  en el código fuente (excluyendo ficheros de archive)
- **THEN** no se encuentra ninguna ocurrencia

### Requirement: Registro declarativo por capa
Cada ensamblado que registra servicios (`GitExtUtils`, `GitCommands`, `GitUI`,
`GitExtensions`) SHALL exponer un método de extensión sobre `IServiceCollection` que
registre sus servicios, reemplazando los métodos `ServiceContainerRegistry.RegisterServices`.

#### Scenario: Registro encadenado de capas
- **WHEN** la shell invoca el registro de la capa superior (`GitExtensions`)
- **THEN** esta capa invoca secuencialmente el registro de `GitExtUtils`, registra sus propios
  servicios, y luego invoca `GitCommands` y `GitUI`

#### Scenario: Cada capa es independiente para tests
- **WHEN** un test unitario solo necesita servicios de `GitExtUtils`
- **THEN** puede invocar únicamente el método de extensión de esa capa sin referenciar las demás

### Requirement: Paridad funcional con la shell WinForms
Los cambios de migración a MSDI SHALL NOT alterar el comportamiento de la aplicación WinForms
existente.

#### Scenario: Verify.ps1 completo
- **WHEN** se ejecuta `eng/Verify.ps1` tras la migración
- **THEN** la build está limpia y los 15 proyectos de unit tests pasan

#### Scenario: Smoke test manual
- **WHEN** se ejecuta la aplicación WinForms y se abre un repositorio
- **THEN** la aplicación funciona sin errores (apertura de repo, navegación, commit, diff)

#### Scenario: Linux CI
- **WHEN** se ejecuta el job `verify-linux` de `fork-ci.yml`
- **THEN** los ensamblados core compilan y `GitCommands.Tests -f net10.0` pasa

### Requirement: Soporte para tests
Los tests existentes que instanciaban `new ServiceContainer()` SHALL funcionar con el nuevo
contenedor MSDI, usando `new ServiceCollection()` + `BuildServiceProvider()` o una factory
compartida equivalente.

#### Scenario: Test unitario de GitUI
- **WHEN** un test de `GitUI.Tests` crea un contenedor con servicios mock
- **THEN** usa `IServiceCollection` para registrar los mocks y `BuildServiceProvider()` para
  obtener el provider

#### Scenario: GlobalServiceContainer de integración
- **WHEN** los tests de `UI.IntegrationTests` invocan la factory compartida
- **THEN** la factory devuelve un `IServiceProvider` construido con MSDI que expone todos los
  servicios necesarios para los tests de UI
