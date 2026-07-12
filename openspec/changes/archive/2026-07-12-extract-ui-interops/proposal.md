# Change 0.3: extraer los interops de UI de GitExtUtils a un ensamblado Windows-only

## Why

**Qué significa "extraer los interops", con un ejemplo.** `GitExtUtils` es la librería de
utilidades más básica del repo: la referencian *todos* los demás proyectos, incluido
`GitCommands` (el core de git, ~25K LOC, el activo que queremos llevar a Linux/macOS). El
problema es que dentro de `GitExtUtils` conviven dos mundos que no tienen nada que ver:

```
GitExtUtils/
├── GitArgumentBuilder.cs          ← construye argumentos de git.exe (neutro, portable)
├── AsyncStreamReader.cs           ← lee la salida de procesos (neutro, portable)
├── GitUI/                         ← ~48 ficheros de OTRO mundo:
│   ├── Interops/User32/GetComboBoxInfo.cs   ← P/Invoke a user32.dll (API Win32 cruda)
│   ├── ToolStripExtensions.cs               ← extensiones de controles WinForms
│   ├── DpiUtil.cs                           ← escalado DPI de WinForms
│   └── Theming/ThemeFix.cs                  ← repintado GDI+ de controles
```

Un **interop** es código que llama directamente a las APIs nativas de Windows
(`[DllImport("user32.dll")]`, estructuras `COMBOBOXINFO`…): por definición no existe en
Linux. Junto a ellos viven extensiones de controles WinForms y theming GDI+ — igual de
Windows-only. Mientras esos ~50 ficheros vivan en `GitExtUtils`, el ensamblado entero
necesita WinForms, y como `GitCommands` depende de él, **el core hereda una dependencia de
Windows que no usa para nada**. Es el mismo problema del change 0.2 pero un piso más abajo.

**Qué se obtiene con la extracción (por qué ahora).** La cadena de dependencias del core es
`GitCommands → GitExtUtils + Extensibility`. Tras el 0.2, `Extensibility` ya compila sin
WinForms; tras este change lo hará `GitExtUtils`. Resultado: **todas las dependencias del
core quedan libres de UI**, y el 0.4 (el canary: retarget a `net10.0` neutro + pata Linux
en CI compilando y pasando los tests de `GitCommands`) se reduce a cambiar TFMs y resolver
lo que quede dentro del propio `GitCommands` — sin arrastrar refactors de utilidades. Cada
change de la Fase 0 deja el siguiente más pequeño; este es el penúltimo.

## What Changes

- **BREAKING** Nuevo proyecto `GitExtUtils.WinForms` (Windows-only, `UseWindowsForms=true`):
  recibe la subcarpeta `GitUI/` de `GitExtUtils` (interops Win32, extensiones de controles,
  DpiUtil, theming) y los Windows-only de la raíz (`ClipboardUtil`, y los recién llegados
  del 0.2: `MessageBoxes`, `FontParser`, `UIExtensions`, `SettingsSourceFontExtensions`).
- **Excepción — se quedan en `GitExtUtils`** (están bajo `GitUI/` pero son neutros y el core
  los usa a fondo): `ThreadHelper`*, `TaskManager`, `CancellationTokenSequence`,
  `ExclusiveTaskRunner` (threading sobre VS-Threading) y los datos puros de theming
  (`AppColor` enum, `AppColorDefaults`; `Color` es System.Drawing.Primitives, portable).
  *`ThreadHelper` se parte: sus extensiones sobre `Control` van al proyecto nuevo.
- Los ficheros movidos **conservan su namespace** (`GitExtUtils.GitUI.*`): los consumidores
  no cambian ni un `using`, solo se recablean referencias de proyecto. La divergencia
  namespace/ensamblado ya existe hoy y se acepta como coste menor.
- Guardarraíl (mismo patrón que el 0.2): `GitExtUtils.csproj` fija
  `<UseWindowsForms>false</UseWindowsForms>` — reintroducir WinForms en la base ya no compila.
- Paridad funcional total: cero cambios de comportamiento, `eng/Verify.ps1` en verde.

## Capabilities

### New Capabilities

- `core-dependencies`: las dependencias del core (`GitExtUtils`, además de la ya cubierta
  `Extensibility`) compilan sin WinForms, con guardarraíl permanente de compilación.

### Modified Capabilities

<!-- vacío: plugin-api no cambia; esto es un piso por debajo de la API -->

## Impact

- **Proyectos**: nuevo `src/app/GitExtUtils.WinForms/`; `GitExtUtils` pierde ~50 ficheros;
  `GitCommands` gana temporalmente la referencia al proyecto nuevo (usa `MessageBoxes` — se
  retira en 0.4 al abstraer esos avisos); por transitividad la ven ResourceManager, GitUI,
  BugReporter y plugins sin tocar sus csproj.
- **Compile-links**: `BOOL.cs`/`RECT.cs` (enlazados hoy desde GitUI) pasan al proyecto nuevo.
- **Tests**: `GitExtUtils.Tests` se parte igual (los tests de lo movido van a un
  `GitExtUtils.WinForms.Tests` o al proyecto de tests de GitUI, a decidir en design).
- **Riesgo bajo**: refactor mecánico guiado por el compilador, sin cambios de firma ni de
  namespace; el patrón (mover + guardarraíl) ya está ensayado en el 0.2.
