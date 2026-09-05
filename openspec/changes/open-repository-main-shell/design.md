## Context

La motivación y el alcance funcional están en `proposal.md`; los contratos observables están en las specs del change. La shell actual crea `MainWindow` directamente y ya instala en `App.OnFrameworkInitializationCompleted()` el proveedor MSDI, `JoinableTaskContext` y los delegates estáticos de `TaskManager`, `UserMessageHandler` y `OsShellUtil`.

El core ya contiene las superficies necesarias para este slice: `RepositoryHistoryManager.Locals` para el historial local, `IGitExecutorProvider`/`GitModule` para validar una ruta y `IGitModule` para obtener rama, remotes y estado. La referencia visual JavaFX de `/home/arume/src/arume` usa una composición tipo `BorderPane` con title bar superior, rails laterales, workspace central y status bar inferior.

## Goals / Non-Goals

**Goals:**

- Construir una composición Avalonia estable que sirva de marco para los slices posteriores.
- Proporcionar un flujo de bienvenida que cargue historial local y permita abrir una carpeta.
- Cargar la información Git básica fuera del hilo de UI y representar estados de carga, error y cancelación.
- Mantener los límites portables de la shell y probar el bootstrap de forma determinista.
- Hacer funcionales las acciones que se presenten como iconos, con tooltips accesibles y sin convertir los rails en menús textuales.

**Non-Goals:**

- No introducir la lista de commits, virtualización, grafo ni diff viewer.
- No portar todavía la UI WinForms, plugins, localización ni operaciones que cambian el repositorio.
- No definir la colección final de comandos de los rails; solo se exponen acciones respaldadas por este change.
- No crear un sistema general de docking o paneles redimensionables.

## Decisions

### 1. Separar composición, estado y acceso a Git

La ventana XAML será únicamente la composición visual. Un ViewModel de shell coordinará el estado de navegación y un servicio de apertura de repositorio coordinará historial, selección de carpeta, validación y carga de datos. El resultado de carga será un modelo de presentación inmutable con ruta, rama, remotes y resumen del working tree.

Esta separación permite probar el flujo sin construir controles y evita que `MainWindow` se convierta en el propietario de la lógica Git. La creación de `GitModule` y las lecturas de Git se ejecutarán como trabajo asíncrono fuera del hilo de UI; la actualización de propiedades observables volverá al contexto de Avalonia.

**Alternativa considerada:** resolver `GitModule` directamente desde los handlers de botones en code-behind. Se descarta porque mezcla I/O, estados de UI y navegación, y haría más difícil cubrir #20 y los errores de apertura.

### 2. Historial y picker como puertos de la shell

El servicio consultará `RepositoryHistoryManager.Locals.LoadRecentHistoryAsync()` para poblar la bienvenida y usará `OsShellUtil.PickFolder` para la selección de carpetas en producción. La entrada elegida se normalizará y validará con la infraestructura existente antes de añadirla como más reciente mediante `AddAsMostRecentAsync`.

Para las pruebas, la coordinación recibirá puertos internos para historial, selección y carga de repositorio. Los adaptadores de producción delegarán en las APIs actuales. Así, las pruebas no necesitarán invocar un picker nativo ni depender de estado persistido del usuario.

**Alternativa considerada:** escribir una segunda implementación de historial dentro de Avalonia. Se descarta porque duplicaría persistencia y rompería la fuente única del historial existente.

### 3. Shell de cinco regiones con rails de ancho estable

`MainWindow` usará un layout equivalente a `Grid`/`DockPanel`: title bar arriba, status bar abajo, rail izquierdo y rail derecho con anchos mínimos/máximos explícitos, y el workspace central ocupando el espacio restante. Los rails contendrán botones cuadrados o de tamaño fijo, separados del workspace por bordes sutiles.

La barra superior incluirá identidad de GitExtensions y los controles de ventana de la shell. La barra inferior mostrará el contexto resumido, especialmente si hay repositorio abierto. El rail izquierdo alojará navegación del flujo (bienvenida/repositorio) y el derecho acciones contextuales disponibles en este change. Los botones de rail no mostrarán texto; cada uno tendrá tooltip y nombre accesible.

Se mantendrá el tema Fluent oscuro ya adoptado y se usarán recursos de tema, no colores dispersos en los controles. Los iconos se centralizarán como recursos de la shell para que el cambio de icono maximizar/restaurar y los estados seleccionado/hover sean coherentes.

