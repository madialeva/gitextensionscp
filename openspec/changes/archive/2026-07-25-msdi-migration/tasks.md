## 1. Infraestructura (NuGet + csproj)

- [x] 1.1 Añadir `Microsoft.Extensions.DependencyInjection` v10.0.0 a `Directory.Packages.props`
- [x] 1.2 Añadir `PackageReference` a `Microsoft.Extensions.DependencyInjection` en `GitExtUtils.csproj`
- [x] 1.3 Añadir `PackageReference` a `Microsoft.Extensions.DependencyInjection` en `GitCommands.csproj`
- [x] 1.4 Añadir `PackageReference` a `Microsoft.Extensions.DependencyInjection` en `GitUI.csproj`
- [x] 1.5 Añadir `PackageReference` a `Microsoft.Extensions.DependencyInjection` en `GitExtensions.csproj` (el proyecto ejecutable)
- [x] 1.6 ~~Añadir `PackageReference` a `Microsoft.Extensions.DependencyInjection` en `BugReporter.csproj`~~ (no usa ServiceContainer — no necesita MSDI directo)
- [x] 1.7 Verificar que la solución compila tras añadir el NuGet (sin cambios de código aún)

## 2. Adaptar ServiceProviderExtensions

- [x] 2.1 Eliminar `AddService<T>(this IServiceContainer, T)` — reemplazado por `services.AddSingleton<T>(instance)` de MSDI
- [x] 2.2 Eliminar `RemoveService<T>(this IServiceContainer)` — MSDI no soporta eliminación post-construcción (los tests usarán el patrón "último gana")
- [x] 2.3 Mantener `GetService<T>(this IServiceProvider)` sin cambios; eliminar `GetRequiredService<T>` (MSDI ya lo proporciona)
- [x] 2.4 Quitar `using System.ComponentModel.Design` de `ServiceProviderExtensions.cs`

## 3. Migrar registros de GitExtUtils

- [x] 3.1 Convertir `GitExtUtils/ServiceContainerRegistry.cs` → nuevo `ServiceCollectionExtensions.cs` con `AddGitExtUtils(this IServiceCollection)`
- [x] 3.2 Quitar `using System.ComponentModel.Design` del fichero (eliminado el fichero antiguo)

## 4. Migrar registros de GitCommands

- [x] 4.1 Actualizar `GitCommands/ServiceContainerRegistry.cs` → nuevo `ServiceCollectionExtensions.cs` con `AddGitCommands(this IServiceCollection)`; usar factory lambdas para dependencias
- [x] 4.2 La resolución de dependencias entre capas usa factory lambdas (`sp => new Foo(sp.GetRequiredService<IBar>())`)
- [x] 4.3 Eliminado el fichero antiguo

## 5. Migrar registros de GitUI

- [x] 5.1 Actualizar `GitUI/ServiceContainerRegistry.cs` → nuevo `ServiceCollectionExtensions.cs` con `AddGitUI(this IServiceCollection)`
- [x] 5.2 Dependencias resueltas con factory lambdas; trace listener wiring extraído a `WireTraceListener(IServiceProvider)`
- [x] 5.3 Eliminado el fichero antiguo

## 6. Migrar registros de GitExtensions (orquestador)

- [x] 6.1 Actualizar `GitExtensions/ServiceContainerRegistry.cs` → nuevo `ServiceCollectionExtensions.cs` con `AddGitExtensions(this IServiceCollection)`; encadena sub-capas
- [x] 6.2 Eliminado el fichero antiguo

## 7. Actualizar Program.cs (shell WinForms)

- [x] 7.1 Reemplazar `private static readonly ServiceContainer _serviceContainer = new()` por `private static IServiceProvider _serviceProvider` + `CreateServiceProvider()`
- [x] 7.2 Construir el contenedor: `new ServiceCollection()` + `AddGitExtensions()` + `BuildServiceProvider()` + `WireTraceListener()`
- [x] 7.3 Actualizar todas las referencias a `_serviceContainer` → `_serviceProvider`
- [x] 7.4 Quitar `using System.ComponentModel.Design` de `Program.cs`

## 8. Actualizar GitUICommands.EmptyServiceProvider

- [x] 8.1 Reemplazar `new ServiceContainer()` por `new ServiceCollection().BuildServiceProvider()`

## 9. Migrar tests unitarios

- [x] 9.1 `GitUI.Tests/FormCommitTests.cs`: migrado a `ServiceCollection` + `BuildServiceProvider()`
- [x] 9.2 `GitUI.Tests/ScriptOptionsParserTests.cs`: migrado
- [x] 9.3 `GitUI.Tests/RevisionFileNameTests.cs`: migrado
- [x] 9.4 `GitUI.Tests/FileViewerTextTests.cs`: migrado

## 10. Migrar tests de integración

- [x] 10.1 `GlobalServiceContainer.cs`: renombrado a `CreateDefaultMockServiceProvider()`, retorna `IServiceProvider`, acepta `Action<IServiceCollection>?` para overrides
- [x] 10.2 Actualizar los callers de `CreateDefaultMockServiceContainer()` en ~20 ficheros de `UI.IntegrationTests`
- [x] 10.3 Adaptar los 4 tests con `RemoveService<T>`: reemplazar por lambda overrides en la factory

## 11. Migrar BugReporter

- [x] 11.1 ~~`BugReporter/Program.cs`~~ — no usa `ServiceContainer`, no requiere cambios

## 12. Verificación

- [x] 12.1 Ejecutar `eng/Verify.ps1` — 15/15 suites pasan (build + unit tests)
- [x] 12.2 Ejecutar `eng/Verify-Linux.ps1` y confirmar Linux CI verde — verificado en PR CI
- [x] 12.3 Smoke test manual: abrir la app WinForms, abrir un repositorio, navegar — verificado por el usuario
- [x] 12.4 Grep final: cero `new ServiceContainer()` en src/ y tests/; un solo `using System.ComponentModel.Design` residual en `ScriptsSettingsPage.ScriptInfoProxy.cs` (usa `UITypeEditor`/`IComponent`, no `ServiceContainer`)
