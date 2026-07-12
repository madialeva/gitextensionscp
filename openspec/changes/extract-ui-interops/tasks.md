# Tasks — Change 0.3: extraer los interops de UI de GitExtUtils

> Dos pasos (design D7), Verify en verde tras cada uno. Los hallazgos del inventario se
> anotan aquí como notas bajo cada tarea, como en el 0.2.

## 1. Inventario

- [ ] 1.1 Clasificar fichero a fichero `GitExtUtils` (raíz y `GitUI/`) en: neutro / Windows-only
      / mixto (a partir; hoy: `ThreadHelper`, posibles cruces en `Theming`). Confirmar con grep
      qué usa `GitCommands` exactamente y detectar dependencias datos↔pintura en Theming;
      anotar aquí la lista de movimiento definitiva y los casos no mecánicos

## 2. Proyecto nuevo y movimiento

- [ ] 2.1 Crear `src/app/GitExtUtils.WinForms/` (`UseWindowsForms=true`, refs a `GitExtUtils`
      y `Extensibility`), añadirlo a `GitExtensions.slnx`, y trasladar los compile-links
      `BOOL.cs`/`RECT.cs`
- [ ] 2.2 `git mv` de los ficheros Windows-only completos según inventario (interops,
      extensiones de controles, DpiUtil, theming de pintura, `ClipboardUtil`, `MessageBoxes`,
      `FontParser`, `UIExtensions`, `SettingsSourceFontExtensions`), conservando namespaces
- [ ] 2.3 Recablear referencias: `GitCommands` → `GitExtUtils.WinForms` (con comentario
      "temporal hasta 0.4" en el csproj); `GitExtUtils.Tests` → proyecto nuevo; verificar que
      el resto de consumidores compila por transitividad sin tocar sus csproj ni sus fuentes;
      `eng/Verify.ps1` en verde

## 3. Ficheros mixtos y guardarraíl

- [ ] 3.1 Partir `ThreadHelper`: extensiones sobre `Control` a
      `GitExtUtils.WinForms` (fichero nuevo), núcleo JoinableTask intacto en la base; ídem
      cualquier mixto que haya destapado el inventario en Theming
- [ ] 3.2 Fijar `<UseWindowsForms>false</UseWindowsForms>` en `GitExtUtils.csproj` (mismo
      comentario-patrón que el 0.2) y compilar la solución completa; arreglar lo que el
      guardarraíl destape (esperable: restos que llegaban por los global usings implícitos)
- [ ] 3.3 `eng/Verify.ps1` completo en verde

## 4. Cierre

- [ ] 4.1 Smoke test manual de las zonas sensibles: arrancar app → cambiar tema claro/oscuro
      → comprobar escalado DPI (si hay monitor a mano) → copiar commits al portapapeles desde
      la parrilla → abrir settings de un plugin (theming de controles)
- [ ] 4.2 README breve en `src/app/GitExtUtils.WinForms/` explicando qué es el proyecto y por
      qué los namespaces no coinciden con el ensamblado (decisión D2)
- [ ] 4.3 Actualizar la hoja de ruta (0.3 completado) y el registro de decisiones interno con
      las decisiones tomadas durante la implementación
