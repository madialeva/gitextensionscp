# Design — Change 0.2: limpiar los tipos WinForms de la API de Extensibility

## Context

Inventario del acoplamiento en `src/app/GitExtensions.Extensibility` (verificado 2026-07-11):

| Nivel | Qué | Dónde | Dificultad |
|---|---|---|---|
| 1 — Owners de diálogos | `IWin32Window? owner` en ~60 métodos; `Func<Form>` en `ShowModelessForm` | `Git/IGitUICommands.cs`, `Git/GitUIEventArgs.cs` (`OwnerForm`), `Git/GitUIPostActionEventArgs.cs` | Amplio pero mecánico |
| 2 — Iconos | `Image? Icon` | `Plugins/IGitPlugin.cs`, `Plugins/GitPluginBase.cs` | Pequeño; toca los 12+ plugins |
| 3 — Settings ligados a controles | `ISettingControlBinding.GetControl(): Control`, `SettingControlBinding<TSetting,TControl>`, propiedades `CustomControl` (`TextBox`/`Control`) en `StringSetting`/`PasswordSetting`/`NumberSetting`…, `CredentialsControl` (UserControl con Designer + resx) | `Settings/*` | El profundo: cambia el patrón, no solo el tipo |
| 4 — Varios | `MessageBoxes` (16 usos de `MessageBox`/`IWin32Window`) | `MessageBoxes.cs` | Reubicación simple |

Hechos que facilitan el trabajo:

- `Extensibility` es un **proyecto hoja**: no referencia a ningún otro proyecto del repo
  (solo los paquetes AdysTech.CredentialManager y StrongOf). Nadie bloquea su limpieza.
- `ISetting.CreateControlBinding()` ya es opcional (default `null`) y `GitUI` ya tiene un
  proveedor de bindings de respaldo (`GitUI.SettingControlBindings.SettingControlBindingsProvider`),
  citado en el propio doc-comment de `ISetting`. El patrón "la UI aporta el renderizado" ya
  existe a medias; este change lo completa.
- Política de ruptura sin deprecaciones ya decidida — no hay que mantener sombras de la API
  antigua.

## Goals / Non-Goals

**Goals:**

- Ninguna firma pública de `Extensibility` menciona tipos de `System.Windows.Forms` ni
  `System.Drawing`.
- Guardarraíl de compilación: el proyecto compila con `UseWindowsForms=false`.
- Paridad funcional de la app WinForms: mismos diálogos, mismos iconos, misma página de
  settings de plugins.
- `eng/Verify.ps1` en verde tras cada capa (commits intermedios funcionales).

**Non-Goals:**

- Retarget de `Extensibility` a `net10.0` neutro (change 0.4; nota: AdysTech.CredentialManager
  compila en netstandard pero es Windows-only en runtime — se aborda allí o en su propio change).
- Tocar `GitUIPluginInterfaces` (proyecto menor, 5 ficheros; se revisará en 0.4 si estorba).
- Rediseñar el sistema de settings más allá de desacoplarlo (nada de nuevos tipos de setting).
- Cualquier código Avalonia.

## Decisions

### D1 — Owner de diálogos: interfaz marcadora `IWindow` propia

Nueva interfaz en `Extensibility`: `public interface IWindow { }` — semántica: "algo que
puede ser dueño de un diálogo". Sustituye a `IWin32Window` en todas las firmas.

- En `GitUI`, las clases base de formularios (`GitExtensionsForm`/`GitModuleForm`/…)
  implementan `IWindow` → **los cientos de call sites que pasan `this` compilan sin
  cambios**. Solo se retocan a mano los sitios que pasan un `IWin32Window` "suelto"
  (inventario en tarea 2.1).
- La implementación de `GitUICommands` (en GitUI) traduce con un helper interno
  (`owner as IWin32Window` con fallback) al invocar `ShowDialog(owner)`.

*Alternativas descartadas*: `object? owner` (funciona igual pero pierde toda la
expresividad del contrato); eliminar el parámetro (rompe la modalidad y el centrado de los
diálogos); exponer `IntPtr` de handle (vuelve a ser un concepto Win32).

### D2 — Iconos como datos: `byte[]? IconData` (PNG)

`IGitPlugin.Icon: Image?` → `IconData: byte[]?`. Cada plugin embebe su PNG como
`EmbeddedResource` y lo lee del ensamblado; `GitUI` lo materializa a `Image` en el único
punto donde se consume (menú/página de plugins).

*Alternativas descartadas*: `Stream` (ambigüedad de propiedad/dispose y de re-lectura);
nombre de recurso + convención (indirección frágil); dejar `Icon` y añadir `IconData` en
paralelo (la política es romper, no duplicar).

