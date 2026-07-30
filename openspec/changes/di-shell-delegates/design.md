## Context

La shell Avalonia existe (1.1a), compila con tema Fluent, y ya inicializa `JoinableTaskContext` (1.1b). Pero no tiene un contenedor DI ni los tres delegates de shell cableados, lo que la hace inoperable para cualquier código del core que necesite resolver servicios o interactuar con el usuario.

La shell WinForms (en `GitExtensions/Program.cs`) construye su contenedor MSDI llamando a `AddGitExtensions()` y cablea los delegates así:

```csharp
// Delegates en WinForms
TaskManager.ExceptionReporter = Application.OnThreadException;               // WinForms
UserMessageHandler.ShowError = (owner, text, caption) => MessageBoxes.ShowError(owner, text, caption);  // WinForms MessageBox
OsShellUtil.PickFolder = (owner, path) => { /* FolderBrowserDialog WinForms */ };
```

La shell Avalonia necesita equivalentes Avalonia de estos tres delegates, y su propio contenedor MSDI con los servicios del core (`GitExtUtils` + `GitCommands`) pero sin referenciar `GitUI` (WinForms).

## Goals / Non-Goals

**Goals:**
- Construir el contenedor MSDI en `App.axaml.cs` con servicios de `GitExtUtils` + `GitCommands`
- Cablear `TaskManager.ExceptionReporter` → diálogo Avalonia que muestra el error + stack trace
- Cablear `UserMessageHandler.ShowError` → ventana modal Avalonia con título y mensaje
- Cablear `OsShellUtil.PickFolder` → `IStorageProvider.OpenFolderPickerAsync` vía `JoinableTaskFactory.Run` (llamada desde el hilo UI; bridge sync→async)
- Almacenar `IServiceProvider` como propiedad estática para consumo por VMs (Service Locator de transición)
- El proyecto `GitExtensions.Avalonia` referencia `GitCommands` (ya referencia `GitExtUtils`)
- `eng/Verify.ps1` sigue pasando 15/15 y WinForms compila/funciona sin cambios

**Non-Goals:**
- NO se referencia `GitUI` (WinForms) ni se usa `AddGitUI()` o `AddGitExtensions()`
- NO se crean ViewModels ni vistas reales (eso es 1.2)
- NO se introduce inyección por constructor en la shell Avalonia (la propiedad Service Locator es el paso intermedio hasta 1.2)
- NO se modifica `ServiceCollectionExtensions` de otros ensamblados
- NO se cablean delegates adicionales (plugins, settings, etc.) — solo los tres de shell
- NO se crea un sistema de navegación entre vistas

## Decisions

### Decisión 1: Dónde construir el contenedor MSDI — `App.OnFrameworkInitializationCompleted()`

Se construye después de inicializar `ThreadHelper.JoinableTaskContext` y antes de crear `MainWindow`. Este es el orden correcto porque el cableado de `ExceptionReporter` depende de que JTF esté operativo (muestra diálogos en el hilo principal), y `MainWindow` (o sus futuras VMs) puede necesitar servicios del contenedor.

```
Orden en OnFrameworkInitializationCompleted():
1. Inicializar JoinableTaskContext (ya hecho en 1.1b)
2. Construir ServiceProvider
3. Cablear delegates (ExceptionReporter, ShowError, PickFolder)
4. Guardar ServiceProvider en App.Services
5. Crear MainWindow
```

**Alternativa considerada**: `Program.cs` (antes de `StartWithClassicDesktopLifetime`). Descartada: en ese punto `AvaloniaSynchronizationContext` aún no está instalado, así que JTF no puede inicializarse. Y los delegates que muestran UI requieren que la plataforma de Avalonia esté inicializada.

### Decisión 2: Registro de servicios — método `AddAvaloniaServices` propio

`GitExtensions/ServiceCollectionExtensions.AddGitExtensions()` referencia `GitUI` (llama a `AddGitUI()`), que es WinForms. No se puede usar desde el proyecto Avalonia.

En su lugar, se crea `ServiceCollectionExtensions.AddAvaloniaServices()` en el proyecto `GitExtensions.Avalonia` que registra el subconjunto portátil:

```csharp
services.AddGitExtUtils();         // SubscribableTraceListener
// Servicios shell-agnostic que AddGitExtensions crea, sin depender de GitUI:
services.AddSingleton<IFileSystem>(new FileSystem());
services.AddSingleton<IGitDirectoryResolver>(new GitDirectoryResolver(fileSystem));
// NOTA: RepositoryDescriptionProvider, AppTitleGenerator, LinkFactory se posponen a 1.2
services.AddGitCommands();         // IGitExecutorProvider, submodules, branch normaliser
```

`AddGitCommands` requiere `IGitDirectoryResolver`, por eso se registra antes.

