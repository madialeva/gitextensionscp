## Purpose

Proporciona una localización portable y observable para la shell Avalonia, reutilizando los
catálogos de traducción existentes sin acoplarla a WinForms ni a `ResourceManager`.

## ADDED Requirements

### Requirement: Resolución de recursos localizados

La shell Avalonia SHALL resolver textos mediante claves estables y una cultura activa,
consultando los catálogos XLIFF disponibles para la aplicación.

#### Scenario: Resolver una traducción existente

- **WHEN** una vista solicita una clave con una traducción disponible para la cultura activa
- **THEN** recibe el texto traducido correspondiente

#### Scenario: Falta una clave en la cultura activa

- **WHEN** una vista solicita una clave que no existe en el catálogo de la cultura activa
  pero sí existe en el catálogo inglés
- **THEN** recibe el texto inglés asociado a la clave

#### Scenario: Falta una clave en todos los catálogos

- **WHEN** una vista solicita una clave ausente en la cultura activa y en inglés
- **THEN** recibe un valor determinista que identifica la clave ausente
- **AND** la resolución no lanza una excepción ni bloquea la shell

### Requirement: Cultura activa observable

La shell Avalonia SHALL exponer una cultura activa y SHALL actualizar los textos enlazados
cuando la cultura cambie, sin reiniciar la aplicación ni recrear la ventana principal.

#### Scenario: Cambiar la cultura activa

- **WHEN** la cultura activa cambia a una cultura con un catálogo disponible
- **THEN** los textos localizados enlazados se actualizan
- **AND** el estado de la vista y el repositorio abierto se conserva

#### Scenario: Cultura no disponible

- **WHEN** se selecciona una cultura sin catálogo disponible
- **THEN** la shell mantiene la cultura activa anterior o aplica el fallback definido
- **AND** los textos siguen siendo resolubles en inglés

### Requirement: Localización de la shell existente

Las vistas y ViewModels visibles de la shell Avalonia SHALL obtener de la capacidad de
localización los textos de bienvenida, apertura de repositorio, errores, estados y comandos
presentados al usuario, sin depender de literales fijos para esos textos.

#### Scenario: Iniciar la shell localizada

- **WHEN** se inicia la aplicación con una cultura configurada
- **THEN** la bienvenida y los comandos visibles se muestran en la cultura activa o en inglés
  mediante fallback

#### Scenario: Mostrar un error localizado

- **WHEN** la apertura de un repositorio produce un error presentable al usuario
- **THEN** el título y el mensaje del estado de error se resuelven mediante claves localizadas

### Requirement: Independencia de la localización WinForms

La localización Avalonia SHALL funcionar sin referencias a `ResourceManager`, `GitUI` o
`GitExtUtils.WinForms`, y SHALL preservar sin cambios el comportamiento de localización de
la aplicación WinForms.

#### Scenario: Compilar la shell sin WinForms

- **WHEN** se compila `GitExtensions.Avalonia` para `net10.0`
- **THEN** la shell no requiere referencias de proyecto a ensamblados WinForms

#### Scenario: Ejecutar la aplicación WinForms

- **WHEN** se ejecuta la aplicación WinForms existente después de instalar la localización
  Avalonia
- **THEN** conserva su infraestructura y comportamiento de traducción actuales
