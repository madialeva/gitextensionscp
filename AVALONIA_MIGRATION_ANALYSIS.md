# GitExtensions — Análisis del stack tecnológico y viabilidad de migración a Avalonia

> Documento de estudio generado el 2026-07-11 a partir del análisis del código fuente del repositorio.
> Objetivo: conocer en detalle el stack tecnológico actual y evaluar qué supondría migrar la UI
> (hoy WinForms, solo Windows) a Avalonia (multiplataforma, MIT).

---

## 1. Resumen ejecutivo

| Pregunta | Respuesta corta |
|---|---|
| ¿Qué UI usa hoy? | **Windows Forms** sobre .NET 10 (`net10.0-windows`), con `UseWindowsForms=true` global |
| ¿Cuánto código de UI hay? | **~142.000 líneas C#** solo en `GitUI` (761 ficheros), con **68 Forms** y **~36 UserControls**, más UI en 12+ plugins |
| ¿El core está separado de la UI? | **Sí, razonablemente**: `GitCommands` (~25.500 LOC), `GitExtUtils` y `GitExtensions.Extensibility` no usan WinForms directamente (con matices, ver §6.3) |
| ¿Es viable un "port" de la UI? | **No como traducción 1:1.** Sería una **reescritura de la capa de presentación** reutilizando el core |
| Esfuerzo estimado | Muy alto: la UI es ~75-80% del código de la aplicación y usa intensivamente owner-drawing, Win32 y componentes WinForms de terceros |
| Estrategia recomendada | Nueva shell Avalonia (MVVM) que consuma `GitCommands`/`GitExtUtils` + `Extensibility` refactorizada, conviviendo con la app WinForms durante la transición (§8) |

---

## 2. Stack tecnológico base

| Aspecto | Detalle |
|---|---|
| Runtime | .NET 10 (SDK `10.0`, rollForward feature) — `global.json` |
| TFM | `net10.0-windows` (definido en `eng/RepoLayout.props` como `SolutionTargetFramework`) |
| Lenguaje | C# **14.0**, `Nullable enable`, `ImplicitUsings`, `TreatWarningsAsErrors` |
| UI | Windows Forms (`UseWindowsForms=true` en `Directory.Build.props`, aplica a *todos* los proyectos) |
| Solución | Formato nuevo `.slnx` (`GitExtensions.slnx`) |
| Paquetes | Central Package Management (`Directory.Packages.props`) |
| Plataformas | x64 (por defecto), x86, arm64 — **solo Windows** |
| Instalador | WiX 3.14 (MSI) + vswhere |
| CI | GitHub Actions (`pr-build.yml`, etc.) + AppVeyor (`appveyor.yml`) |
| Calidad | StyleCop.Analyzers, Microsoft.CodeAnalysis.BannedApiAnalyzers, ruleset propio, SonarQube, analizador Roslyn propio (`GitExtensions.Analyzers.CSharp`) |
| Localización | Sistema propio basado en **XLIFF** (`.xlf` por idioma en `GitUI/Translation`) + Transifex; `TranslationApp` como herramienta |
| Telemetría | Application Insights (`AppInsights.WindowsDesktop`) |

---

## 3. Arquitectura de proyectos

```
                    ┌───────────────────────────────┐
                    │  GitExtensions.exe (WinExe)   │  ← bootstrap, Program.cs, mutex, jump lists
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────▼───────────────┐
                    │            GitUI              │  ← 142K LOC, 68 Forms, WinForms puro
                    │  RevisionGrid │ Editor │ ...  │
                    └──┬─────────┬─────────┬────────┘
          ┌────────────┘         │         └───────────────┐
┌─────────▼─────────┐  ┌─────────▼──────────┐  ┌───────────▼──────────┐
│    GitCommands    │  │  ResourceManager   │  │  Externals (submód.) │
│  (lógica git CLI, │  │ (traducción XLIFF, │  │  ICSharpCode.TextEd. │
│   settings, 25K)  │  │  ligada a WinForms)│  │  ConEmuWinForms      │
└─────────┬─────────┘  └────────────────────┘  │  NetSpell, Git.hub   │
          │                                    └──────────────────────┘
┌─────────▼──────────────────┐   ┌────────────────────────────┐
│ GitExtensions.Extensibility│◄──┤  Plugins (MEF / VS-MEF)    │
│ + GitUIPluginInterfaces    │   │  GitHub3, BuildServers,    │
│ + GitExtUtils              │   │  Statistics, ...  (12+)    │
└────────────────────────────┘   └────────────────────────────┘
          │
┌─────────▼──────────────────────────────────────┐
│ Nativo C++: GitExtensionsShellEx (shell ext.   │
│ de Explorer), GitExtSshAskPass (diálogo SSH)   │
└────────────────────────────────────────────────┘
```

### Proyectos y su acoplamiento a Windows

| Proyecto | Rol | LOC aprox. | Acoplamiento a Windows/WinForms |
|---|---|---|---|
| `src/app/GitExtensions` | Ejecutable, arranque | pequeño | **Alto** (WinExe, mutex, DllImport en Program.cs) |
| `src/app/GitUI` | Toda la UI | **~142.300** | **Total** — es WinForms puro + Win32 |
| `src/app/GitCommands` | Lógica git (proceso `git.exe`), settings, config | ~25.500 | **Bajo** — sin `using System.Windows.Forms/Drawing`; 1 DllImport en `ProcessExtensions` |
| `src/app/GitExtUtils` | Utilidades | medio | **Medio** — contiene `GitUI/DpiUtil`, interops User32/ComCtl32 (separables) |
| `src/app/GitExtensions.Extensibility` | API pública de plugins | medio | **Medio** — expone `IWin32Window`, `Form`, `Image` en firmas (§6.3) |
| `src/app/ResourceManager` | Traducción + base de Forms/Controls | medio | **Alto** — recorre árboles de controles WinForms para traducir |
| `src/app/BugReporter` | Reporte de errores (NBug) | pequeño | Alto (WinExe, 3 forms) |
| `src/plugins/*` (12+) | Plugins integrados | medio | Mixto: lógica portable + forms de configuración WinForms |
| `src/native/GitExtensionsShellEx` | Extensión shell Explorer (menú contextual) | C++ | **Windows por definición** |
| `src/native/GitExtSshAskPass` | Diálogo askpass para SSH | C++ | **Windows por definición** |
| `externals/*` | Submódulos (ver §5) | — | Mayormente alto |