### D3 — Settings declarativos: el binding a controles se muda entero a GitUI

- `ISetting` y los settings concretos (`StringSetting`, `BoolSetting`, `ChoiceSetting`,
  `NumberSetting`, `PasswordSetting`, `PathSetting`…) quedan como **datos puros**: nombre,
  caption, default, valor. Desaparecen `CreateControlBinding()` y las propiedades
  `CustomControl`.
- `ISettingControlBinding`, `SettingControlBinding<,>` y `CredentialsControl` (con su
  Designer/resx) **se mueven a `GitUI`**, donde el proveedor de bindings existente pasa a
  ser el único mecanismo: un registro tipo-de-setting → binding WinForms.
- Los plugins que hoy personalizan `CustomControl` se migran a la personalización
  equivalente del lado GitUI (inventario en tarea 4.1; si alguno necesita algo no cubierto,
  se decide en ese momento con el caso concreto delante).
- `CredentialsSetting` (usa AdysTech.CredentialManager) se queda en `Extensibility` como
  dato; su control se va a GitUI como el resto.

*Alternativa descartada*: interfaces de binding "abstractas" en la API (un
`ISettingRenderer<T>` neutro) — sobre-ingeniería hoy; la shell Avalonia definirá su propio
renderizado declarativo cuando exista (Fase 2+), y entonces se verá qué necesita la API.

### D4 — `MessageBoxes` y `ShowModelessForm` salen de la API

- `MessageBoxes` (estático, envuelve `MessageBox.Show`) se muda a `GitUI` tal cual. Los
  plugins que lo usen pasan a mostrar mensajes vía los servicios que ya reciben (inventario
  en tarea 5.1).
- `ShowModelessForm(IWin32Window?, ..., Func<Form> provideForm)` es una factoría de Forms en
  la firma pública: se retira de `IGitUICommands` y se recoloca como mecanismo interno de
  GitUI accesible a los plugins que lo usan (según inventario; hoy se estima uso mínimo).

### D5 — Guardarraíl: `UseWindowsForms=false` en el csproj

Último commit del change: `GitExtensions.Extensibility.csproj` fija
`<UseWindowsForms>false</UseWindowsForms>` (anulando el `true` global de
`Directory.Build.props`). A partir de ahí, reintroducir un tipo WinForms en la API **no
compila**. Es la versión barata del canary del 0.4 y su prueba de fuego anticipada.

### D6 — Orden de ejecución por capas, Verify en verde entre capas

Iconos (pequeño, calienta motores) → owners (amplio y mecánico) → settings (el profundo) →
varios y barrido final → guardarraíl. Cada capa termina con `eng/Verify.ps1` en verde y la
app arrancando; permite PR única con commits revisables o trocear en 2 PRs si la de
settings se complica (se decidirá al llegar, sin cambiar el change).

## Risks / Trade-offs

- **[Volumen de call sites]** Cientos de puntos tocados en GitUI/plugins/tests. → Mitigación:
  D1 minimiza los retoques (los `this` compilan solos); el resto lo guía el compilador
  (`TreatWarningsAsErrors` ya activo). Es trabajo mecánico, no de diseño.
- **[Regresión en settings de plugins]** Es el subsistema que cambia de patrón. → Smoke test
  manual guiado al final (abrir settings de cada plugin integrado, editar y guardar un valor,
  verificar persistencia) además de los unit tests.
- **[Traducciones de CredentialsControl]** El resx viaja con el control a GitUI; verificar
  que la tubería de traducción (ResourceManager recorre GitUI) lo sigue encontrando. →
  Comprobación explícita en tareas.
- **[Plugins externos rotos]** Cualquier plugin de terceros compilado contra la API antigua
  deja de cargar. → Aceptado por decisión previa (fork sin ecosistema binario); PluginManager
  ya no es una restricción.
- **[Choque con tags upstream futuros]** `Extensibility` y sus consumidores divergen fuerte
  del upstream; absorber v7.3+/v8 dará conflictos aquí. → Aceptado y previsto: la prioridad
  de sincronización es `GitCommands`; los conflictos de API se resuelven a favor del fork.

## Migration Plan

Sin despliegue: es refactor interno con la app WinForms como única consumidora real.
Rollback = revertir la PR. Los commits por capa (D6) permiten bisecar si algo regresa.

## Open Questions

- ¿Algún plugin integrado usa `CustomControl` con lógica no trivial que el registro de
  bindings de GitUI no cubra? (se responde con el inventario de la tarea 4.1).
- ¿`GitUIPostActionEventArgs` u otros EventArgs exponen más tipos WinForms además del owner?
  (barrido de la tarea 5.2 lo confirma).