**Alternativa considerada**: referenciar `GitExtensions` y no usar los tipos de `GitUI`. Descartada: `AddGitExtensions` llama a `AddGitUI()` en un `using GitUI`, que falla en compilación porque el proyecto Avalonia no conoce ese namespace. Además, `AddGitExtensions` depende de `ResourceManager` (WinForms) y registra servicios como `IAppTitleGenerator` que no se necesitan aún.

**Alternativa considerada**: factorizar `IFileSystem`/`IGitDirectoryResolver` a `GitExtUtils` o a un método independiente en `GitCommands`. Descartada: añadiría un refactor a ensamblados del core cuyo único propósito es evitar ~5 líneas en el Avalonia `ServiceCollectionExtensions`. No merece la pena. Si en 1.2 o posteriores se necesita más granularidad, se reconsidera.

### Decisión 3: Cableado de `TaskManager.ExceptionReporter`

El delegate `Action<Exception>` recibe la excepción ya demystificada (la llama `TaskManager.ReportExceptionOnMainThreadAsync()` vía `SwitchToMainThread`). Se muestra en un diálogo Avalonia simple: una `Window` con título "Error", un `TextBox` multilínea readonly con el mensaje y stack trace, y un botón "OK".

```csharp
TaskManager.ExceptionReporter = ex =>
{
    var dialog = new ExceptionDialog(ex);
    dialog.ShowDialog(MainWindow); // bloquea hasta que el usuario cierra
};
```

La ventana de diálogo se define en un fichero nuevo `Services/ExceptionDialog.axaml`.

**Alternativa considerada**: `Debug.WriteLine` (status quo del 1.1b). Descartada: sin feedback visual, las excepciones de `FileAndForget` serían invisibles para el usuario final y para el desarrollo de cambios posteriores (1.2+).

### Decisión 4: Cableado de `UserMessageHandler.ShowError`

El delegate `Action<IWindow?, string, string?>` recibe un owner opcional, un texto y un caption opcional. Se muestra un diálogo modal similar al de ExceptionReporter pero más simple (solo título + mensaje, sin stack trace). En Avalonia 11.x no hay `MessageBox` nativo, así que se implementa con una `Window` personalizada.

```csharp
UserMessageHandler.ShowError = (owner, text, caption) =>
{
    var dialog = new ErrorDialog(caption ?? "Error", text);
    Window? parent = ResolveOwner(owner) ?? MainWindow;
    dialog.ShowDialog(parent);
};
```

`ResolveOwner` intenta extraer una `Window` del `IWindow?` (si implementa un adapter de Avalonia) o devuelve `null` para usar el MainWindow por defecto.

### Decisión 5: Cableado de `OsShellUtil.PickFolder`

El delegate `Func<IWindow?, string?, string?>` es síncrono, pero `IStorageProvider.OpenFolderPickerAsync` es asíncrono. Se usa `ThreadHelper.JoinableTaskFactory.Run()` para hacer el bridge sync→async desde el hilo UI:

```csharp
OsShellUtil.PickFolder = (owner, selectedPath) =>
{
    Window? parent = ResolveOwner(owner) ?? MainWindow;

    return ThreadHelper.JoinableTaskFactory.Run(async () =>
    {
        IReadOnlyList<IStorageFolder> folders =
            await parent.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Select folder",
                    AllowMultiple = false,
                    SuggestedStartLocation = selectedPath is not null
                        ? await parent.StorageProvider.TryGetFolderFromPathAsync(selectedPath)
                        : null
                });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    });
};
```

**Razonamiento sobre `JoinableTaskFactory.Run` + deadlocks**: `PickFolder` es llamado desde el hilo UI (los call sites están en event handlers de WinForms y lo mismo ocurrirá en Avalonia). `JTF.Run` bloquea el hilo actual pero permite que el `JoinableTaskContext` re-entrante ejecute continuaciones en el mismo hilo. Esto es el caso de uso exacto para el que VS-Threading existe. Probado indirectamente en 1.1b (el botón de spike usaba `FileAndForget` + `SwitchToMainThreadAsync` sin deadlock).

**Alternativa considerada**: cambiar la firma a `Func<IWindow?, string?, Task<string?>>` asíncrona. Descartada: cambiaría la API del core (`OsShellUtil.PickFolder`) y todos sus call sites (~15 en GitUI + GitCommands), forzando un refactor transversal que no aporta valor para este change. Si en el futuro se detectan problemas, se reconsidera.

### Decisión 6: Adaptador `IWindow` ↔ `Avalonia.Controls.Window`

Siguiendo el patrón de `Win32WindowAdapter` en `GitUI/WindowExtensions.cs`, se crea `AvaloniaWindowAdapter`:

```csharp
// Services/AvaloniaWindowAdapter.cs
internal sealed class AvaloniaWindowAdapter(Window window) : IWindow
{
    internal Window Window { get; } = window;
}
```

`ResolveOwner(IWindow?)` extrae el `Window`:

```csharp
private static Window? ResolveOwner(IWindow? window) =>
    (window as AvaloniaWindowAdapter)?.Window;
```

Y `AsApiWindow()` para exponer vents Avalonia como `IWindow`:

```csharp
internal static IWindow? AsApiWindow(this Window? window) =>
    window is null ? null : new AvaloniaWindowAdapter(window);
```

### Decisión 7: Exponer `IServiceProvider` como Service Locator

Se añade una propiedad estática `App.ServiceProvider` inicializada en `OnFrameworkInitializationCompleted()`. Esto permite a las futuras VMs y views acceder a servicios sin inyección por constructor (transición hasta que 1.2 introduzca la inyección real).

```csharp
public static IServiceProvider ServiceProvider { get; private set; } = null!;
```

**Alternativa considerada**: exponer el `IServiceProvider` como un singleton en el contenedor y pasarlo al constructor de MainWindow. Descartada: obliga a propagar el provider manualmente por la jerarquía de views hasta que haya inyección por constructor. La propiedad estática es más simple y se eliminará cuando 1.2 añada soporte completo de DI.

### Decisión 8: Mantener el botón de spike de 1.1b

El botón "Probar ThreadHelper" de `MainWindow` que se añadió en 1.1b se conserva. Sigue siendo útil como canary para verificar que JTF funciona tras los cambios de este change. Se eliminará o moverá a un panel de diagnóstico en 1.2.

## Riesks / Trade-offs

- **[Riesgo: `JoinableTaskFactory.Run` en `PickFolder` puede deadlockear si se llama desde thread pool]** — `OsShellUtil.PickFolder` es un delegate estático; nada impide que código futuro lo llame desde un hilo background. Si eso ocurre, `JTF.Run` bloquearía el hilo background esperando al hilo principal, y el hilo principal esperaría al background (si hay dependencia mutua). **Mitigación**: todos los call sites actuales están en handlers de UI (botones de Forms, etc.). La documentación del delegate en `OsShellUtil` debería advertir "must be called from UI thread". Si en el futuro aparece un call site en background, se refactoriza el delegate a async.

- **[Riesgo: `Microsoft.Extensions.DependencyInjection` no está en `Directory.Packages.props`]** — El paquete MSDI fue añadido en el cambio 1.0 (`msdi-migration`) a `Directory.Packages.props` y referenciado globalmente vía `Directory.Build.targets`. El proyecto Avalonia hereda esta referencia. **Mitigación**: verificado — `dotnet build` ya funciona tras 1.1b, y `GitExtUtils.csproj` usa MSDI sin `PackageReference` explícito.

- **[Trade-off: `ServiceProvider` estático]** — Es un anti-patrón Service Locator. **Mitigación**: se usa solo como paso intermedio hasta 1.2 (inyección por constructor). La propiedad se marcará como `[Obsolete]` o se eliminará cuando todas las VMs usen DI por constructor.

- **[Trade-off: `AddAvaloniaServices` duplica lógica de `AddGitExtensions`]** — Los registros de `IFileSystem`/`IGitDirectoryResolver` están en dos sitios. **Mitigación**: son 5 líneas y los tipos registrados son estables. Si en 1.2 se necesita más granularidad, se puede factorizar un método `AddCoreServices()` en `GitCommands` que ambos shells llamen.

- **[Trade-off: Diálogos personalizados en lugar de toolkit de diálogos]** — Avalonia no incluye `MessageBox` nativo. Existen paquetes (MessageBox.Avalonia, DialogHost de Material.Avalonia) pero añadir una dependencia para dos diálogos simples es excesivo. **Mitigación**: si en fases posteriores se necesitan diálogos más complejos (confirmación, input), se reconsidera.

## Open Questions

- ¿Debe `UserMessageHandler.ShowError` ser bloqueante (modal) o no bloqueante? En WinForms, `ShowError` → `MessageBox.Show` que es modal (bloquea el hilo UI hasta que el usuario cierra). En Avalonia seguimos el mismo comportamiento con `ShowDialog()`. Si en el futuro aparece un caso donde bloquear la UI es problemático, se puede añadir una sobrecarga no-bloqueante.
- ¿Hay que exponer `MainWindow` como propiedad estática en App? `ExceptionReporter` y `PickFolder` necesitan referencia a la ventana principal como owner de diálogos. Se puede almacenar como `App.MainWindow` (ya que `desktop.MainWindow` es una propiedad del lifetime) o pasarlo explícitamente. Se decide usar `desktop.MainWindow` cuando esté disponible y cachearlo si es necesario.