---

## 4. Librerías NuGet (Directory.Packages.props)

### 4.1 Portables — funcionarían igual en Avalonia/Linux/macOS

| Paquete | Versión | Uso | Nota |
|---|---|---|---|
| `Ben.Demystifier` | 0.4.1 | Stack traces legibles | ✅ portable |
| `ExCSS` | 4.1.3 | Parseo de temas CSS (`GitUI/Themes/*.css`) | ✅ portable (aunque el sistema de theming entero se replantearía, §6.5) |
| `GitInfo` | 2.2.0 | Versionado desde git en build | ✅ build-time |
| `JetBrains.Annotations` | 2021.2 | Anotaciones | ✅ |
| `LibGit2Sharp` | 0.31.0 | **Solo en tests** (`CommonTestUtils`) — la app usa `git.exe` por proceso | ✅ multiplataforma |
| `Microsoft.VisualStudio.Composition` | 17.2 | **VS-MEF**: sistema de plugins | ✅ netstandard, multiplataforma |
| `Microsoft.VisualStudio.Threading` | 17.13 | `JoinableTaskFactory`, async/UI-thread (258 usos de `ThreadHelper.`) | ✅ multiplataforma, pero hay que re-inicializar el `JoinableTaskContext` con el `SynchronizationContext` de Avalonia |
| `RestSharp` | 106.12 | Cliente HTTP (Git.hub → API GitHub) | ✅ (versión antigua, convendría actualizar) |
| `SmartFormat` | 3.6 | Formateo de strings (ResourceManager) | ✅ |
| `StrongOf` | 1.2.4 | Tipos fuertes | ✅ |
| `System.ComponentModel.Composition` | 6.0 | MEF clásico (PluginManager) | ✅ |
| `System.IO.Abstractions` | 22.0 | Abstracción de FS (testabilidad) | ✅ |
| `System.Reactive` / `.Linq` / `.Interfaces` | 5.0 | Rx (autocompletado, eventos) | ✅ |
| `YamlDotNet` | 16.3 | Tests | ✅ |

### 4.2 Windows-only — habría que sustituir o condicionar por plataforma

| Paquete | Uso | Alternativa multiplataforma |
|---|---|---|
| `AdysTech.CredentialManager` | Credenciales en Windows Credential Manager (GitExtUtils, Extensibility) | libsecret/Keychain vía abstracción propia, o `git credential` como backend |
| `AppInsights.WindowsDesktop` | Telemetría | SDK estándar de App Insights / OpenTelemetry |
| `ConEmu.Core` + submódulo `conemu-inside` | **Terminal embebida** (consola interactiva en la ventana) | No hay equivalente directo; en Avalonia: control de terminal propio (p. ej. estilo vte/xterm.js) o abrir terminal externo |
| `EnvDTE` | Automatización COM de **Visual Studio** (`GitUI/VisualStudioIntegration.cs`, "abrir en VS") | Solo tiene sentido en Windows; condicionar por SO |
| `Microsoft-WindowsAPICodePack-Core/-Shell` | Diálogos de shell, taskbar, jump lists | APIs de Avalonia (StorageProvider) + eliminar features de taskbar |
| `Microsoft.Windows.CsWin32` | Generación de P/Invoke Win32 (28 APIs declaradas en `GitUI/Interops/NativeMethods.txt`) | Desaparecería con la UI nueva |

### 4.3 Infraestructura de build/test (no afectan a la migración de UI)

- Analyzers: `StyleCop.Analyzers`, `Microsoft.CodeAnalysis.*`, `NUnit.Analyzers`
- Setup: `WiX` 3.14 (MSI Windows) + `vswhere` → en multiplataforma harían falta empaquetados adicionales (AppImage/deb/rpm, dmg/pkg, winget/MSIX)
- Tests: `NUnit` 4, `NSubstitute`, `AwesomeAssertions`, `Verify.NUnit`, `System.IO.Abstractions.TestingHelpers`

---

## 5. Submódulos externos (`externals/`)

| Submódulo | Qué es | Impacto en migración |
|---|---|---|
| `ICSharpCode.TextEditor` | Editor de texto **WinForms** (fork). Base del visor de diffs/blame/ficheros (`GitUI/Editor/*`: resaltado de diff, márgenes de blame, números de línea, ANSI) | **Crítico**. Sustituto natural: **AvaloniaEdit** (port de AvalonEdit). Todo `GitUI/Editor` (servicios de highlight de diff, blame margin, etc.) se reescribiría contra su API |
| `conemu-inside` (ConEmuWinForms) | Embebido del terminal **ConEmu** vía reparenting de ventanas Win32 | **Sin equivalente**. ConEmu es Windows-only. Requiere solución nueva de terminal |
| `NetSpell.SpellChecker` | Corrector ortográfico (diccionarios propios) integrado con el editor de mensajes de commit | El motor/diccionarios son C# portable; la integración visual (subrayado, menú contextual) se reharía en Avalonia |
| `Git.hub` | Cliente de API de GitHub sobre RestSharp (usado por plugin GitHub3, pull requests) | ✅ Portable prácticamente tal cual (o sustituir por Octokit) |

---

## 6. Puntos de acoplamiento a Windows más allá de "usar WinForms"

Esto es lo que hace que la migración sea más que "cambiar controles por controles":

