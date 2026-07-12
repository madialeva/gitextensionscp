# Design — Change 0.3: extraer los interops de UI de GitExtUtils

## Context

`GitExtUtils` es la base de la pirámide de dependencias (`GitCommands`, `ResourceManager`,
`GitUI`, plugins y tests la referencian, directa o transitivamente). Inventario inicial
(verificado 2026-07-12; el fino se hace en la tarea 1.1):

| Grupo | Qué | Destino |
|---|---|---|
| Raíz neutra | `GitArgumentBuilder`, `AsyncStreamReader`, `LinqExtensions`, `Validates`, `MruCache`… (~20 ficheros) | Se queda |
| Raíz Windows-only | `ClipboardUtil` (WinForms `Clipboard`), `MessageBoxes`, `FontParser`, `UIExtensions`, `SettingsSourceFontExtensions` (llegados en el 0.2) | Se mueve |
| `GitUI/` threading | `ThreadHelper` (mixto), `TaskManager`, `CancellationTokenSequence`, `ExclusiveTaskRunner` — **GitCommands los usa en 10 ficheros** | Se queda (núcleo); extensiones `Control` se mueven |
| `GitUI/` interops | `Interops/User32|ComCtl32/*` (P/Invoke), `Win32ApiUtil` | Se mueve |
| `GitUI/` extensiones WinForms | `ToolStrip*`, `Control*`, `ComboBox`, `ListView`, `PropertyGrid`, `TableLayoutPanel`, `ImageList`, `DpiUtil`, `HighDpiMouseCursors` | Se mueve |
| `GitUI/Theming` | Datos (`AppColor` enum, `AppColorDefaults` — `Color` es Primitives; **AppSettings de GitCommands los usa**) vs renderizado (`ThemeFix`, `TabControlRenderer`, `ColorHelper`, `BrushScope`…) | Datos se quedan; renderizado se mueve |
| Compile-links | `BOOL.cs`/`RECT.cs` enlazados desde el proyecto GitUI | El link pasa al proyecto nuevo |

## Goals / Non-Goals

**Goals:**

- `GitExtUtils` compila con `UseWindowsForms=false` (guardarraíl permanente).
- Cero cambios en los ficheros fuente de los consumidores (namespaces intactos).
- Paridad funcional; `eng/Verify.ps1` en verde.

**Non-Goals:**

- Retarget a `net10.0` neutro (0.4; aquí ambos proyectos siguen en `net10.0-windows`).
- Quitar `MessageBoxes` de `GitCommands` (0.4: exigirá abstraer los avisos al usuario del
  core; aquí solo se documenta la deuda).
- Renombrar namespaces `GitExtUtils.GitUI.*` para que casen con el ensamblado (churn masivo
  de usings sin valor funcional; si algún día molesta, change propio).
- Tocar theming/DPI funcionalmente.

## Decisions

### D1 — Dirección de la extracción: GitExtUtils queda neutro, lo Windows-only sale

Podría hacerse al revés (dejar `GitExtUtils` como el ensamblado Windows y sacar lo neutro),
pero `GitCommands → GitExtUtils` es una referencia existente que queremos conservar apuntando
a código portable: **el proyecto que el core referencia es el que debe quedar limpio**. Nuevo
proyecto `src/app/GitExtUtils.WinForms/` (`UseWindowsForms=true`, referencia a `GitExtUtils`
y `Extensibility`).

### D2 — Los tipos movidos conservan su namespace

`GitExtUtils.GitUI.*` y `GitExtUtils.*` siguen siendo los namespaces tras el movimiento,
aunque el ensamblado sea otro. C# no exige que namespace y ensamblado coincidan, y el repo
ya convive con esa divergencia (hay namespace `GitUI` dentro del ensamblado GitExtUtils hoy).
A cambio, **ningún consumidor toca un solo using**: el compilador resuelve todo con las
referencias de proyecto. Contraste didáctico con el 0.2: allí los movimientos cambiaban el
*contrato* (firmas de la API) y el churn era inevitable; aquí solo cambia el *empaquetado*.

