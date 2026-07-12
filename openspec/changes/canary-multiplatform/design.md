# Design — Change 0.4: canary multiplataforma

## Context

Tras el 0.2 y el 0.3, los tres ensamblados del core (`Extensibility`, `GitExtUtils`,
`GitCommands`) tienen `UseWindowsForms=false` y compilan limpios. Pero los tres heredan el TFM
`net10.0-windows` del `Directory.Build.props`. Además, `GitCommands` arrastra dos deudas del 0.3:

1. Una referencia temporal a `GitExtUtils.WinForms` para usar `MessageBoxes` en 4 call sites.
2. Sus tests (`GitCommands.Tests`) referencian `ResourceManager` (WinForms) y heredan
   `net10.0-windows`, por lo que no se pueden ejecutar en Linux tal cual.

El cambio consiste en tres movimientos coordinados: (a) retargetear los ensamblados a `net10.0`,
(b) abstraer las notificaciones de usuario que aún atan `GitCommands` al ensamblado WinForms, y
(c) partir/filtrar los tests para que una porción significativa corra en Linux en CI.

### Restricciones

- La app WinForms debe seguir compilando y funcionando exactamente igual (la verificación
  completa en Windows sigue siendo `eng/Verify.ps1` en verde, 15 suites).
- El TFM neutro `net10.0` es compatible hacia arriba: un ensamblado `net10.0` puede ser
  referenciado por un proyecto `net10.0-windows` sin problemas.
- El cambio no toca `GitUI`, `ResourceManager`, plugins ni tests de UI.

## Goals / Non-Goals

**Goals:**

- `Extensibility`, `GitExtUtils` y `GitCommands` compilan con TFM `net10.0` y
  `UseWindowsForms=false` (ambos).
- `GitUIPluginInterfaces` también compila con TFM `net10.0` y `UseWindowsForms=false` (es
  dependencia transitiva de `GitCommands` y no usa WinForms en absoluto).
- `GitCommands` pierde la referencia a `GitExtUtils.WinForms`; los avisos al usuario pasan por
  un callback instalable (mismo patrón que `TaskManager.ExceptionReporter`).
- Existe una pata Linux en CI que compila los ensamblados retargeteados y ejecuta los tests de
  `GitCommands` que no dependen de WinForms/ResourceManager.
- La app WinForms sigue compilando y pasando `eng/Verify.ps1` completa en Windows.

**Non-Goals:**

- Retargetear el resto de la solución (GitUI, ResourceManager, plugins, BugReporter,
  GitExtensions.exe…) a `net10.0` — eso es para la Fase 1.
- Diseñar el sistema de notificaciones de la shell Avalonia (solo se crea el punto de
  instalación neutro; cada shell conecta lo suyo).
- Mover/refactorizar `ResourceManager` para que sea multiplataforma (toda la tubería de
  traducción se reescribe en Fase 4; aquí solo se evita que los tests del core la necesiten).
- Cambiar el comportamiento visible de la app WinForms.

## Decisions

### D1 — Abstracción de notificaciones al usuario: delegado estático, no interfaz inyectada

Los 4 call sites actuales de `MessageBoxes` en `GitCommands` no usan el valor de retorno:
son avisos *fire-and-forget* (errores y advertencias). La abstracción más ligera que sigue el
patrón ya establecido en el 0.3 es un delegado estático:

```csharp
// En GitExtUtils (neutro, junto a TaskManager)
public static class UserMessageHandler
{
    // La shell instala esto; default no-op (Trace).
    public static Action<IWindow?, string/*text*/, string?/*caption*/> ShowError { get; set; }
        = (owner, text, caption) => Trace.TraceWarning($"[UserMessage] {caption}: {text}");
}
```

Los 4 call sites pasan de `MessageBoxes.Show*(…)` a `UserMessageHandler.ShowError(…)`. El
ensamblado `GitExtUtils.WinForms` conserva `MessageBoxes` (lo usan `GitUI`, plugins…) y además
instala el delegado en su inicialización estática o la shell lo conecta en `Program.cs`:

```csharp
UserMessageHandler.ShowError = (owner, text, caption) =>
    MessageBoxes.ShowError(owner, text, caption ?? "Error");
```

**Alternativas consideradas:**