### 6.1 Interop Win32 directo
- `GitUI/Interops/*` + CsWin32 (`NativeMethods.txt`): Job Objects de kernel32 (matar árboles de procesos git), User32 (SetParent, EnumWindows, mensajes de ventana, scroll), Gdi32, UxTheme (`SetWindowTheme` para dark mode), WinInet.
- ~57 `DllImport` repartidos por `src` y `externals`.
- `MouseWheelRedirector`, cursores DPI (`HighDpiMouseCursors`), `GetComboBoxInfo`, etc.
- **En Avalonia**: la mayoría desaparece (Avalonia gestiona DPI, theming, input), pero el manejo de procesos (Job Objects) necesitaría equivalente por SO (process groups en Unix).

### 6.2 Integración con el escritorio de Windows
- **Jump lists** y **thumbnail toolbar** de la barra de tareas (`WindowsJumpListManager`, `WindowsThumbnailToolbarButton*`, `TaskbarProgress`).
- **Shell extension** C++ (menú contextual del Explorador) — seguiría siendo Windows-only; en Linux/macOS serían integraciones distintas (Nautilus/Finder) o se pierde la feature.
- `GitExtSshAskPass` (diálogo nativo Win32) — reescribible como pequeña app Avalonia.
- Integración **PuTTY/pageant** para SSH (en Linux/macOS se usaría OpenSSH/ssh-agent, que git ya soporta; simplificación real).
- Integración con **Visual Studio** vía COM (`EnvDTE`).

### 6.3 La API de plugins expone tipos WinForms
`GitExtensions.Extensibility` (contrato público) usa `IWin32Window`, `Form`, `System.Drawing.Image` en firmas (`IGitUICommands.StartXxxDialog(IWin32Window? owner, ...)`, `Func<Form>`, iconos como `Image`). **Migrar la UI implica romper/versionar la API de plugins** y todo el ecosistema de plugins externos (PluginManager). Un paso previo útil e independiente de Avalonia: abstraer esos tipos (p. ej. `IWindowHandle`, iconos como streams/URIs).

### 6.4 Sistema de traducción propio ligado a WinForms
`ResourceManager` + `TranslationApp` traducen **recorriendo por reflexión los árboles de controles** de Forms/UserControls y volcando a `.xlf`. Con Avalonia/XAML el mecanismo cambia por completo (bindings a recursos, `x:Static`, o librerías tipo `Avalonia.Localizer`). Los `.xlf` existentes (≈20 idiomas) son reutilizables como *datos*, pero la tubería se reescribe.

### 6.5 Theming propio
Temas como **CSS** (`GitUI/Themes/*.css`) parseados con ExCSS que remapean `SystemColors`/`AppColor` + hacks Win32 (`UxTheme`, subclassing) para dark mode. Avalonia trae theming real (Fluent, `ThemeVariant` claro/oscuro, DynamicResource): este subsistema entero se sustituye por algo más simple y estándar.

### 6.6 Owner-drawing intensivo
La joya de la app, el **RevisionGrid** (`GitUI/UserControls/RevisionGrid`), es un `DataGridView` con dibujado propietario: grafo de commits (`Graph/`, `Graph/Rendering`), columnas custom (avatares, refs/labels con hit-testing propio, estado de build), quick search… Todo GDI+ (`System.Drawing`). En Avalonia se reimplementa con `DrawingContext`/custom controls — es el componente de más riesgo y más valor. Lo positivo: el *modelo* del grafo (`RevisionGraph`, lanes, segmentos) es lógica pura separada del pintado y es reutilizable.

### 6.7 Controles WinForms sin equivalente directo
- `WebBrowser` (IE) para informes de build (`BuildReportTabPageExtension`) y `RichTextBox`/RTF con extensión XHTML propia (`RichTextBoxXhtmlSupportExtension`) para el panel de commit info → en Avalonia: `SelectableTextBlock`+inlines, HtmlLabel o WebView híbrido.
- `DataGridView`, `TreeView` (panel izquierdo de ramas), `ToolStrip`/`MenuStrip`, `PropertyGrid`(settings de plugins) → equivalentes Avalonia existen (`TreeDataGrid`, `TreeView`, `Menu`) pero con APIs distintas; el `PropertyGrid` requiere librería de terceros o generación de formularios.

---

## 7. Qué se reutiliza y qué se reescribe

```
REUTILIZABLE (≈30-35% del código)            REESCRITURA (≈65-70%)
┌──────────────────────────────┐             ┌──────────────────────────────┐
│ GitCommands (25K LOC)        │             │ GitUI completo (142K LOC)    │
│  - ejecución git.exe         │             │  - 68 Forms → ventanas/vistas│
│  - parsing revisiones/diffs  │             │  - RevisionGrid + grafo (UI) │
│  - settings, config git      │             │  - Editor/diff viewer        │
│ GitExtUtils (mayoría)        │             │ ResourceManager (traducción) │
│ Extensibility (refactor API) │             │ Theming CSS → Avalonia themes│
│ Lógica de plugins            │             │ UI de los 12+ plugins        │
│ Modelo del grafo (lanes)     │             │ BugReporter UI               │
│ Git.hub, NetSpell (motor)    │             │ Terminal embebida (ConEmu)   │
│ Ficheros .xlf (20 idiomas)   │             │ Integraciones taskbar/shell  │
│ Tests de GitCommands         │             │ Tests de UI (NUnit+WinForms) │
└──────────────────────────────┘             └──────────────────────────────┘
```

Nota: `GitCommands` invoca `git.exe` por línea de comandos (no LibGit2Sharp), lo cual es **bueno** para multiplataforma — git CLI existe en todas partes. Habría que revisar suposiciones de rutas Windows (`cmd`, `.exe`, PuTTY/plink como SSH por defecto, detección de instalación de Git for Windows) y el `DllImport` de `ProcessExtensions` (kill de árboles de procesos).

---

## 8. Estrategias posibles

