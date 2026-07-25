## Context

El proyecto usa `System.ComponentModel.Design.ServiceContainer` como contenedor DI desde la
shell WinForms. Los servicios se registran mediante 4 métodos estáticos
`ServiceContainerRegistry.RegisterServices(ServiceContainer)` encadenados
(`GitExtUtils` → `GitExtensions` → `GitCommands` → `GitUI`). Los consumidores resuelven
servicios con `GetService<T>()` / `GetRequiredService<T>()`, extensiones genéricas definidas
en `ServiceProviderExtensions.cs` que encapsulan `IServiceProvider.GetService(Type)`.

En los tests, se usa `new ServiceContainer()` con mocks de NSubstitute, y la factory compartida
`GlobalServiceContainer.CreateDefaultMockServiceContainer()` en `UI.IntegrationTests`.

Para la Fase 1 (shell Avalonia con MVVM) se necesita inyección por constructor, que
`ServiceContainer` no soporta. `Microsoft.Extensions.DependencyInjection` (MSDI) es el
estándar de .NET moderno y proporciona inyección por constructor, scopes, y `IOptions<T>`.

## Goals / Non-Goals

**Goals:**
- Reemplazar `ServiceContainer` por MSDI en todos los ensamblados (core y shell WinForms)
- Mantener compatibilidad total: los consumidores que usan `GetService<T>()` no cambian
- Los tests existentes migran sin cambiar su estructura de assertions
- `eng/Verify.ps1` sigue pasando 15/15 y la app WinForms funciona igual

**Non-Goals:**
- No se introduce inyección por constructor en las clases existentes de WinForms (fuera de alcance; se hará en la shell Avalonia)
- No se cambia el modelo de registro (sigue siendo imperativo, no basado en convenciones ni attributes)
- No se introducen scopes, `IOptions<T>`, hosted services ni otras features avanzadas de MSDI

## Decisions

### Decisión 1: Métodos de extensión sobre `IServiceCollection`

Cada `ServiceContainerRegistry.RegisterServices(ServiceContainer)` se convierte en un método
de extensión `static void AddXxxServices(this IServiceCollection services)`.

```
ANTES                                    DESPUÉS
────────────────────────────────────     ──────────────────────────────────────────
ServiceContainerRegistry                 ServiceCollectionExtensions (en cada ensamblado)
  .RegisterServices(ServiceContainer)      .AddGitExtUtils(this IServiceCollection)
  .RegisterServices(ServiceContainer)      .AddGitCommands(this IServiceCollection)
  .RegisterServices(ServiceContainer)      .AddGitUI(this IServiceCollection)
  .RegisterServices(ServiceContainer)      .AddGitExtensions(this IServiceCollection)
```

**Alternativa considerada**: mantener la misma firma pero con `IServiceCollection`.
Descartada: los métodos de extensión son más idiomáticos en MSDI y permiten encadenamiento
(`services.AddGitExtUtils().AddGitCommands()`).

### Decisión 2: `ServiceProviderExtensions` se adapta, no se elimina

`AddService<T>` y `RemoveService<T>` operan sobre `IServiceContainer`, que MSDI no implementa.
`GetService<T>` y `GetRequiredService<T>` operan sobre `IServiceProvider`, que ambos
implementan.

**Cambios en `ServiceProviderExtensions`**:
- `AddService<T>` → se reemplaza por `services.AddSingleton<T>(instance)` de MSDI. Los call
  sites en `ServiceContainerRegistry` se actualizan.
- `RemoveService<T>` → se elimina. MSDI no permite quitar servicios tras construirlos. Los
  tests que lo usaban (4 ficheros) se adaptan al patrón de "último registro gana" (ver
  Decisión 4).
- `GetService<T>` y `GetRequiredService<T>` → sin cambios; siguen funcionando contra el
  `IServiceProvider` de MSDI.

### Decisión 3: `GitUICommands.EmptyServiceProvider`

Actualmente: `public static IServiceProvider EmptyServiceProvider = new ServiceContainer();`

