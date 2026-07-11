# Tasks — Change 0.2: limpiar los tipos WinForms de la API de Extensibility

> Orden por capas (design D6). Al cerrar cada grupo: `eng/Verify.ps1` en verde y la app
> WinForms arranca. Los inventarios (1.1, 4.1, 5.1) se hacen con grep/compilador y se anotan
> aquí mismo como notas bajo la tarea.

## 1. Inventario

- [x] 1.1 Inventariar en `GitUI`, `src/plugins` y tests todos los consumidores de:
      `IWin32Window` de la API, `IGitPlugin.Icon`/`GitPluginBase.Icon`, `CustomControl`,
      `CreateControlBinding`, `MessageBoxes` y `ShowModelessForm`; anotar recuentos y casos
      no mecánicos como notas en este fichero

      **Notas del inventario (2026-07-11):**
      - `IWin32Window`: 272 usos en 40 ficheros de `src/` (90 en `GitUICommands.cs`, 83 en
        `IGitUICommands.cs`). También en `GitCommands` (ExceptionUtils, GitTagController,
        OsShellUtil) y `GitExtUtils` (ThemeFix): APIs propias de esos proyectos, **fuera del
        alcance 0.2** (irán en 0.3/0.4); aquí solo se toca lo que la API de Extensibility
        arrastre.
      - Iconos: los 11 PNG originales existen como ficheros (`Resources/IconX.png` en cada
        plugin); patrón uniforme `Icon = Resources.IconX` (accessor resx). Consumo de la API:
        `FormBrowse.cs:881` (menú Plugins) y `AddCommitTemplate(..., Image? icon, ...)`
        (GitHub3, 3 llamadas pasando `Icon`).
      - Settings: los bindings concretos **ya viven en GitUI** (`SettingControlBindings/`,
        10 ficheros + `SettingControlBindingsProvider`); en Extensibility solo quedan la
        abstracción (`ISettingControlBinding`, `SettingControlBinding<,>`), las propiedades
        `CustomControl` y `CredentialsControl`. `PseudoSetting` es el más acoplado (3 refs).
      - `MessageBoxes`: hay DOS clases homónimas — `Extensibility.MessageBoxes` y
        `GitUI.MessageBoxes` (esta con muchos más usos). Decisión: fusionar la de la API en
        la de GitUI; los usos desde plugins (Gource, GitHub3…) migran a la copia fusionada
        vía… (ver tarea 5.1; los plugins referencian GitUI? NO → los usos de plugins
        necesitan alternativa, resolver en 5.1).
      - `ShowModelessForm`: solo 3 call sites, todos internos de GitUI, ningún plugin →
        retirada de la interfaz sin migración.
      - Sin `using System.Windows.Forms/Drawing` explícitos en Extensibility: llegan como
        **global usings implícitos** de `UseWindowsForms=true`; el guardarraíl 6.1 los
        elimina y hará visible cualquier resto.

## 2. Capa iconos

- [x] 2.1 `IGitPlugin.Icon: Image?` → `IconData: byte[]?` (y `GitPluginBase`); helper para
      leer el PNG embebido del ensamblado del plugin
      → `GitPluginBase.SetIconFromEmbeddedPng(fileName)`; también `AddCommitTemplate` y
      `CommitTemplateManager`/`CommitTemplateItem` (GitCommands) pasan a `byte[]`
- [x] 2.2 Migrar los iconos de los 12+ plugins integrados a `EmbeddedResource` PNG
      → 11 plugins con icono; los PNG originales ya existían como ficheros
- [x] 2.3 Adaptar los puntos de consumo en `GitUI` (menú/página de plugins) para
      materializar `byte[]` → `Image`; verificar visualmente que los iconos aparecen
      → `IconDataExtensions.ToImage/ToPngBytes`; consumos: FormBrowse (menú Plugins),
      FormSettings (árbol), FormCommit (plantillas), GitHub3 (menú contextual, helper local);
      verificación visual pendiente del smoke test 6.2. Unit tests 15/15 verdes tras la capa

## 3. Capa owners

- [x] 3.1 Crear `IWindow` en `Extensibility`; sustituir `IWin32Window` en `IGitUICommands`
      (~60 métodos), `GitUIEventArgs.OwnerForm`, `GitUIPostActionEventArgs` y cualquier otra
      firma pública
      → también `MessageBoxes` de Extensibility (acepta `IWindow`, castea dentro) y
      `GitTagController` (GitCommands)
- [x] 3.2 Implementar `IWindow` en los Forms base de `GitUI` y crear el helper interno de
      traducción `IWindow` → `IWin32Window` en la implementación de `GitUICommands`
      → `GitExtensionsFormBase`, `GitExtensionsControl` (ResourceManager) y `BugReportForm`;
      adaptadores en `GitUI.WindowExtensions` (`AsWinFormsWindow`/`AsApiWindow`)