| Estrategia | Descripción | Valoración |
|---|---|---|
| **A. Port in-place** (form a form) | Sustituir WinForms por Avalonia dentro del mismo ejecutable | ❌ Inviable: no hay hosting estable WinForms↔Avalonia y no resolvería lo multiplataforma |
| **B. Nueva shell Avalonia** ("strangler") | Nuevo proyecto `GitUI.Avalonia` (MVVM) que consume `GitCommands`/`GitExtUtils`/`Extensibility` refactorizada. La app WinForms sigue viva mientras la nueva alcanza paridad por fases (1º browse+grid+diff, luego diálogos) | ✅ **Recomendada**. Permite entregar valor incremental y valida el desacoplamiento del core desde el día 1 |
| **C. Adoptar/contribuir a un cliente Avalonia existente** | P. ej. SourceGit (git GUI en Avalonia, MIT) y portarle features de GitExtensions | ⚠️ Menos control, pero esfuerzo órdenes de magnitud menor si el objetivo es "un GitExtensions-like multiplataforma" y no "migrar este código" |

### Trabajo previo recomendado (independiente de Avalonia, reduce riesgo)
1. **Limpiar la API de Extensibility** de tipos WinForms (`IWin32Window`, `Form`, `Image`) → abstracciones neutras.
2. Extraer de `GitExtUtils` los interops de UI (`GitUI/*` dentro de ese proyecto) a un ensamblado Windows-only.
3. Auditar `GitCommands` con `TargetFramework=net10.0` (sin `-windows`) como *canary* de multiplataforma: compilar y pasar sus tests en Linux en CI.
4. ~~Introducir ViewModels/servicios testables detrás de los Forms más gordos (FormCommit, FormBrowse) — hoy la lógica de presentación vive en code-behind.~~ **Descartado en §10.3**: siendo fork independiente, los Forms WinForms se tiran enteros; la lógica de presentación se extrae bajo demanda al construir cada vista Avalonia.

---

## 9. Riesgos principales

1. **Volumen**: ~142K LOC de UI + UI de plugins; paridad funcional completa es un proyecto plurianual para un equipo pequeño.
2. **RevisionGrid**: rendimiento del grafo con repos de cientos de miles de commits (hoy muy optimizado sobre GDI+/DataGridView virtual). Hay que reproducir virtualización y caché de render en Avalonia.
3. **Ecosistema de plugins**: romper `GitUIPluginInterfaces`/`Extensibility` invalida plugins de terceros (PluginManager los distribuye por NuGet).
4. **Traducciones**: la tubería XLIFF↔controles se reescribe; riesgo de perder los ~20 idiomas si no se migran los datos.
5. **Features sin equivalente multiplataforma**: shell extension del Explorer, jump lists, ConEmu, integración VS, PuTTY — hay que decidir qué se pierde, qué se condiciona por SO y qué se reinventa.
6. **VS-Threading**: 258 usos de `ThreadHelper` con reglas de hilo-UI verificadas por analizadores; funciona en Avalonia pero exige re-plumbing cuidadoso del `JoinableTaskContext`.
7. **Testing**: los tests de UI actuales (`UI.IntegrationTests`) instancian Forms reales de WinForms; se rehacen con Avalonia.Headless.

---

## 10. Decisiones tomadas (registro)

> Sección viva: recoge las decisiones cerradas durante el estudio.

### 10.1 Modelo de trabajo: fork propio, sin upstreaming (2026-07-11)
- No se contribuirá con PRs al proyecto upstream (`gitextensions/gitextensions`); todo el trabajo se hace en fork propio.
- **Consecuencias que esto habilita:**
  - El desacoplamiento del core (§8) se hace sin restricciones de compatibilidad: se puede **romper la API de plugins** directamente (eliminar `IWin32Window`/`Form`/`Image` de `Extensibility`) en lugar de abstraerla con ciclos de deprecación.
  - Se pueden **eliminar** subsistemas Windows-only que no interesen (jump lists, integración VS/EnvDTE, telemetría AppInsights, ConEmu) en vez de condicionarlos por plataforma.
  - El soporte de plugins de terceros (PluginManager) deja de ser una restricción; decidir más adelante si el fork mantiene ecosistema de plugins externo o solo los integrados.
- **Coste a vigilar:** `GitCommands` parsea la salida de `git` CLI, que evoluciona (formatos, mensajes, features nuevas). Conviene seguir absorbiendo fixes del upstream aunque no se contribuya.

### 10.2 Versión de partida: tag `v7.2.0` (2026-07-11)
- Baseline: **tag `v7.2.0`** (creado 2026-07-10). En la fecha de la decisión, `master` upstream no tiene commits por delante del tag, así que es simultáneamente "última estable" y "punta de master".
- Se descartan las ramas `release/X.Y` como base: son ramas de mantenimiento que solo reciben backports.
- Rama de larga vida propuesta: `avalonia/main` creada desde `v7.2.0`.
- **Política de sincronización:** absorber upstream por **tags de release** (v7.3.0, v8.0…), no commits diarios de master. Prioridad al sincronizar: `GitCommands`, `GitExtUtils`, `Extensibility` (el código reutilizado); los cambios de `GitUI` upstream dejarán de aplicar progresivamente.
- Configuración de remotos recomendada: `origin` → fork propio; `upstream` → `gitextensions/gitextensions` (solo fetch).

```
v7.2.0 (== master upstream hoy)
   │
   ├── avalonia/main          ← rama de larga vida (fork)
   │      ├── fase 0: desacoplamiento core
   │      └── fase 1: GitUI.Avalonia (vertical slice)
   │
   └── master (upstream) ──── v7.3.0 ──── v8.0
              se absorben tags, no commits diarios
```

- **Higiene del fork (2026-07-11):** se eliminaron del fork todas las ramas heredadas del
  upstream; solo se conservan `master` (espejo de referencia, se actualiza puntualmente con
  `git fetch upstream` + push) y `avalonia/main`. Las ramas del upstream siguen accesibles vía
  el remoto `upstream` si hicieran falta.

### 10.3 Faseado revisado para desarrollo incremental con OpenSpec (2026-07-11)

