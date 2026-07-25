## Context

El proyecto tiene el core multiplataforma (`GitCommands`, `GitExtUtils`, `Extensibility`,
`GitUIPluginInterfaces`) compilando en `net10.0` y el DI migrado a MSDI (cambio 1.0). La
shell WinForms (`GitExtensions.exe`, `GitUI`) sigue funcionando y compilando en
`net10.0-windows`.

Este cambio crea el primer proyecto Avalonia del repositorio: un esqueleto mínimo que
arranca y muestra una ventana vacía con tema Fluent. Es el cambio más pequeño posible con
Avalonia — deliberadamente vacío de lógica de negocio — porque su objetivo es validar que
la infraestructura de build y CI funciona con Avalonia antes de añadir complejidad (JTF
re-plumbing, DI, VMs, vistas reales...).

**Restricciones:**
- `Directory.Build.props` define `UseWindowsForms=true` y `TargetFramework=net10.0-windows`
  globalmente. El nuevo proyecto debe anular ambas propiedades porque es un proyecto
  Avalonia multiplataforma, no WinForms.
- El proyecto NO referencia `GitCommands`, `GitUI` ni ningún ensamblado del core en este
  cambio. Las dependencias se añadirán en 1.1c (DI shell + delegates).
- La app WinForms y todos los tests existentes deben seguir compilando y pasando.
- El CI de Linux debe poder compilar el proyecto (TFM `net10.0`, sin dependencias Windows).

## Goals / Non-Goals

**Goals:**
- Crear `src/app/GitExtensions.Avalonia/` con `App.axaml`, `MainWindow.axaml` y `.csproj`
- Configurar tema Fluent con soporte claro/oscuro
- Añadir referencias NuGet de Avalonia 11.3 y CommunityToolkit.Mvvm
- Añadir el proyecto a `GitExtensions.slnx`
- `eng/Verify.ps1` compila el proyecto en Windows
- `eng/Verify-Linux.ps1` compila el proyecto en Linux
- CI (`fork-ci.yml`) pasa en ambas plataformas

**Non-Goals:**
- NO se inicializa `JoinableTaskContext` (eso es 1.1b)
- NO se configura el contenedor MSDI en la shell Avalonia (eso es 1.1c)
- NO se cablean los delegates de shell (`ExceptionReporter`, `ShowError`, `PickFolder`)
- NO hay vistas reales ni ViewModels (eso es 1.2+)
- NO se referencia `GitCommands` ni ningún ensamblado del core
- NO se ejecuta la app (solo compila); la ejecución requiere al menos 1.1b (JTF)

## Decisions

### Decisión 1: Versión de Avalonia — 11.3.18 (última estable 11.x)

Se usa Avalonia 11.3.18, la última liberación estable de la línea 11.x (publicada
2026-06-23). La 12.x (12.1.0 a 2026-07-09) ya es estable, pero se mantiene la decisión
original de evitar la rama 12.x en este momento — es una versión mayor con posibles
cambios de API, y el proyecto no necesita ninguna feature exclusiva de 12.x.

**Paquetes NuGet necesarios:**
- `Avalonia` 11.3.x — núcleo de la UI
- `Avalonia.Desktop` 11.3.x — integración con escritorio (inicialización, lifetime)
- `Avalonia.Themes.Fluent` 11.3.x — tema Fluent (claro/oscuro)
- `CommunityToolkit.Mvvm` — toolkit MVVM con source generators (se instalará en este
  cambio aunque no se use aún, para evitar tocar `Directory.Packages.props` dos veces)

**Alternativa considerada**: Avalonia 11.2 (mencionada en el análisis inicial).
Descartada: 11.3.18 es más reciente, recibe bugfixes y soporta mejor .NET 10.

### Decisión 2: TFM del proyecto — `net10.0`

El proyecto usa `<TargetFramework>net10.0</TargetFramework>` (sin `-windows`). Es
multiplataforma por definición. Debe anular explícitamente las propiedades heredadas de
`Directory.Build.props`:
- `<UseWindowsForms>false</UseWindowsForms>`
- `<TargetFramework>net10.0</TargetFramework>` (sobreescribe el `net10.0-windows` global)

