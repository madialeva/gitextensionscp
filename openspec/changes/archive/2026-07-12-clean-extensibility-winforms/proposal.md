# Change 0.2 — Limpiar los tipos WinForms de la API de Extensibility

## Why

`GitExtensions.Extensibility` es el contrato público entre el core, la UI y los plugins. Hoy
ese contrato **expone tipos de WinForms y GDI+ en sus firmas**, lo que encadena a Windows a
cualquier ensamblado que lo implemente o lo consuma. Mientras eso no se limpie, ni el core
puede compilarse multiplataforma (change 0.4) ni una futura shell Avalonia puede usar la API.
Es el primer desacoplamiento real de la Fase 0.

### Qué significa "limpiar los tipos WinForms de la API" — un ejemplo

Así se ve hoy el contrato que un plugin implementa y consume:

```csharp
// HOY — la API obliga a WinForms/GDI+ a todo el que la toca
public interface IGitPlugin
{
    Image? Icon { get; }          // System.Drawing.Image: un bitmap GDI+, tecnología solo-Windows
    ...
}

public interface IGitUICommands
{
    // System.Windows.Forms.IWin32Window: "un handle de ventana Win32" — el concepto
    // mismo solo existe en Windows
    bool StartCommitDialog(IWin32Window? owner, string? commitMessage = null, ...);
    // ~60 métodos más con el mismo parámetro owner, y factorías como Func<Form>
}
```

El problema no es estético: **el tipo de un parámetro es una dependencia**. Para implementar
`IGitPlugin` o llamar a `StartCommitDialog`, tu ensamblado necesita referenciar
`System.Windows.Forms` y `System.Drawing` enteros — y esos ensamblados solo existen en
Windows. Consecuencias en cadena: `GitCommands` (que referencia esta API) no puede compilar
como `net10.0` neutro; una shell Avalonia no podría ni referenciar la API sin cargar
WinForms; y un plugin queda soldado a la UI antigua aunque su lógica sea pura.

Limpiar es sustituir cada tipo de UI por un equivalente neutro que exprese la *intención*
sin imponer la *tecnología*:

```csharp
// DESPUÉS — misma intención, cero dependencia de UI
public interface IGitPlugin
{
    byte[]? IconData { get; }     // los bytes de un PNG; cada UI los materializa
                                  // (Image en WinForms, Bitmap en Avalonia)
    ...
}

public interface IGitUICommands
{
    bool StartCommitDialog(IWindow? owner, string? commitMessage = null, ...);
    // IWindow: interfaz marcadora propia = "algo que puede ser dueño de un diálogo";
    // la shell WinForms la implementa con sus Forms, la Avalonia con sus Windows
}
```

El caso más profundo es el sistema de settings: `ISetting.CreateControlBinding()` devuelve
un objeto cuyo `GetControl()` es un **control WinForms vivo** (hay hasta un `UserControl`
con diseñador, `CredentialsControl`, dentro del ensamblado de la API). Ahí no basta con
cambiar un tipo: la API debe volverse **declarativa** (un setting describe su nombre, tipo y
valor) y ser cada capa de UI quien aporte el renderizado.

## What Changes

- **BREAKING**: se rompe la API de plugins sin deprecaciones (política ya decidida: fork
  independiente, sin ecosistema binario de terceros que preservar).
- `IWin32Window? owner` → nueva interfaz neutra `IWindow` en ~60 métodos de `IGitUICommands`,
  en `GitUIEventArgs.OwnerForm` y resto de apariciones. Los Forms base de `GitUI` implementan
  `IWindow`, y un adaptador interno de GitUI la traduce a `IWin32Window` para `ShowDialog`.
- `Image?` (iconos de plugin en `IGitPlugin`/`GitPluginBase`) → datos neutros (PNG embebido).
  Migración de los iconos de los 12+ plugins integrados.
- **Settings declarativos**: `ISetting`/`SettingControlBinding`/`CustomControl` pierden todo
  tipo de control; la vinculación setting→control WinForms se muda a `GitUI` (que ya tiene un
  proveedor de bindings como fallback). `CredentialsControl` (UserControl) se muda a `GitUI`.
- `MessageBoxes` (envoltorio de `MessageBox.Show`) y `ShowModelessForm(Func<Form>)` salen de
  la API pública hacia `GitUI`.
- **Guardarraíl final**: `GitExtensions.Extensibility.csproj` compila con
  `UseWindowsForms=false` — cualquier regresión futura ni siquiera compila. (El retarget a
  `net10.0` neutro queda para el change 0.4.)
- La app WinForms sigue funcionando igual: esto mueve piezas de sitio, no cambia
  comportamiento visible.

## Capabilities

### New Capabilities

- `plugin-api`: el contrato público de extensibilidad (diálogos, plugins, settings) es
  neutro respecto a la tecnología de UI: sin tipos WinForms/GDI+ en firmas, owners
  abstractos, iconos como datos y settings declarativos.

### Modified Capabilities

<!-- Ninguna: local-verification y continuous-integration no cambian de requisitos. -->

## Impact

- **Ensamblado objetivo**: `src/app/GitExtensions.Extensibility` (leaf: sin referencias a
  otros proyectos del repo, solo AdysTech.CredentialManager y StrongOf).
- **Onda expansiva mecánica**: cientos de call sites en `GitUI` (los `Start*Dialog(owner...)`),
  los 12+ plugins integrados (iconos, settings con `CustomControl`), `GitCommands` y tests.
  Mucho volumen, poca decisión: el compilador guía.
- **Es el change más grande de la Fase 0**; las tareas van por capas (iconos → owner →
  settings → resto) y tras cada capa `eng/Verify.ps1` debe seguir en verde, con la app
  WinForms funcional.
- **Riesgo funcional**: regresiones en el diálogo de settings de plugins y en la carga de
  iconos — se cubre con smoke test manual además de los unit tests.
