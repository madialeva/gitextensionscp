# Tasks — Change 0.3: extraer los interops de UI de GitExtUtils

> Dos pasos (design D7), Verify en verde tras cada uno. Los hallazgos del inventario se
> anotan aquí como notas bajo cada tarea, como en el 0.2.

## 1. Inventario

- [x] 1.1 Clasificar fichero a fichero `GitExtUtils` (raíz y `GitUI/`) en: neutro / Windows-only
      / mixto (a partir; hoy: `ThreadHelper`, posibles cruces en `Theming`). Confirmar con grep
      qué usa `GitCommands` exactamente y detectar dependencias datos↔pintura en Theming;
      anotar aquí la lista de movimiento definitiva y los casos no mecánicos

      **Notas del inventario (2026-07-12):**
      - **SE MUEVEN (21 ficheros)** — raíz: ClipboardUtil, FontParser, MessageBoxes,
        UIExtensions, SettingsSourceFontExtensions; `GitUI/`: ComboBoxExtensions,
        ControlDpiExtensions, ControlHotkeyExtensions, ControlTagExtensions,
        ControlThreadingExtensions, ControlUtil, DpiUtil, HighDpiMouseCursors,
        ImageListExtensions, IMenuItemBackgroundFilter, IToolStripEx, ListViewExtensions,
        PropertyGridExtensions, TableLayoutPanelExtensions, ToolStripEx*Renderer (x2),
        ToolStripExtensions, ToolStripUtil, Win32ApiUtil (usa `Message` de WinForms);
        `Interops/` completa (incl. Constants/GWL/ComboBoxButtonState: enums neutros pero
        soporte de los DllImport); `Theming/`: BmpTransformation, BrushScope, ColorHelper,
        LightnessCorrection, TabControlPaintContext, TabControlRenderer, ThemeFix.
      - **SE QUEDAN** — raíz neutra (GitArgumentBuilder, AsyncStreamReader, Linq, Validates,
        DisplayWithSuffixUpdater…; GitArgumentBuilder/LinqExtensions fueron falsos positivos
        del grep grueso); threading (ThreadHelper*, TaskManager*, CancellationTokenSequence,
        ExclusiveTaskRunner); RectangleExtensions (Rectangle es Primitives); `Theming/` de
        datos: AppColor, AppColorDefaults, ComparableExtensions, HslColor,
        IThemeSerializationData, OtherColors, Theme, ThemeId, ThemeModifiers, ThemeSettings
        (GitCommands usa ThemeId/ThemeVariations/AppColor; sin referencias a las clases de
        pintura que se van — verificado por grep).
      - **MIXTOS, caso no mecánico**: ThreadHelper y TaskManager. Además de los
        `InvokeAndForget(Control, …)` (se van como extensiones), el reporte de excepciones de
        `TaskManager.ReportExceptionOnMainThreadAsync` — usado por `FileAndForget`, que
        GitCommands consume — llama a `Application.OnThreadException` (WinForms). Decisión:
        callback inyectable estático en TaskManager (default: Trace) que la shell WinForms
        fija al arrancar a `Application.OnThreadException`; el switch al main thread se queda
        en el núcleo (VS-Threading, neutro). Detalles: las extensiones movidas necesitan
        `HandleExceptionsAsync`/token internos → `InternalsVisibleTo("GitExtUtils.WinForms")`.
      - GitCommands usa de todo esto: ThreadHelper.JoinableTaskFactory/JoinableTaskContext/
        FileAndForget (10 ficheros), CancellationTokenSequence, ThemeId/AppColor(Defaults).
      - AssemblyInfo: el proyecto nuevo replica los InternalsVisibleTo pertinentes.

## 2. Proyecto nuevo y movimiento