- [x] 3.3 Arreglar los call sites no mecánicos que el compilador señale (los `this` deben
      compilar sin cambios); `eng/Verify.ps1` en verde
      → 143 conversiones automatizadas (script guiado por errores del compilador) + ~30
      manuales (plugins con diálogos propios, LeftPanel/ParentWindow, TaskDialog, tests)

## 4. Capa settings

- [x] 4.1 Confirmar con el inventario qué plugins personalizan `CustomControl` y decidir su
      equivalente en el registro de bindings de GitUI (anotar aquí la decisión por plugin)
      → NINGÚN plugin usa `CustomControl` (solo GitUI: `StringComboBoxAdapter`, que ahora
      lleva su ComboBox como propiedad propia). Lo que sí usan los plugins: `PseudoSetting`
      con controles vivos (GitHub3: 2 LinkLabel; BackgroundFetch: texto multilínea) →
      `PseudoSetting` reescrito como datos (Text/Caption/Height) y nuevo `LinkSetting`
      (Text + Action) para los enlaces
- [x] 4.2 Dejar `ISetting` y settings concretos como datos puros: eliminar
      `CreateControlBinding()` y las propiedades `CustomControl`
- [x] 4.3 Mover `ISettingControlBinding`, `SettingControlBinding<,>` y `CredentialsControl`
      (con Designer y resx) a `GitUI`; completar el proveedor de bindings de GitUI como único
      mecanismo de renderizado
      → namespace `GitUI.SettingControlBindings`; sorpresa: el generador Roslyn del repo
      crea ctors [Obsolete] sin parámetros — el binding llama al ctor real con argumentos
      explícitos. Tests de bindings adaptados a inyección por ctor. 15/15 suites verdes
- [ ] 4.4 Verificar la tubería de traducción del `CredentialsControl` reubicado (el resx debe
      seguir entrando en los `.xlf`) y el escenario de credenciales; `eng/Verify.ps1` en verde
      → parcial: Verify verde tras la capa; la comprobación de traducción/credenciales se
      hace con el smoke test 6.2

## 5. Capa varios y barrido

- [x] 5.1 Mover `MessageBoxes` a `GitUI`; migrar los usos desde plugins según inventario
      → destino corregido: `GitExtUtils` (GitUI no es visible para plugins ni GitCommands;
      GitExtUtils lo es para todos y es el candidato al ensamblado Windows-only del 0.3).
      Firmas ya en `IWindow` desde la capa owners, cero cambios en call sites de plugins
- [x] 5.2 Retirar `ShowModelessForm(Func<Form>)` de `IGitUICommands` y recolocar el mecanismo
      en GitUI para sus consumidores; barrido final de `using System.Windows.Forms|Drawing`
      en `Extensibility` (debe quedar a cero)
      → método fuera de la interfaz (queda público en `GitUICommands`; único caller externo,
      RevisionGridControl, castea). Barrido extra que destapó el guardarraíl:
      `UIExtensions` (CheckBox) movido a GitExtUtils; `TranslationUtil` convertido a
      detección de tipos WinForms por reflexión (sin referencia compile-time; la tubería
      entera se reemplaza en Fase 4); `Color` se queda (System.Drawing.Primitives es
      multiplataforma)

## 6. Guardarraíl y cierre

- [x] 6.1 Fijar `<UseWindowsForms>false</UseWindowsForms>` en
      `GitExtensions.Extensibility.csproj` y compilar la solución completa
      → el guardarraíl destapó 5 restos invisibles al grep (llegaban por los global usings
      implícitos de WinForms): `Point`/`Color` (System.Drawing.Primitives, multiplataforma →
      solo faltaba el `using System.Drawing;` explícito en IBrowseRepo/IGitModule/
      SettingsSource); `Application.ExecutablePath` en DebugHelpers (→
      `Environment.ProcessPath`); `Font` (GDI+, Windows-only) en FontParser y
      SettingsSource.GetFont/SetFont (→ FontParser movido a GitExtUtils y Get/SetFont
      convertidos en métodos de extensión allí; único consumidor: AppSettings); y
      `ContextMenuStrip` en `IRepositoryHostPlugin.ConfigureContextMenu` (→ método retirado
      de la interfaz; BlameControl construye ahora el menú "View in {0}" con la API neutral
      ya existente — Name, IconData, GetHostedRemotesForModule/GetBlameUrl — y GitHub3
      pierde su código WinForms de menús). Consumidores arreglados con `using GitExtUtils;`
      (GitCommands, ResourceManager, BugReporter, 4 plugins); FontParserTests movido a
      GitExtUtils.Tests; ambigüedad GitUI.MessageBoxes/GitExtUtils.MessageBoxes en Gource y
      FindLargeFiles resuelta cualificando hacia GitExtUtils. Solución completa compila con
      el guardarraíl activo
- [ ] 6.2 `eng/Verify.ps1` completo en verde + smoke test manual del escenario de la spec:
      arrancar app → abrir repo → ejecutar un plugin → settings del plugin → editar y guardar
- [ ] 6.3 Actualizar la hoja de ruta (0.2 completado) y registrar en el registro de
      decisiones interno las decisiones tomadas durante la implementación