Revisión de la hoja de ruta original a la luz de la decisión de independencia total (§10.1) y
de la adopción de OpenSpec como flujo de trabajo (un change = unidad pequeña, con
propuesta/diseño aprobados antes de implementar y resultado verificable). Faseado vigente:

- **Fase 0 — Cimientos**: 0.1 CI propia del fork (build + unit tests, Windows) → 0.2 limpiar
  `Extensibility` de tipos WinForms → 0.3 extraer interops de UI de `GitExtUtils` → 0.4
  canary multiplataforma (retarget `net10.0` + tests de `GitCommands` en Linux) → 0.5
  (oportunista) purga de subsistemas sentenciados.
- **Fase 1 — Walking skeleton Avalonia**: 1.1 app que arranca (MVVM, DI,
  `JoinableTaskContext` sobre el `SynchronizationContext` de Avalonia, tema Fluent) → 1.2
  abrir repositorio → 1.3 lista plana de commits virtualizada (sin grafo).
- **Fase 2 — Vertical slice**: 2.1 grafo del RevisionGrid (`DrawingContext` sobre el modelo
  existente) → 2.2 refs/labels con hit-testing → 2.3 panel de ficheros → 2.4 diff viewer con
  AvaloniaEdit. *Hito: browse completo solo-lectura en Windows/Linux/macOS.*
- **Fase 3 — Operaciones de escritura**: commit/stage, fetch/pull/push, ramas, stash… un
  change por operación.
- **Fase 4 — Plataforma y paridad**: localización (reutilizando datos `.xlf`), settings UI,
  plugins, empaquetado por SO, retirada de `GitUI` WinForms.

**Justificación de los cambios respecto al plan original:**

1. **Se descarta el retrofit de ViewModels en los Forms WinForms** (antiguo punto 4 de la
   Fase 0, y punto 4 del trabajo previo de §8). Tenía sentido con upstreaming o con una
   convivencia larga strangler; en un fork donde `GitUI` se elimina entero es trabajo perdido.
   La lógica de presentación se extraerá bajo demanda, al construir cada vista Avalonia.
2. **La Fase 0 se reordena según las dependencias reales entre ensamblados**: el canary
   multiplataforma de `GitCommands` exige antes limpiar `Extensibility` (expone
   `IWin32Window`/`Form`/`Image`) y extraer los interops de `GitExtUtils`, porque `GitCommands`
   depende de ambos. Orden: CI del fork → limpiar Extensibility → extraer interops → retarget
   `net10.0` + tests de `GitCommands` en Linux (definition of done de la fase).
3. **Se añade una CI propia del fork como primer change** (0.1): sin ella, "pasar tests en
   Linux" no tiene dónde vivir y los changes posteriores no tienen verificación automática.
4. **Se intercala un "walking skeleton" Avalonia (Fase 1) antes del vertical slice (Fase 2)**:
   el salto directo de "core desacoplado" a "RevisionGrid + diff viewer" empaquetaba juntas las
   tres incógnitas mayores (bootstrap Avalonia + re-plumbing de VS-Threading/
   `JoinableTaskContext`, grafo owner-drawn, editor). El skeleton (app que arranca, abre repo,
   lista plana de commits virtualizada sin grafo) despeja la infraestructura y deja el change
   del grafo acotado a "solo el pintado".
5. **Purga de subsistemas sentenciados solo oportunista** (0.5): AppInsights, EnvDTE, jump
   lists se eliminan si estorban al desacoplamiento; si no, mueren con `GitUI`. ConEmu no se
   toca: la app WinForms debe seguir usable como referencia funcional diaria durante toda la
   migración.
6. **Organización OpenSpec**: capabilities de `openspec/specs/` por área estable
   (`core-decoupling`, `avalonia-shell`, `revision-grid`, `diff-viewer`, `git-operations`…),
   no por fase. Decisiones transversales abiertas (toolkit MVVM — propuesta:
   CommunityToolkit.Mvvm —, DI/composición, estrategia de localización) se trabajarán con
   `openspec-explore` antes de sus changes.

### 10.4 Decisiones durante la implementación del change 0.1 — add-fork-ci (2026-07-11)

- **Completado** (PR #2, mergeada con run verde). Datos: run de CI ~7,5 min (checkout + SDK +
  build Release + 15 suites); sin caché NuGet de momento. Los 15 proyectos de unit tests
  pasan estables en el runner — no hubo que excluir ninguno (el upstream los tenía
  desactivados en su CI).
- **`global.json` diverge del upstream**: `actions/setup-dotnet` rechaza una versión parcial
  (`"10.0"`) cuando hay `rollForward`; se fija versión completa (`10.0.301` +
  `latestFeature`). Al absorber tags upstream dará un conflicto trivial de una línea.
- **"Leave fork network" ejecutado**: el repo quedó desvinculado de la red de forks de
  GitHub. Motivo: una PR se creó por error contra el upstream (el formulario de PR de un
  fork pone el repo padre como base por defecto); tras desvincular, eso ya no puede pasar.
  No afecta al remoto `upstream` ni a la absorción de tags (es pura relación de metadatos
  en GitHub).
- **Incidente registrado**: PR #13171 abierta por error contra `gitextensions/gitextensions`
  y cerrada sin consecuencias.

### 10.5 Decisiones durante la implementación del change 0.2 — clean-extensibility-winforms (2026-07-12)

- **Completado** (issue #3; implementado por capas con Verify verde tras cada una y smoke
  test manual final OK). 5 commits: iconos, owners, settings, varios+guardarraíl, docs.
- **`IWindow` marcadora funcionó como se diseñó**: los Forms base implementan la interfaz y
  los cientos de call sites que pasan `this` compilaron sin cambios; ~143 conversiones
  restantes se automatizaron con un script guiado por los errores CS1503 del compilador
  (adaptadores `AsWinFormsWindow`/`AsApiWindow` en GitUI) y ~30 se retocaron a mano.