- **Interfaz `IUserNotificationService` inyectada por DI.** Sería lo "correcto" en MVVM pero
  exigiría introducir un contenedor de DI en el core antes de tiempo. Además, los 4 call sites
  están en clases estáticas o helpers (`ExceptionUtils`, `GitVersion`) donde la inyección
  obligaría a cambiar firmas públicas. El delegado estático es consistente con
  `TaskManager.ExceptionReporter` (0.3) y se puede migrar a DI más adelante sin cambiar a los
  consumidores.
- **Neutral enums (`MessageBoxButton`, `MessageBoxImage`) + método `Show` con retorno.** Los
  4 call sites no usan el retorno; añadir tipos neutrales que nadie consume es speculación.
- **Dejar las 4 llamadas como están y condicionarlas con `#if WINDOWS`.** Viola la regla de
  "core no depende de Windows" y el guardarraíl `UseWindowsForms=false` lo rechazaría.

### D2 — Partición de tests: GitCommands.Tests gana un TFM `net10.0` sin referencias Windows

`GitCommands.Tests` actualmente referencia `ResourceManager` (WinForms). Esa dependencia viene
de un solo fichero de test (`GitCommandHelpersTest.cs`) que usa `LocalizationHelpers` de
`ResourceManager`. Opciones:

**A. Mover ese test a un proyecto Windows-only** (p.ej. `GitCommands.WinForms.Tests`) y que
`GitCommands.Tests` se quede solo con referencias `net10.0`. Es limpio pero crea otro proyecto
de tests para un solo fichero.

**B. Multitarget en `GitCommands.Tests`** (`<TargetFrameworks>net10.0;net10.0-windows</…>`)
con `#if WINDOWS` alrededor del fichero ofensor y de la referencia a `ResourceManager`. Es un
solo proyecto pero añade complejidad de build condicional.

**C. Hacer portable el `LocalizationHelpers` que necesita el test** (extraerlo de
`ResourceManager` a `GitExtUtils` o a `GitCommands`, ya que `GitCommands` es quien lo llama en
producción). El método `GetRelativeDateString` es pura manipulación de strings y el test solo
necesita `AppSettings.CurrentTranslation = "English"`. Mover `LocalizationHelpers` es el cambio
más alineado con la arquitectura (la localización de strings del core no debería vivir en
`ResourceManager`, que es la capa de traducción de controles WinForms).

**Decisión: opción C** — mover `LocalizationHelpers` y `AppSettings.CurrentTranslation` (si no
está ya en GitCommands — está en `AppSettings` que ya es de GitCommands) al core, eliminando la
dependencia de los tests hacia `ResourceManager`. Esto de paso limpia una dependencia
arquitectural incorrecta (ResourceManager → GitCommands hoy, pero GitCommands llama a
ResourceManager.LocalizationHelpers — no es una dependencia circular de proyecto, pero sí
conceptual: el core no debería llamar a la capa de UI).

Si `LocalizationHelpers` usa `Strings` (recursos `.resx` de ResourceManager), se evalúa mover
también la tabla de strings de fechas relativas a `GitCommands` (son strings fijos, no
dependientes de traducciones XLIFF).

### D3 — Linux CI: script/parámetro nuevo o `eng/Verify.ps1` con switch de plataforma

`eng/Verify.ps1` hoy compila la solución completa (`GitExtensions.slnx`) en Release y ejecuta
todos los tests bajo `tests/app/UnitTests` y `tests/plugins/UnitTests`. En Linux, la solución
completa no compila (GitUI, GitExtUtils.WinForms, etc. siguen siendo `net10.0-windows`).

**Decisión: nuevo script `eng/Verify-Linux.ps1`** que compila un subset de proyectos (los
retargeteados + sus tests) con `dotnet build` y ejecuta `dotnet test` sobre los proyectos de
tests que compilan en Linux. El workflow YAML (`fork-ci.yml`) gana un job `linux` con matriz
`os: [windows-latest, ubuntu-latest]` y cada SO invoca su script.

**Alternativa considerada:** un solo script con parámetro `-Platform`. Añade complejidad de
control de flujo sin ganancia real; el script Windows existente está diseñado para compilar la
solución entera y ejecutar todas las suites — mantenerlo intacto y crear uno específico para
Linux es más mantenible.

El script Linux:
1. `dotnet build` los proyectos `net10.0`: `Extensibility`, `GitExtUtils`, `GitCommands`,
   `GitUIPluginInterfaces`, y el proyecto de tests de `GitCommands`.
2. `dotnet test` solo `GitCommands.Tests`.