Se reemplaza por: `new ServiceCollection().BuildServiceProvider()`. Es un provider vacío que
devuelve `null` para cualquier `GetService()`, igual que el `ServiceContainer` vacío.

### Decisión 4: Eliminación de `RemoveService<T>` en tests

MSDI resuelve conflictos de registro con la regla "último registro gana": si se registran dos
implementaciones para el mismo tipo, `GetService<T>()` devuelve la última. Esto elimina la
necesidad de `RemoveService`.

```
ANTES (con ServiceContainer)                  DESPUÉS (con MSDI)
───────────────────────────────────────       ───────────────────────────────
var c = CreateDefaultMockServiceContainer();  var services = new ServiceCollection();
c.RemoveService<IScriptsRunner>();            services.RegisterDefaultServices();
c.AddService<IScriptsRunner>(mock);           services.AddSingleton<IScriptsRunner>(mock);
                                              var provider = services.BuildServiceProvider();
```

**Alternativa considerada**: clonar la factory y permitir sobreescritura mediante un callback
`Action<IServiceCollection> overrides`. Añade complejidad innecesaria; el patrón de
"reconstruir el contenedor" es más explícito y ya se usa en tests unitarios.

### Decisión 5: Orden de migración dentro del change

Para minimizar el riesgo y permitir verificación incremental:

1. `Directory.Packages.props` + `GitExtUtils.csproj` (añadir NuGet)
2. `ServiceProviderExtensions.cs` (adaptar/quitar `AddService`/`RemoveService`)
3. `ServiceContainerRegistry.cs` en cada ensamblado, en orden de dependencia:
   `GitExtUtils` → `GitCommands` → `GitUI` → `GitExtensions`
4. `Program.cs` (shell WinForms)
5. `GitUICommands.EmptyServiceProvider`
6. Tests (unitarios primero, integración después)

Cada paso compila y pasa los tests relevantes. Orden alternativo considerado: "big bang"
(tocar todo a la vez) — descartado por riesgo de regresión difícil de diagnosticar.

### Decisión 6: Nombre del NuGet y versión

`Microsoft.Extensions.DependencyInjection` versión `10.0.0` (la que acompaña a .NET 10).
No se necesita `Microsoft.Extensions.DependencyInjection.Abstractions` por separado: el
paquete principal ya lo incluye.

## Risks / Trade-offs

- **[Riesgo: `ServiceContainer` implementa `IServiceContainer`]** — Código que haga cast a
  `IServiceContainer` o `ServiceContainer` directamente (para añadir/quitar servicios en
  runtime) romperá. **Mitigación**: el grep muestra que esto solo ocurre en los propios
  `ServiceContainerRegistry` y en los tests que usan `RemoveService`, ambos cubiertos por el
  plan de migración.

- **[Riesgo: Orden de registro distinto]** — `ServiceContainer.AddService` sobreescribe si el
  tipo ya existe. MSDI por defecto también (último gana). Pero si algún código depende de
  `GetServices<T>()` (resolver todas las implementaciones), el comportamiento difiere:
  `ServiceContainer` solo guarda una por tipo, MSDI puede tener varias. **Mitigación**: el
  proyecto no usa `GetServices<T>()` ni resuelve `IEnumerable<T>` del contenedor.

- **[Riesgo: Tests de integración de UI]** — `UI.IntegrationTests` instancian Forms reales de
  WinForms que reciben el `IServiceProvider` vía `GitUICommands`. Si el provider cambia pero
  los Forms esperan un `ServiceContainer`, fallan. **Mitigación**: los Forms consumen
  `IServiceProvider`, no `ServiceContainer`, así que no deberían notar el cambio.

- **[Trade-off: `RemoveService` se pierde]** — Los tests pierden la capacidad de quitar
  servicios después del registro. **Mitigación**: el patrón "último gana" es equivalente y
  más explícito; los 4 tests afectados se actualizan mecánicamente.

## Open Questions

- ¿Migrar también `BugReporter/Program.cs`? Usa `ServiceContainer` pero es una app
  independiente (reporte de errores). Se migra igual que la shell principal para consistencia,
  pero si da problemas se puede posponer.
