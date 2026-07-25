## Context

La shell WinForms (`Program.cs:113-118`) inicializa `JoinableTaskContext` creando un
`new Form()` efímero para capturar `WindowsFormsSynchronizationContext`:

```csharp
using (new Form())
{
    ThreadHelper.JoinableTaskContext = new JoinableTaskContext();
}
```

En Avalonia, el `SynchronizationContext` lo instala `AppBuilder.StartWithClassicDesktopLifetime()`
antes de llamar a `OnFrameworkInitializationCompleted()`. La hipótesis (a validar con el
spike) es que `SynchronizationContext.Current` dentro de `OnFrameworkInitializationCompleted()`
será `AvaloniaSynchronizationContext`, y que `JoinableTaskContext` lo captura correctamente.

El proyecto ya tiene `Avalonia` 11.3.18 como dependencia. Necesita añadir
`Microsoft.VisualStudio.Threading` 17.13.61 (ya en `Directory.Packages.props`).

## Goals / Non-Goals

**Goals:**
- Inicializar `ThreadHelper.JoinableTaskContext` en `OnFrameworkInitializationCompleted()`
- Validar que `FileAndForget` + `SwitchToMainThreadAsync` funcionan con `AvaloniaSynchronizationContext`
- Añadir un botón de test en `MainWindow` que demuestre el flujo completo (thread pool → main thread → actualizar UI)
- Cablear `TaskManager.ExceptionReporter` mínimo (escribir a debug/trace) para capturar excepciones de fire-and-forget durante las pruebas

**Non-Goals:**
- NO se referencia `GitCommands` ni ningún ensamblado del core
- NO se cablean los delegates de shell completos (`ShowError`, `PickFolder`) — eso es 1.1c
- NO se configura MSDI en la shell Avalonia — eso es 1.1c
- NO se migran tests de threading a Avalonia
- NO se modifica la inicialización de JTF en WinForms

## Decisions

### Decisión 1: Momento de inicialización — `OnFrameworkInitializationCompleted()`

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // AvaloniaSynchronizationContext is already installed by AppBuilder
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext();
        desktop.MainWindow = new MainWindow();
    }

    base.OnFrameworkInitializationCompleted();
}
```

El constructor de `JoinableTaskContext` captura `SynchronizationContext.Current`. En
Avalonia, este ya es `AvaloniaSynchronizationContext` en este punto porque
`StartWithClassicDesktopLifetime()` lo instala durante la inicialización de la plataforma.

**Alternativa considerada**: Inicializar en `MainWindow` constructor. Descartada: el JTC
debe estar disponible antes de que cualquier ViewModel o servicio lo use; inicializarlo en
la ventana fuerza un orden implícito que se romperá cuando haya múltiples ventanas.

### Decisión 2: Spike de validación — botón en MainWindow

Se añade un `Button` y un `TextBlock` en `MainWindow.axaml`. Al pulsar el botón:

1. `ThreadHelper.FileAndForget` lanza una tarea asíncrona
2. La tarea hace `await Task.Delay(500)` (simula trabajo en thread pool)
3. Llama a `await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()`
4. Actualiza `TextBlock.Text` con un timestamp

Esto prueba el flujo completo que usa el core (file → background work → back to UI).

```xml
<StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="10">
    <TextBlock x:Name="StatusText" Text="Pulsa el botón para probar el thread switching..." />
    <Button Content="Probar ThreadHelper" Click="OnTestThreadingClick" />
</StackPanel>
```

**Alternativa considerada**: Validar solo en debugger sin UI. Descartada: un botón es una
prueba reproducible, visible, y queda como canary permanente en el proyecto.

### Decisión 3: ExceptionReporter mínimo

`FileAndForget` captura excepciones y las reenvía a `TaskManager.ExceptionReporter`. Si no
se cablea, las excepciones solo se tracean (sin feedback visual). Para el spike se cablea
un reporter mínimo que escribe a `Debug.WriteLine`:

```csharp
TaskManager.ExceptionReporter = ex => Debug.WriteLine($"JTF Exception: {ex}");
```

Esto permite ver en la ventana de Output/consola si algo falla durante las pruebas.

**Alternativa considerada**: Esperar a 1.1c para todo el cableado. Descartada: si el spike
falla, sin ExceptionReporter no sabremos si fue por el JTC o por una excepción silenciada.

### Decisión 4: Dependencia — solo Microsoft.VisualStudio.Threading

El proyecto ya depende de `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` y
`CommunityToolkit.Mvvm`. Se añade únicamente `Microsoft.VisualStudio.Threading`. No se
añade referencia a `GitCommands`, `GitExtUtils` ni `Extensibility` — esos son para 1.1c.

## Risks / Trade-offs

- **[Riesgo: `JoinableTaskContext` no captura `AvaloniaSynchronizationContext`]** — Es el
  riesgo principal de este change. Si `SynchronizationContext.Current` es `null` o no es
  `AvaloniaSynchronizationContext` en `OnFrameworkInitializationCompleted()`, el JTC no
  tendrá forma de volver al hilo principal. **Mitigación**: el spike lo detecta
  inmediatamente. Si falla, opciones a explorar: (a) inicializar en otro hook del
  lifecycle, (b) instalar manualmente el SC con `SynchronizationContext.SetSynchronizationContext()`,
  (c) custom `JoinableTaskContext` con factory manual.

- **[Riesgo: `AvaloniaSynchronizationContext` no bombea mensajes como espera VS-Threading]** —
  `SwitchToMainThreadAsync` espera a que el SC esté libre. Si Avalonia no libera el SC
  mientras espera entrada, podría causar deadlocks. **Mitigación**: el spike con
  `Task.Delay` y `SwitchToMainThreadAsync` prueba el caso simple; si el SC de Avalonia
  difiere del de WinForms en comportamiento de bombeo, se detectará en pruebas más
  complejas (1.2+).

- **[Riesgo: `STAThread` no está en Avalonia puro]** — El `Program.cs` no usa `[STAThread]`
  porque `net10.0` sin `-windows` no lo requiere. Avalonia maneja el threading
  internamente. **Mitigación**: `JoinableTaskContext` no depende de STA; funciona con
  cualquier `SynchronizationContext`.

- **[Trade-off: El spike queda en MainWindow]** — Un botón de test no debería estar en
  producción. **Mitigación**: se eliminará o moverá a un panel de diagnóstico en 1.2
  cuando haya vistas reales.