### D4 — Ubicación de la sobrescritura de TFM: por proyecto, no global

Los tres ensamblados + `GitUIPluginInterfaces` fijan `<TargetFramework>net10.0</TargetFramework>`
explícitamente en su csproj, rompiendo la herencia de `eng/RepoLayout.props`. No se cambia
`SolutionTargetFramework` globalmente porque el 95% de la solución sigue siendo
`net10.0-windows` y lo seguirá siendo durante varias fases.

Cada csproj que se retargetea también fija explícitamente `<UseWindowsForms>false</…>` si no lo
tenía ya. `GitCommands` es el único de los cuatro que no lo tenía (heredaba `true`), así que es
el único que gana el guardarraíl nuevo.

### D5 — GitUIPluginInterfaces: retarget trivial

Este proyecto solo referencia `GitExtUtils`, `Microsoft.VisualStudio.Composition`,
`System.Reactive.Interfaces` y `JetBrains.Annotations` — todas librerías `netstandard` o
multiplataforma. No usa WinForms en absoluto (cero `using System.Windows.Forms`). Retargetearlo
a `net10.0` + `UseWindowsForms=false` es cambiar dos líneas en el csproj. Es necesario porque
`GitCommands` lo referencia como `ProjectReference` y un ensamblado `net10.0` no puede depender
de uno `net10.0-windows`.

### D6 — Orden de ejecución: tres capas con Verify entre ellas

1. **Retarget mecánico**: cambiar TFMs + `UseWindowsForms` en los 4 csproj, eliminar
   `ProjectReference` a `GitExtUtils.WinForms` en `GitCommands.csproj`. El build de la solución
   completa fallará (los 4 call sites de MessageBoxes se quedan sin tipo). → Esto es esperado;
   confirma que son los únicos puntos de acoplamiento.
2. **Abstracción de MessageBoxes**: crear `UserMessageHandler` en `GitExtUtils`, adaptar los 4
   call sites, instalar el delegado en el arranque WinForms. → `eng/Verify.ps1` en verde en
   Windows (build completo + 15 suites).
3. **Linux CI y tests**: mover `LocalizationHelpers`, limpiar referencias de
   `GitCommands.Tests`, crear `Verify-Linux.ps1`, añadir job Linux al workflow. → CI verde en
   ambos SO.

## Risks / Trade-offs

- **[TFM `net10.0` compatible hacia arriba pero no hacia abajo]** Un proyecto
  `net10.0-windows` puede referenciar uno `net10.0`, pero no al revés. → Se verifica que
  ningún proyecto `net10.0` referencie accidentalmente uno `net10.0-windows`. El compilador lo
  rechaza, así que es un riesgo autoverificado.
- **[`LocalizationHelpers` tiene más acoplamientos de los previstos]** Si el método usa
  recursos `.resx` de `ResourceManager`, moverlo puede requerir mover también los strings.
  → El inventario de la tarea 1.1 lo destapa antes de mover; si la extracción es inviable, se
  cae en la opción B (multitarget con `#if WINDOWS` en el test).
- **[El `dotnet test` en Linux puede fallar por paths con `\` o suposiciones de filesystem]**
  `GitCommands` trabaja con rutas de git y filesystem; sus tests usan `System.IO.Abstractions`
  y LibGit2Sharp (multiplataforma) para el setup. → Probablemente pasen sin cambios, pero si
  alguno falla se diagnostica en la tarea 3.3 y se corrige (cambiando `\` por `/` o usando
  `Path.Combine`). El propósito del canary es justamente destapar esto.
- **[La app WinForms podría no arrancar si el delegado no se instala a tiempo]** Al mover
  `MessageBoxes` a delegado, si la inicialización del delegado ocurre después del primer aviso
  (p.ej. `GitVersion` en el arranque), el mensaje se pierde (cae en `Trace`). → Se instala en
  el constructor estático de `GitExtUtils.WinForms` (se ejecuta al cargar el ensamblado, antes
  de cualquier call site) o en `Program.Main` antes de cualquier uso de `GitCommands`.

## Open Questions

- ¿Vale la pena mover `LocalizationHelpers` al core o es más simple el multitarget? (Se
  responde en la tarea 1.1 con el inventario del fichero.)
- ¿Qué runner de Linux usar en CI? `ubuntu-latest` es el estándar; la imagen incluye .NET SDK
  y git. Se usará a menos que surja un problema específico.
