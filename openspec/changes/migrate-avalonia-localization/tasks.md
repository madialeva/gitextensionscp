## 1. Catálogo y resolución portable

- [x] 1.1 Inventariar los textos visibles de la shell actual y definir claves estables con fixtures mínimos; verificar que cada texto localizable de bienvenida, apertura, estados, errores y comandos tiene una clave documentada.
- [x] 1.2 Implementar el modelo de catálogo y el lector del subconjunto XLIFF necesario para claves, cultura, texto fuente y traducción; verificar carga correcta de catálogos válidos y tratamiento no bloqueante de unidades incompletas.
- [x] 1.3 Implementar la resolución por cultura activa, fallback a inglés y marcador determinista para claves ausentes; verificar culturas disponibles, catálogo incompleto y clave desconocida con tests unitarios.

## 2. Integración con la shell Avalonia

- [x] 2.1 Registrar el servicio de localización en la composición DI de Avalonia y exponer cultura activa observable; verificar resolución desde un contexto headless sin referencias a WinForms.
- [x] 2.2 Conectar los textos visibles de la bienvenida, apertura de repositorio, estados, errores y comandos a claves localizadas; verificar que la shell inicia con la cultura configurada y conserva su estado al cambiarla.
- [x] 2.3 Añadir el cambio de cultura configurable para la shell sin introducir todavía una pantalla completa de preferencias; verificar que las vistas actualizan sus textos sin recrear la ventana principal.

## 3. Verificación y compatibilidad

- [x] 3.1 Añadir tests headless para carga, fallback, cambio de cultura, claves ausentes y resolución desde vistas/ViewModels; verificar ejecución en `net10.0` con todos los tests verdes.
- [x] 3.2 Verificar que `GitExtensions.Avalonia` sigue compilando como `net10.0` sin referencias a `ResourceManager`, `GitUI` o `GitExtUtils.WinForms`, y que la aplicación WinForms conserva su infraestructura de localización.
- [x] 3.3 Ejecutar build, tests y validación OpenSpec; verificar que la shell localizada funciona con el catálogo inglés y que el change cumple todas las specs antes de solicitar revisión.