- **`MessageBoxes` fue a `GitExtUtils`, no a GitUI** (corrección sobre el design): plugins y
  `GitCommands` lo consumen y no ven GitUI; `GitExtUtils` es visible para todos y es el
  candidato natural al ensamblado Windows-only del 0.3. Ídem `UIExtensions` y `FontParser`.
- **Ningún plugin usaba `CustomControl`**; lo que usaban era `PseudoSetting` con controles
  vivos → `PseudoSetting` quedó como datos y se añadió `LinkSetting` (texto + `Action`).
- **El guardarraíl `UseWindowsForms=false` destapó 5 acoplamientos invisibles al grep**
  (llegaban por los global usings implícitos): `Font` en la API (movido), `ContextMenuStrip`
  en `IRepositoryHostPlugin.ConfigureContextMenu` (retirado — GitUI construye el menú de
  blame con la API neutral y GitHub3 perdió su código WinForms de menús),
  `Application.ExecutablePath` en DebugHelpers, y usings explícitos para `Point`/`Color`.
  Lección: el barrido textual no basta; el compilador con el guardarraíl es el inventario real.
- **`Color`/`Point`/`ColorTranslator` se quedan en la API**: `System.Drawing.Primitives` es
  parte del framework base y multiplataforma.
- **`TranslationUtil` permanece en Extensibility con detección WinForms por reflexión**
  (sin referencia compile-time); toda la tubería de traducción se reemplaza en Fase 4.
- **Sorpresa del repo**: un generador Roslyn (`FormDefaultConstructorGenerator`, solo en
  GitUI) crea ctors `[Obsolete]` sin parámetros para Forms/UserControls; los bindings deben
  llamar al ctor real con argumentos explícitos.

### 10.6 Decisiones durante la implementación del change 0.3 — extract-ui-interops (2026-07-12)

- **Completado** (implementación + Verify verde 15/15 + smoke test de theming/portapapeles/
  pestañas OK). Un solo commit de código (67 ficheros): al no cambiar contratos no hizo
  falta el troceado por capas del 0.2.
- **Dirección validada**: `GitExtUtils` queda neutro (es lo que referencia el core) y lo
  Windows-only sale a `GitExtUtils.WinForms`. Con esto, **las dos dependencias del core
  (`Extensibility` y `GitExtUtils`) compilan con `UseWindowsForms=false`**.
- **Conservar namespaces al mover = cero churn**: los 39 ficheros movidos mantienen
  `GitExtUtils.*`/`GitUI.*` y ningún consumidor tocó un using; solo referencias de proyecto
  (GitCommands directa y marcada temporal; el resto por transitividad). Contraste con 0.2,
  donde cambiaban firmas y hubo ~170 retoques.
- **`TaskManager` era el mixto de verdad** (no solo `ThreadHelper`): su canal de excepciones
  de `FileAndForget` llamaba a `Application.OnThreadException`. Solución:
  `TaskManager.ExceptionReporter` (callback estático, default Trace) instalado por la app,
  BugReporter y la infra de tests (que captura por `Application.ThreadException`). Patrón a
  recordar para 0.4: el core no reporta a la UI, la shell se suscribe.
- **La mitad "datos" del theming tenía dos ganchos WinForms** que solo el guardarraíl vio:
  `Theme.SystemColorMode` → propiedad neutra `Theme.IsDark` + puente `ThemeSystemColorMode`
  (WinForms), y `ThemeId.ColorModeThemeId` → mismo puente. `OtherColors` resultó ser
  "pintura" (Application.IsDarkModeEnabled) y se movió entero.
- **Deuda registrada para 0.4**: (1) referencia temporal `GitCommands → GitExtUtils.WinForms`
  por `MessageBoxes` (abstraer avisos del core); (2) los tests no se partieron
  (`GitExtUtils.Tests` referencia ambos ensamblados) — partir cuando el retarget exija correr
  la mitad neutra en Linux.

### 10.7 Decisiones durante la implementación del change 0.4 — canary-multiplatform (2026-07-12)