- [x] 2.1 Crear `src/app/GitExtUtils.WinForms/` (`UseWindowsForms=true`, refs a `GitExtUtils`
      y `Extensibility`), añadirlo a `GitExtensions.slnx`, y trasladar los compile-links
      `BOOL.cs`/`RECT.cs`
      → también AssemblyInfo con los mismos InternalsVisibleTo que la base, y paquete
      JetBrains.Annotations (DpiUtil); VS-Threading llega global desde Directory.Build.targets
- [x] 2.2 `git mv` de los ficheros Windows-only completos según inventario (interops,
      extensiones de controles, DpiUtil, theming de pintura, `ClipboardUtil`, `MessageBoxes`,
      `FontParser`, `UIExtensions`, `SettingsSourceFontExtensions`), conservando namespaces
      → 38 ficheros movidos + OtherColors (mal clasificado: usa
      Application.IsDarkModeEnabled y solo lo consume GitUI). Total 39
- [x] 2.3 Recablear referencias: `GitCommands` → `GitExtUtils.WinForms` (con comentario
      "temporal hasta 0.4" en el csproj); `GitExtUtils.Tests` → proyecto nuevo; verificar que
      el resto de consumidores compila por transitividad sin tocar sus csproj ni sus fuentes;
      `eng/Verify.ps1` en verde
      → confirmado: solución entera compila sin tocar ningún csproj de consumidores (D2
      funcionó: cero cambios de usings por el movimiento en sí)

## 3. Ficheros mixtos y guardarraíl

- [x] 3.1 Partir `ThreadHelper`: extensiones sobre `Control` a
      `GitExtUtils.WinForms` (fichero nuevo), núcleo JoinableTask intacto en la base; ídem
      cualquier mixto que haya destapado el inventario en Theming
      → `ControlThreadHelper` (WinForms) recibe los 4 `InvokeAndForget` de
      ThreadHelper/TaskManager; `TaskManager.ExceptionReporter` (callback estático, default
      Trace) sustituye la llamada directa a `Application.OnThreadException` — lo instalan
      Program.cs, BugReporter y ConfigureJoinableTaskFactoryAttribute (los tests siguen
      capturando por `Application.ThreadException`). Accesores internos
      (`SwitchToMainThreadCancellationToken`, `DefaultTaskManager`) +
      `InternalsVisibleTo("GitExtUtils.WinForms")`
- [x] 3.2 Fijar `<UseWindowsForms>false</UseWindowsForms>` en `GitExtUtils.csproj` (mismo
      comentario-patrón que el 0.2) y compilar la solución completa; arreglar lo que el
      guardarraíl destape (esperable: restos que llegaban por los global usings implícitos)
      → destapó: usings System.Drawing implícitos (RectangleExtensions, AppColorDefaults,
      ComparableExtensions, IThemeSerializationData, HslColor, Theme — Primitives, neutros);
      y dos WinForms reales en la mitad "datos" de theming: `Theme.SystemColorMode` →
      propiedad neutra `Theme.IsDark` + extensión `GetSystemColorMode()` en el puente
      `ThemeSystemColorMode` (WinForms), y `ThemeId.ColorModeThemeId` (lee
      Application.SystemColorMode) → movido al mismo puente; 3 ficheros de GitUI adaptados
- [x] 3.3 `eng/Verify.ps1` completo en verde
      → build limpio + 15/15 suites (4:26), con el guardarraíl activo

## 4. Cierre

- [ ] 4.1 Smoke test manual de las zonas sensibles: arrancar app → cambiar tema claro/oscuro
      → comprobar escalado DPI (si hay monitor a mano) → copiar commits al portapapeles desde
      la parrilla → abrir settings de un plugin (theming de controles)
- [ ] 4.2 README breve en `src/app/GitExtUtils.WinForms/` explicando qué es el proyecto y por
      qué los namespaces no coinciden con el ensamblado (decisión D2)
- [ ] 4.3 Actualizar la hoja de ruta (0.3 completado) y el registro de decisiones interno con
      las decisiones tomadas durante la implementación