**Alternativa considerada**: Mantener `net10.0-windows` y añadir `net10.0` como TFM
adicional. Descartada: este proyecto es solo Avalonia, no necesita compilar para Windows
específicamente. Si en el futuro alguna feature requiere APIs Windows-only, se puede añadir
un TFM condicional.

### Decisión 3: Estructura de ficheros

```
src/app/GitExtensions.Avalonia/
├── App.axaml                  # Application + estilos
├── App.axaml.cs               # OnFrameworkInitializationCompleted
├── App.axaml                  # Application + estilos
├── GitExtensions.Avalonia.csproj
└── MainWindow.axaml           # Ventana vacía con título "GitExtensions"
    └── MainWindow.axaml.cs    # Code-behind vacío
```

Sin subdirectorios `Views/`, `ViewModels/` ni `Controls/` — se crearán cuando haya
contenido real (cambios 1.2+).

**Alternativa considerada**: Crear la estructura de directorios completa desde el principio
(`Views/`, `ViewModels/`, `Services/`, `Controls/`). Descartada: añade complejidad sin
valor en este cambio. YAGNI.

### Decisión 4: Tema — Fluent con ThemeVariant.Default (Dark)

```xml
<Application.Styles>
    <FluentTheme />
</Application.Styles>
```

`FluentTheme` sin `Mode` explícito usa `ThemeVariant.Default`, que en Avalonia 11.x es
**Light**. Para que sea Dark por defecto (más cómodo para desarrollo), se puede establecer
`RequestedThemeVariant="Dark"` en `Application`.

**Alternativa considerada**: `SimpleTheme`. Descartado: Fluent es el tema recomendado y
más completo.

### Decisión 5: Solución — carpeta `src/app/` sin carpeta propia

El proyecto se añade a `GitExtensions.slnx` bajo la carpeta existente `/src/app/`, al
mismo nivel que `GitExtensions`, `GitUI`, etc. No se crea una subcarpeta específica para
Avalonia.

### Decisión 6: CI — añadir a Verify.ps1 y Verify-Linux.ps1

**`Verify.ps1`** (Windows): compila `GitExtensions.slnx` entera, así que al añadir el
proyecto a la solución, se compila automáticamente. Sin cambios necesarios en el script.

**`Verify-Linux.ps1`** (Linux): compila proyectos individuales. Hay que añadir
`src/app/GitExtensions.Avalonia/GitExtensions.Avalonia.csproj` a la lista `$coreProjects`.

**`fork-ci.yml`**: sin cambios; invoca los scripts.

## Risks / Trade-offs

- **[Riesgo: `CommunityToolkit.Mvvm` no es necesario aún]** — Se añade como dependencia
  "preinstalada" para no tener que tocar `Directory.Packages.props` otra vez en 1.1c. Si
  resulta que no se usa (p.ej. se elige otro toolkit), se puede quitar sin consecuencias.
  **Mitigación**: es un paquete ligero, solo añade analizadores/source generators en
  tiempo de compilación.

- **[Riesgo: Versión de Avalonia incompatible con .NET 10]** — .NET 10 es muy reciente y
  podría haber problemas de compatibilidad con Avalonia 11.3. **Mitigación**: Avalonia
  11.3.18 se publicó en junio 2026 con soporte para .NET 10. Si hay problemas, se
  actualiza a un patch posterior de 11.3.x.

- **[Riesgo: Linux no puede compilar por `EnableWindowsTargeting`]** — El proyecto Avalonia
  no referencia ningún TFM Windows, así que no debería necesitar
  `EnableWindowsTargeting=true`. Pero `Directory.Build.props` fuerza
  `TargetFramework=net10.0-windows` globalmente. **Mitigación**: el `.csproj` sobreescribe
  `TargetFramework` a `net10.0`, lo que elimina la dependencia del TFM Windows. Si aun
  así Linux falla, se añade el flag.

- **[Trade-off: El proyecto no arranca (solo compila)]** — Sin JTF re-plumbing (1.1b), la
  app no puede inicializar `JoinableTaskContext` con el `AvaloniaSynchronizationContext`
  y por tanto no puede ejecutar código del core. Es aceptable en este punto: el objetivo
  es validar compilación, no ejecución.