### D3 — Línea de corte en los ficheros mixtos

- `ThreadHelper`: el núcleo (JoinableTaskContext/Factory, `ThrowIfNotOnUIThread`,
  `FileAndForget`) se queda; las extensiones sobre `Control` (`InvokeAndForget(this Control…)`)
  se parten a un fichero nuevo (`ControlThreadHelperExtensions`) en el proyecto WinForms.
- `Theming`: la frontera es "datos vs pintura". `AppColor`/`AppColorDefaults` (enum + tabla
  de `Color` de Primitives, usados por `AppSettings` en GitCommands) se quedan; todo lo que
  toca `Control`/GDI+ de verdad (`ThemeFix`, `TabControlRenderer`, `BrushScope`…) se mueve.
  Si el inventario 1.1 encuentra acoplamientos datos→pintura, se parte el fichero, no se
  arrastra la pintura a la base.
- Regla general ante la duda: **lo que `GitCommands` necesita marca lo que se queda**; el
  resto, si menciona WinForms, se va. El árbitro final es el guardarraíl (D5).

### D4 — GitCommands referencia (temporalmente) el proyecto WinForms

`GitCommands` usa `MessageBoxes` (CommitMessageManager, ExceptionUtils, GitVersion) y eso es
WinForms. En este change `GitCommands` sigue siendo `net10.0-windows`, así que gana la
referencia a `GitExtUtils.WinForms` sin drama, y por **transitividad** ResourceManager,
GitUI, BugReporter, plugins y tests ven el ensamblado nuevo sin tocar sus csproj. La
referencia es deuda explícita del 0.4: quitar los avisos de usuario del core es un problema
de diseño (¿callback? ¿evento?) que merece decidirse allí, no de contrabando aquí.

### D5 — Guardarraíl: `UseWindowsForms=false` en GitExtUtils.csproj

Mismo patrón que el 0.2, mismo comentario explicativo en el csproj. Tras este change hay
**dos** ensamblados con el candado puesto (`Extensibility`, `GitExtUtils`); en el 0.4 el
candado se convierte en el definitivo: TFM `net10.0` sin `-windows`.

### D6 — Los tests no se mueven

`GitExtUtils.Tests` referencia también el proyecto nuevo y conserva todos sus tests
(FontParser, UIExtensions, theming…). Crear un `GitExtUtils.WinForms.Tests` sería una suite
más en Verify/CI sin ganancia: la partición de tests puede hacerse en el 0.4 si el retarget
la exige (los tests de lo neutro querrán correr en Linux; los del WinForms no).

### D7 — Ejecución en dos pasos con Verify entre ambos

1. Crear proyecto + `git mv` de ficheros completos + recablear referencias → Verify verde.
2. Partir los ficheros mixtos (`ThreadHelper`, theming si hace falta) + guardarraíl → Verify
   verde + smoke test.

Menos capas que el 0.2 porque no hay cambios de contrato: el compilador guía todo.

## Risks / Trade-offs

- **[Namespace ≠ ensamblado]** Puede despistar ("¿dónde vive `GitExtUtils.GitUI.DpiUtil`?").
  → Aceptado conscientemente (D2); documentado en el README del proyecto nuevo. La
  alternativa (renombrar) tocaría cientos de ficheros para nada funcional.
- **[Ficheros mixtos con más maraña de la prevista]** `ThemeFix`/`ColorHelper` pueden tener
  dependencias cruzadas datos↔pintura. → El inventario 1.1 lo destapa antes de mover; si la
  partición de un fichero se complica, se decide con el caso delante (partir clase parcial,
  o mover el consumidor de datos también).
- **[Choque con tags upstream]** Mover ~50 ficheros da conflictos de rename al absorber
  v7.3+. → Aceptado y ya asumido en el 0.2; `git mv` conserva historial y los merges de
  renames suelen resolverse solos.
- **[Referencia temporal de GitCommands al ensamblado WinForms]** Alguien podría leerla como
  definitiva. → Comentario en el csproj marcándola "hasta 0.4" + deuda registrada en la
  hoja de ruta.
