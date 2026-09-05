## Purpose

Establece una ventana principal Avalonia con composición de aplicación tipo IDE, preparada para crecer por slices verticales sin acoplarla a la UI WinForms.

## ADDED Requirements

### Requirement: Composición estable de la ventana principal
La ventana principal SHALL presentar regiones persistentes para la barra superior, rail izquierdo, área central, rail derecho y barra inferior. Las regiones laterales SHALL conservar dimensiones estables y no desplazar ni redimensionar inesperadamente el área central al cambiar su contenido.

#### Scenario: La shell se muestra completa
- **WHEN** la aplicación inicia la ventana principal
- **THEN** son visibles la barra superior, ambos rails, el área central y la barra inferior
- **AND** el área central ocupa el espacio restante

#### Scenario: La ventana cambia de tamaño
- **WHEN** el usuario redimensiona la ventana
- **THEN** las regiones mantienen sus restricciones mínimas
- **AND** el contenido central se adapta al espacio disponible sin solaparse con las barras

### Requirement: Barra superior personalizada
La ventana SHALL desactivar sus decoraciones nativas y la barra superior propia SHALL mostrar la identidad de GitExtensions y controles funcionales de minimizar, maximizar/restaurar y cerrar. Estos controles SHALL ser el único chrome de ventana visible y SHALL comportarse de forma equivalente en Windows y Linux.

#### Scenario: Controles de ventana visibles
- **WHEN** la ventana principal está visible
- **THEN** el usuario puede identificar los controles de minimizar, maximizar/restaurar y cerrar mediante iconos
- **AND** cada control tiene un tooltip accesible
- **AND** no se muestra una barra de título ni controles de ventana nativos

#### Scenario: Maximizar y restaurar
- **WHEN** el usuario activa maximizar/restaurar
- **THEN** la ventana alterna entre estado maximizado y su estado anterior
- **AND** el icono refleja el estado resultante

### Requirement: Rails laterales de acciones icon-only
Los rails izquierdo y derecho SHALL contener acciones representadas por iconos sin texto visible dentro de los botones. Cada acción disponible SHALL tener un tooltip accesible y SHALL ejecutar únicamente un comportamiento que esté implementado en la shell.

#### Scenario: Acción del rail izquierdo
- **WHEN** el usuario activa una acción disponible del rail izquierdo
- **THEN** se ejecuta su comando asociado
- **AND** el estado visual de la acción activa es distinguible

#### Scenario: Acción del rail derecho
- **WHEN** el usuario activa una acción disponible del rail derecho
- **THEN** se ejecuta su comando asociado sin abandonar el área central
- **AND** el botón conserva una presentación icon-only

#### Scenario: Tooltip de una acción icon-only
- **WHEN** el usuario mantiene el puntero sobre una acción icon-only
- **THEN** aparece un tooltip que identifica su acción
- **AND** el tooltip no se sustituye por una etiqueta permanente en el rail

### Requirement: Barra inferior de estado
La barra inferior SHALL permanecer visible y SHALL ofrecer un área compacta para el contexto del repositorio y el estado de la shell, sin ocultar el contenido central.

#### Scenario: Estado inicial sin repositorio
- **WHEN** la shell se muestra sin repositorio abierto
- **THEN** la barra inferior indica que no hay repositorio abierto

#### Scenario: Estado con repositorio
- **WHEN** existe un repositorio abierto
- **THEN** la barra inferior muestra un resumen de contexto coherente con el repositorio activo