**Alternativa considerada:** usar un `Menu` textual como navegación principal y dejar los rails decorativos. Se descarta porque no cumple la intención IDE-like ni proporciona acciones funcionales en las barras laterales.

**Alternativa considerada:** hacer los rails auto-ocultables o redimensionables desde el primer slice. Se pospone porque añade estados de layout y riesgos de accesibilidad sin aportar valor al flujo 1.2.

### 4. Controles de ventana y portabilidad

La ventana usará `SystemDecorations=None`, por lo que no mostrará la barra ni los controles nativos del sistema. La barra superior Avalonia será el único chrome visible y contendrá los botones propios de minimizar, maximizar/restaurar y cerrar. La interacción de arrastre, maximizar/restaurar y cerrar se encapsulará en la shell y no se expondrá al core.

La implementación verificará el comportamiento de esos botones sobre los backends Windows y Linux disponibles. Las diferencias inevitables de integración con el gestor de ventanas se limitarán al comportamiento del sistema, no a la composición visual ni a la ubicación de los controles.

### 5. Bootstrap headless extraído de `App`

El cableado de startup se extraerá a una unidad interna de composición que pueda recibir adaptadores de prueba. En producción, `App` seguirá siendo el punto de entrada y conectará los adaptadores Avalonia reales antes de crear `MainWindow`. En tests, los adaptadores registrarán invocaciones y resultados sin crear ventanas ni mostrar diálogos.

Los tests aislarán el estado estático de los delegates entre casos, ejecutarán el ciclo de vida Avalonia con backend headless y verificarán la asociación de `JoinableTaskContext` con el contexto de sincronización usado. El proveedor DI será construido por test para evitar depender del estado de otra prueba.

**Alternativa considerada:** probar `App.OnFrameworkInitializationCompleted()` únicamente mediante smoke tests visuales. Se descarta porque no cubre #20 en CI Linux y deja sin aislar los delegates globales.

### 6. Errores como estados de presentación

Una ruta inválida o un fallo de Git no cerrará la shell ni lanzará una excepción no observada desde el comando de UI. El ViewModel expondrá estado y mensaje apropiados, conservará la bienvenida o el repositorio anterior según corresponda y permitirá reintentar. Las excepciones inesperadas continuarán usando `TaskManager.ExceptionReporter` como canal global ya instalado.

## Risks / Trade-offs

- **[API de Git síncrona]** Algunas lecturas de `IGitModule` son síncronas y pueden tardar → ejecutarlas en trabajo de fondo, cancelar cargas obsoletas y publicar el resultado solo si sigue siendo la selección activa.
- **[Delegates estáticos globales]** Las pruebas pueden contaminarse entre sí → encapsular la instalación/restauración en fixtures y mantener los adaptadores de test por instancia.
- **[Backend headless]** La inicialización de Avalonia puede variar entre plataformas → centralizar el setup de test, documentar el orden de inicialización y evitar APIs que requieran display real.
- **[Iconos multiplataforma]** Fuentes o glyphs del sistema pueden no existir en todas las plataformas → usar recursos de iconos controlados por la shell y verificar que cada acción tiene fallback accesible.
- **[Alcance visual amplio]** La shell puede absorber tiempo que corresponde a fases posteriores → limitar las acciones a abrir repositorio, navegación mínima, contexto, ayuda y controles de ventana que estén implementados y dejar comandos Git de escritura fuera.
- **[Gestor de ventanas]** Maximizar, restaurar y minimizar pueden tener restricciones específicas del backend → encapsular las llamadas en la shell y probar el resultado observable, manteniendo siempre el mismo chrome propio.

## Migration Plan

1. Añadir el servicio y ViewModels de apertura de repositorio junto con las vistas de bienvenida e información básica.
2. Sustituir la ventana vacía por la composición de cinco regiones y conectar las acciones respaldadas por el slice.
3. Extraer el bootstrap testeable y añadir el proyecto o conjunto de tests headless a `GitExtensions.slnx`.
4. Ejecutar tests portables y builds de la solución primaria; comprobar también que la solución WinForms de referencia continúa compilando.
5. Si la integración de una acción de ventana resulta problemática en una plataforma, mantener `SystemDecorations=None` y ajustar únicamente el adaptador de esa acción; el rollback no requiere cambios en el core ni en la API de plugins.

## Open Questions

- El conjunto exacto de iconos de los rails puede cerrarse durante la implementación al inventariar las acciones que ya tienen una ruta Avalonia segura; no cambia los contratos de apertura ni de testing.