- **Completado** (issue #5; PR #8; 5+1 commits tras iteración de CI). Fase 0 completada.
- **Retarget**: `Extensibility`, `GitExtUtils`, `GitCommands` y `GitUIPluginInterfaces`
  fijan `<TargetFramework>net10.0</TargetFramework>` explícito (rompiendo la herencia de
  `Directory.Build.props`). `GitCommands` gana `UseWindowsForms=false` + NuGets
  `System.Drawing.Common` y `System.Configuration.ConfigurationManager`.
- **El guardarraíl destapó 28 errores, no 8**. Más allá de los 4 `MessageBoxes` previstos,
  había acoplamientos invisibles al grep que llegaban por los global usings implícitos de
  WinForms: `Font` (7 sitios), `Color` (4), `Icon` (3), `Control` (3), `IWin32Window` (1),
  `ToolStripMenuItem` (1), `Application.*` (4), `TextRenderer` (1), `System.Configuration`
  (Settings.Designer.cs auto-generado), `SystemFonts`, etc. Lección confirmada del 0.2: el
  compilador con el guardarraíl es el inventario real.
- **`Font` se resuelve con `System.Drawing.Common` (NuGet cross-platform)**, no con
  `System.Drawing` de WinForms. `FontParser` y `SettingsSourceFontExtensions` se mueven
  de `GitExtUtils.WinForms` a `GitCommands` (solo dependen de Common, ya disponible).
  `CustomDiffMergeTool` (`ToolStripMenuItem`) se mueve a GitUI. `TextRenderer.MeasureText`
  se reemplaza por `Graphics.MeasureString` (GDI+ vía Common).
- **`Application.*` reemplazado**: `ProductVersion` → propiedad settable (la shell la fija);
  `ExecutablePath` → `Environment.ProcessPath`; `UserAppDataPath` → `Environment.SpecialFolder`
  + `Directory.CreateDirectory` (el API de WinForms creaba el directorio implícitamente);
  `ProductName` → `AppSettings.ApplicationName`.
- **`MessageBoxes` abstraído con `UserMessageHandler.ShowError`** (delegate estático,
  mismo patrón que `TaskManager.ExceptionReporter`). Los 4 call sites no usaban el
  `DialogResult` de vuelta (eran fire-and-forget). Instalado en `Program.cs`.
- **`PickFolder` también a delegate** (`OsShellUtil.PickFolder` pasa de método con
  `IWin32Window` a `Func<IWindow?, string?, string?>`). 11 call sites en GitUI
  actualizados con `this.AsApiWindow()` (puente `IWin32Window`→`IWindow` del 0.2).
- **`CommitMessageManager.Control` → `IWindow`**: el `Control` servía para (a) cambiar al
  hilo UI y (b) pasar como owner del diálogo. `SwitchToMainThreadAsync` ahora usa
  `ThreadHelper.JoinableTaskFactory`; el owner ya era `IWindow` via cast. `DummyOwner`
  en tests cambia de `new Control()` a un stub de `IWindow`.
- **Referencias directas añadidas**: `GitUI.csproj` y `ResourceManager.csproj` ganan
  `ProjectReference` a `GitExtUtils.WinForms` — antes llegaba por transitividad desde
  `GitCommands`, cadena rota al retirar la referencia temporal.
- **Tests multi-target**: `GitCommands.Tests` y `CommonTestUtils` ganan
  `<TargetFrameworks>net10.0-windows;net10.0</TargetFrameworks>`. En `net10.0`:
  `UseWindowsForms=false`, se excluyen `GitCommandHelpersTest.cs` (depende de
  `ResourceManager.LocalizationHelpers`), `ConfigureJoinableTaskFactoryAttribute.cs`,
  `WinFormsTestHelper.cs` y tests legacy de XML. `AssemblyInfo.cs` usa
  `#if WINDOWS` para `ConfigureJoinableTaskFactory`. Resultado: ~3210/3450 tests pasan
  en TFM neutro; los ~240 restantes necesitan `JoinableTaskContext` (no disponible sin la
  infraestructura WinForms de tests). No se movió `LocalizationHelpers` al core (opción C
  del design) porque depende de `TranslatedStrings` → infraestructura de traducción
  completa; el multitarget fue más simple y suficiente para el canary.
- **Linux CI**: `fork-ci.yml` gana job `verify-linux` (`ubuntu-latest`) con script
  `eng/Verify-Linux.ps1` que compila los 4 ensamblados core y ejecuta
  `dotnet test -f net10.0`. Requiere `-p:EnableWindowsTargeting=true` porque los proyectos
  multi-target listan `net10.0-windows` en `TargetFrameworks` y MSBuild en Linux necesita
  permiso explícito para evaluar (no compilar) TFMs Windows. `Verify.ps1` pineado a
  `-f net10.0-windows` para que el multi-target no ejecute el TFM neutro (sin JTF) en
  Windows.
- **Canary logrado**: los cuatro ensamblados del core compilan como `net10.0`, y
  `GitCommands.Tests` pasa >90% de tests en Linux en CI. La app WinForms sigue compilando
  y pasando `eng/Verify.ps1` completo (15/15 suites). **Fase 0 completada.**

### 10.8 Decisiones para la Fase 1 — Walking skeleton Avalonia (2026-07-25)

Sesión de exploración con OpenSpec (`/opsx-explore`) para refinar el plan de Fase 1 antes de
comenzar la implementación. El plan base (§10.3) definía tres bloques (1.1 proyecto que
arranca, 1.2 abrir repo, 1.3 lista plana de commits). El análisis detallado reveló que 1.1
empaquetaba varias decisiones de infraestructura con riesgos distintos y además faltaba un
prerrequisito: migrar el `ServiceContainer` a `Microsoft.Extensions.DependencyInjection` (MSDI)
en todo el código, no solo en la shell Avalonia.

#### Tecnologías seleccionadas

| Decisión | Elección | Justificación |
|---|---|---|
| Versión de Avalonia | **11.3.18** (última estable 11.x) | Estable, documentada, tooling maduro; 12.x es versión mayor con posibles cambios de API |
| MVVM toolkit | **CommunityToolkit.Mvvm** | Source generators (`[ObservableProperty]`, `[RelayCommand]`), ligero, compatible con C# 14 / .NET 10 |
| Contenedor DI | **Microsoft.Extensions.DependencyInjection** (MSDI) | Estándar .NET, ligero, inyección por constructor, reemplaza `ServiceContainer` en todo el código (no solo la shell) |
| Plugins | **Diferidos a Fase 4** | Mantener Fase 1 lean; la shell no referencia `GitUIPluginInterfaces` hasta que el esqueleto esté estable |
| Tema | **Fluent** (claro/oscuro vía `ThemeVariant`) | Nativo de Avalonia, multiplataforma |

#### Desglose refinado de la Fase 1

Se divide en 6 changes secuenciales (cada uno ≈ un change de OpenSpec), ordenados por
dependencias:

```
1.0 — MSDI migration (PRERREQUISITO)
│     Reemplazar ServiceContainer por MSDI en core + shell WinForms.
│     Cambio mecánico: ServiceContainer → IServiceCollection/IServiceProvider.
│     Verificación: Verify.ps1 15/15 + smoke test WinForms + Linux CI.
│
├─ 1.1a — Hello Avalonia
│     Proyecto GitExtensions.Avalonia, App.axaml, MainWindow vacía,
│     FluentTheme. Compila en Windows y Linux CI.
│     Depende de: 1.0.
│
├─ 1.1b — JoinableTaskContext re-plumbing (ALTO RIESGO)
│     Inicializar JoinableTaskContext con AvaloniaSynchronizationContext
│     en OnFrameworkInitializationCompleted(). Validar que
│     ThreadHelper.FileAndForget / SwitchToMainThreadAsync funcionan.
│     Depende de: 1.1a.
│
├─ 1.1c — DI shell + delegates
│     Contenedor MSDI en la shell Avalonia, registro de servicios del
│     core + VMs + Views. Cablear los 3 delegates de shell:
│     TaskManager.ExceptionReporter → diálogo Avalonia
│     UserMessageHandler.ShowError → diálogo Avalonia
│     OsShellUtil.PickFolder → IStorageProvider.OpenFolderPickerAsync
│     Depende de: 1.0, 1.1a, 1.1b.
│
├─ 1.2 — Abrir repositorio
│     WelcomeView (repos recientes de RepositoryHistoryManager + explorar),
│     MainView con info básica (rama actual, remotes, estado).
│     Primer "el core funciona bajo Avalonia".
│     Depende de: 1.1c.
│
└─ 1.3 — Lista plana de commits
      RevisionReader → lista virtualizada (DataGrid/TreeDataGrid con
      VirtualizingStackPanel). Benchmark con repos grandes (100K+ commits)
      para validar pipeline de lectura y virtualización.
      Depende de: 1.2.
```

#### Riesgos identificados

1. **`JoinableTaskContext` + Avalonia SyncContext** (change 1.1b): incertidumbre máxima. El
   `JoinableTaskContext` captura `SynchronizationContext.Current` en su constructor; en Avalonia
   el `AvaloniaSynchronizationContext` se instala durante el setup de la plataforma, entre
   `Application.Initialize()` y `OnFrameworkInitializationCompleted()`. La hipótesis es que
   funciona desde `OnFrameworkInitializationCompleted()`, pero hay que validarlo
   experimentalmente (primer spike del change). Si no funciona, habrá que buscar alternativas
   (custom factory, o reemplazar VS-Threading con algo más ligero para la shell nueva).

2. **Migración MSDI** (change 1.0): riesgo bajo pero alto volumen. `ServiceContainer` implementa
   `IServiceProvider` igual que MSDI, así que los consumidores que usan `GetService<T>()` no
   deberían romperse. El riesgo está en código que haga cast a `ServiceContainer` o llame a
   `.AddService()` fuera de los métodos de registro.

3. **Virtualización con repos grandes** (change 1.3): `RevisionReader` devuelve datos en
   streaming (es `IEnumerable`), así que la paginación es cuestión de consumir bajo demanda.
   El riesgo es la integración con el sistema de virtualización de Avalonia y el consumo de
   memoria con cientos de miles de revisiones.

#### Lo que NO incluye la Fase 1

| Excluido | Se hará en |
|---|---|
| Plugins (referencia a `GitUIPluginInterfaces`) | Fase 4 |
| Grafo del RevisionGrid | Fase 2.1 |
| Diff viewer con AvaloniaEdit | Fase 2.4 |
| Operaciones de escritura (commit, push…) | Fase 3 |
| Localización | Fase 4 |
| Empaquetado por SO | Fase 4 |

### 10.9 Registro de changes implementados (tabla viva)

> Esta tabla se actualiza con cada change que se archiva. Refleja el estado real del
> proyecto, no el plan. Ver también `openspec/changes/archive/`.

| Change | Fase | Fecha | Descripción | Capabilities |
|---|---|---|---|---|
| `add-fork-ci` | 0.1 | 2026-07-11 | CI del fork: `eng/Verify.ps1` + `fork-ci.yml` | `continuous-integration`, `local-verification` |
| `clean-extensibility-winforms` | 0.2 | 2026-07-12 | `GitExtensions.Extensibility` sin tipos WinForms | `plugin-api` |
| `extract-ui-interops` | 0.3 | 2026-07-12 | Win32 interops → `GitExtUtils.WinForms` | `core-dependencies` |
| `canary-multiplatform` | 0.4 | 2026-07-12 | Core retargeteado a `net10.0`; Linux CI | `cross-platform-core`, `core-dependencies`, `continuous-integration` |
| `msdi-migration` | 1.0 | 2026-07-25 | `ServiceContainer` → `Microsoft.Extensions.DependencyInjection` | `dependency-injection`, `core-dependencies` |
| `hello-avalonia` | 1.1a | 2026-07-25 | Proyecto `GitExtensions.Avalonia`, FluentTheme, ventana vacía | `avalonia-shell`, `continuous-integration` |
| `jtf-replumbing` | 1.1b | 2026-07-25 | `JoinableTaskContext` con `AvaloniaSynchronizationContext` | `avalonia-threading`, `avalonia-shell` |
| `di-shell-delegates` | 1.1c | 2026-07-30 | Contenedor MSDI + 3 delegates de shell cableados a diálogos Avalonia | `avalonia-di`, `avalonia-shell`, `avalonia-threading` |
| `make-avalonia-solution-primary` | 1.1d | 2026-08-26 | `GitExtensions.slnx` = solución cross-platform primaria; CI simétrico Win/Linux; test infra del core sin WinForms (`SingleThreadSynchronizationContext`) | `solution-structure`, `continuous-integration`, `local-verification`, `cross-platform-core` |

## 11. Conclusión

- El proyecto tiene una **separación core/UI mejor de lo habitual** en apps WinForms de esta edad: la lógica git (`GitCommands`) es portable casi tal cual, no depende de LibGit2 nativo (usa `git` CLI) y las librerías de infraestructura clave (VS-MEF, VS-Threading, Rx) son multiplataforma.
- Aun así, **no existe un camino de "migración" barato**: la capa de presentación (≈70% del código) usa WinForms de forma profunda (owner-drawing, Win32, controles de terceros WinForms, traducción y theming acoplados a la jerarquía de controles) y habría que **reescribirla** en Avalonia, además de versionar la API pública de plugins.
- Si el objetivo es tener GitExtensions multiplataforma, la ruta realista es la **estrategia B** (nueva shell Avalonia sobre el core existente) precedida por el trabajo de desacoplamiento del §8, empezando por un vertical slice: abrir repo → RevisionGrid → diff viewer, que es a la vez el mayor riesgo técnico y el 80% del valor de la aplicación.
