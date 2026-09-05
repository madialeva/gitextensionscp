## Context

La shell Avalonia actual está desacoplada de WinForms, pero sus textos visibles todavía viven
principalmente en XAML y ViewModels. Los catálogos existentes están en XLIFF y la aplicación
WinForms los consume mediante `ResourceManager` y `TranslatedStrings`, una infraestructura que
no puede convertirse en dependencia de la shell portable.

Este change debe servir a la shell Avalonia actual y a sus próximas vistas, empezando por la
lista de commits, sin cambiar el contrato ni el comportamiento de la aplicación WinForms.

## Goals / Non-Goals

**Goals:**

- Definir una única abstracción portable para resolver claves localizadas.
- Leer el subconjunto necesario de los catálogos XLIFF existentes para la shell Avalonia.
- Exponer cultura activa y notificar cambios a las vistas y ViewModels enlazados.
- Aplicar fallback estable a inglés y una representación diagnóstica para claves ausentes.
- Cubrir resolución, fallback, cambio de cultura y ausencia de WinForms con tests headless.

**Non-Goals:**

- No reemplazar `ResourceManager`, `TranslatedStrings` ni la localización WinForms.
- No migrar todos los consumidores de XLIFF, plugins ni herramientas de traducción.
- No rediseñar en este change la edición, importación o exportación de traducciones.
- No introducir localización de contenido generado por Git que todavía no tenga un contrato de
  claves en la shell.

## Decisions

### 1. Catálogos XLIFF como fuente de datos, adaptados por una capa portable

La shell consumirá un lector/adaptador portable que transforme las unidades XLIFF relevantes
(key, texto fuente y texto traducido) en un catálogo indexado por clave y cultura. El lector
aceptará únicamente datos y APIs multiplataforma; no referenciará `ResourceManager`, `GitUI`,
WinForms ni controles Avalonia.

**Alternativa descartada:** reutilizar directamente `ResourceManager` o `TranslatedStrings`.
Su modelo actual recorre árboles de controles WinForms y mantendría el acoplamiento que este
change pretende eliminar.

**Alternativa descartada:** copiar los textos a un nuevo fichero JSON o RESX. Duplicaría los
catálogos existentes y abriría una segunda fuente de verdad antes de haber definido la tubería
completa de traducción.

### 2. Servicio observable para cultura y recursos

La shell tendrá un servicio de localización con una cultura activa y una notificación de cambio.
Las vistas y ViewModels resolverán claves a través de ese servicio o de una propiedad enlazable,
de forma que el cambio de cultura actualice los textos sin recrear la ventana ni perder el
estado de navegación.

La cultura inicial se determinará desde la configuración disponible de la shell y, si no existe,
se usará inglés. La selección de cultura será una responsabilidad de la shell, no del core Git.

**Alternativa descartada:** usar directamente `Thread.CurrentUICulture` como único almacén de
estado. No proporciona por sí sola notificación suficiente para refrescar bindings existentes y
mezcla la cultura del proceso con la cultura observable de la ventana.

### 3. Fallback explícito y no bloqueante

La resolución seguirá este orden: cultura activa, inglés y marcador de clave ausente. Un catálogo
incompleto no impedirá iniciar la aplicación ni abrir un repositorio. Las claves ausentes se
registrarán de forma diagnóstica sin mostrar excepciones como flujo normal de usuario.

**Alternativa descartada:** devolver una cadena vacía cuando falte una traducción. Ocultaría
información de la interfaz y haría difícil detectar catálogos incompletos.

### 4. Integración gradual de la shell

Se sustituirán los textos localizables de la shell existente por claves y bindings, manteniendo
los textos técnicos que no son contenido de usuario fuera del catálogo. La misma abstracción se
usará en la lista de commits posterior, evitando crear un mecanismo paralelo para cada vista.

### 5. Tests headless y compatibilidad

Los tests se ejecutarán en `net10.0` con el contexto headless ya establecido. Probarán catálogos
mínimos en memoria o desde fixtures, para aislar el comportamiento de resolución de la lectura
física de todos los idiomas. También verificarán que el proyecto Avalonia no adquiere referencias
WinForms.

## Risks / Trade-offs

- **Formato XLIFF más complejo de lo necesario:** el adaptador puede encontrar variantes de
  versión o unidades sin traducción. Mitigación: soportar el subconjunto usado por los catálogos,
  tratar errores de unidad como ausencia y cubrir fixtures representativos.
- **Claves que no coinciden con los identificadores de la shell:** una migración mecánica puede
  producir textos sin traducción. Mitigación: catálogo explícito de claves de la shell, fallback
  inglés y test de todas las claves usadas por la vista.
- **Actualización dinámica incompleta:** bindings directos pueden no reaccionar al cambio de
  cultura. Mitigación: una propiedad observable o mecanismo equivalente como único punto de
  acceso en vistas y ViewModels, probado headless.
- **Divergencia futura con WinForms:** ambos consumidores pueden necesitar convenciones distintas.
  Mitigación: compartir los datos XLIFF y mantener adaptadores separados, sin forzar una API de
  presentación común.

## Migration Plan

1. Inventariar los textos visibles de la shell y establecer las claves y fixtures mínimas.
2. Implementar el catálogo portable, el lector XLIFF y la resolución con fallback.
3. Registrar el servicio en la composición Avalonia y conectar cultura y bindings.
4. Migrar la bienvenida, apertura de repositorio, estados, errores y comandos visibles.
5. Añadir y ejecutar tests headless, build portable y validación de ausencia de referencias
   WinForms.
6. Mantener los cambios confinados al nuevo servicio y a la shell; si la migración falla, se
   puede revertir la integración de bindings sin modificar la infraestructura WinForms.

## Open Questions

- La selección visual de idioma y su integración con settings puede definirse en un change
  posterior; este change solo necesita una cultura activa configurable para probar el contrato.
