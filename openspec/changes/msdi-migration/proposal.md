## Why

El proyecto usa `System.ComponentModel.Design.ServiceContainer` como contenedor DI: un service locator sin inyección por constructor. Para la shell Avalonia con MVVM (`CommunityToolkit.Mvvm`) necesitamos inyección por constructor, el estándar de `Microsoft.Extensions.DependencyInjection` (MSDI). Migrar ahora, antes de crear el proyecto Avalonia, evita tener que mantener un puente entre dos contenedores y unifica el stack DI de todo el código.

## What Changes

- Añadir `Microsoft.Extensions.DependencyInjection` al `Directory.Packages.props` y referenciarlo en los ensamblados que registran servicios
- Convertir los 4 `ServiceContainerRegistry.RegisterServices(ServiceContainer)` a métodos de extensión sobre `IServiceCollection`
- Actualizar `Program.cs` (shell WinForms) para construir el `IServiceProvider` con `new ServiceCollection()` + `BuildServiceProvider()`
- Reemplazar `new ServiceContainer()` en `GitUICommands.EmptyServiceProvider` por un `IServiceProvider` vacío construido con MSDI
- Actualizar todos los tests que crean `new ServiceContainer()` (unitarios e integración)
- Las extensiones `GetService<T>()` / `GetRequiredService<T>()` de `ServiceProviderExtensions` se adaptan para funcionar con `IServiceProvider` de MSDI (en lugar de `IServiceContainer`)

## Capabilities

### New Capabilities
- `dependency-injection`: El sistema de inyección de dependencias del proyecto usa MSDI (`IServiceCollection`/`IServiceProvider`) en lugar de `ServiceContainer`. Todos los ensamblados (core y shell) registran y resuelven servicios con el mismo contenedor estándar.

### Modified Capabilities
- `core-dependencies`: Los métodos de registro de servicios (`ServiceContainerRegistry.RegisterServices`) cambian de firma — de `ServiceContainer` a `IServiceCollection`. Los consumidores que resuelven servicios (`GetRequiredService<T>`) no cambian, ya que tanto `ServiceContainer` como el `IServiceProvider` de MSDI implementan `IServiceProvider`.

## Impact

- **NuGet**: `Directory.Packages.props` gana `Microsoft.Extensions.DependencyInjection` (versión 10.0.x)
- **Ensamblados afectados**: `GitExtUtils` (añade PackageReference; `ServiceContainerRegistry.cs` + `ServiceProviderExtensions.cs`), `GitCommands` (`ServiceContainerRegistry.cs`), `GitExtensions` (`ServiceContainerRegistry.cs` + `Program.cs`), `GitUI` (`ServiceContainerRegistry.cs` + `GitUICommands.cs`)
- **Tests**: ~30 ficheros de test que instancian `ServiceContainer` (12 en `UI.IntegrationTests`, 5 en `GitUI.Tests`, y ~15 que usan `GlobalServiceContainer.CreateDefaultMockServiceContainer`)
- **Verificación**: `eng/Verify.ps1` (15/15 suites) + smoke test manual de la app WinForms + Linux CI
- **No es breaking**: Los consumidores que resuelven servicios vía `IServiceProvider.GetService<T>()` no cambian
