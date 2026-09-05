## 1. Infraestructura de composición y pruebas

- [x] 1.1 Extraer el cableado de startup de `App` a una composición interna testeable, conservando los adaptadores Avalonia reales para producción; verificar con una prueba de compilación que `GitExtensions.Avalonia` sigue inicializando el proveedor DI y los tres delegates.
- [x] 1.2 Añadir el setup headless de Avalonia y aislar por fixture el estado global de `TaskManager.ExceptionReporter`, `UserMessageHandler.ShowError` y `OsShellUtil.PickFolder`; verificar que dos pruebas consecutivas no comparten invocaciones ni configuración.
- [x] 1.3 Añadir las pruebas de #20 para resolución de servicios, asociación de `JoinableTaskContext`/`AvaloniaSynchronizationContext` y delegates con dobles, sin mostrar ventanas; verificar ejecución en `net10.0` sin WinForms.
- [x] 1.4 Incluir el proyecto o conjunto de tests headless en `GitExtensions.slnx` y documentar el orden de ciclo de vida necesario; verificar la ejecución desde la solución primaria en Linux.

## 2. Servicio y estado de apertura de repositorios

- [x] 2.1 Crear el modelo de presentación de repositorio con ruta, rama actual, remotes y resumen del working tree, sin exponer controles Avalonia; verificar sus estados válidos mediante tests unitarios.
- [x] 2.2 Crear el servicio de apertura que consulte el historial local existente, solicite una carpeta mediante el puerto de shell, valide la ruta con el core Git y registre como más reciente un repositorio válido; verificar casos de ruta válida, ruta inválida y cancelación con dobles.
- [x] 2.3 Implementar la carga asíncrona de rama, remotes y estado, con cancelación de cargas obsoletas y propagación controlada de errores; verificar que una carga fallida no deja el ViewModel en estado de éxito parcial.
- [x] 2.4 Añadir el ViewModel de bienvenida/repository con comandos para abrir una entrada reciente, explorar una carpeta, reintentar y volver a seleccionar; verificar transiciones de carga, éxito, error y cancelación.
- [x] 2.5 Conectar el ViewModel a la composición DI de Avalonia sin referencias a `GitUI`, `ResourceManager` ni `GitExtUtils.WinForms`; verificar el grafo de referencias y la compilación `net10.0`.

## 3. Composición visual de la shell principal

- [x] 3.1 Reemplazar `MainWindow` vacío por un layout estable de cinco regiones: barra superior, rail izquierdo, workspace central, rail derecho y barra inferior; verificar dimensiones mínimas, ausencia de solapamientos y adaptación al redimensionado.
- [x] 3.2 Configurar `SystemDecorations=None` y crear la barra superior propia con identidad de GitExtensions y controles iconográficos funcionales de minimizar, maximizar/restaurar y cerrar; verificar que no aparece chrome nativo, que cada acción funciona y que el icono de maximizar/restaurar refleja el estado.
- [x] 3.3 Crear los rails laterales con botones icon-only, estados hover/seleccionado y tooltips accesibles; verificar que no muestran etiquetas permanentes y que cada acción conectada ejecuta su comando.
- [x] 3.4 Crear la barra inferior de estado para indicar ausencia de repositorio o contexto del repositorio activo; verificar que permanece visible y no invade el workspace.
- [x] 3.5 Centralizar recursos de tema e iconos y mantener la variante Fluent actual; verificar la apariencia básica en tema oscuro y que no dependa de fuentes o APIs WinForms del sistema.
- [x] 3.6 Conectar la navegación mínima de los rails con bienvenida, repositorio abierto y acciones respaldadas por este change; verificar que las acciones futuras no aparecen como botones inertes.

## 4. Integración del flujo de apertura

- [x] 4.1 Mostrar el historial reciente en la vista de bienvenida con ruta/nombre distinguibles y acción icon-only para explorar; verificar que una selección inicia una única carga.
- [x] 4.2 Mostrar la vista de información básica al abrir correctamente un repositorio y reflejar ruta, rama, remotes y estado en el workspace y status bar; verificar un repositorio con y sin remote.
- [x] 4.3 Mostrar estados de loading, carpeta inválida y error con una ruta para reintentar o seleccionar otra carpeta; verificar que cancelar no genera un error ni borra el estado válido anterior.
- [ ] 4.4 Ejecutar una comprobación visual de la shell en Windows y Linux, usando la misma composición y recursos; verificar la paridad visual del chrome propio y que las acciones de ventana siguen siendo funcionales en ambos backends.

## 5. Verificación y compatibilidad

- [x] 5.1 Ejecutar los tests unitarios y headless del slice en `net10.0`; verificar que cubren startup, DI, threading, delegates, historial, validación y estados del ViewModel.
- [x] 5.2 Ejecutar `dotnet build GitExtensions.slnx`; verificar que la solución primaria y la shell Avalonia compilan sin referencias WinForms nuevas.
- [x] 5.3 Ejecutar la verificación Linux existente; verificar que el nuevo conjunto headless no requiere display nativo ni WinForms.
- [ ] 5.4 Compilar la solución WinForms de referencia y ejecutar su verificación aplicable; verificar que la instalación estática de delegates de WinForms no se rompe.
- [x] 5.5 Ejecutar `openspec validate --changes "open-repository-main-shell" --strict`; verificar que todos los artefactos y escenarios cumplen el schema antes de solicitar revisión.
