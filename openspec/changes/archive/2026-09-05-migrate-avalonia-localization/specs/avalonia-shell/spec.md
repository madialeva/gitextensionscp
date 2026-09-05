## ADDED Requirements

### Requirement: Textos de la shell mediante localización

Los textos visibles añadidos o modificados en la shell Avalonia SHALL resolverse mediante la
capacidad `avalonia-localization`, incluyendo textos de bienvenida, apertura de repositorio,
estados, errores y comandos.

#### Scenario: No hay literales de presentación para textos localizables

- **WHEN** se inspeccionan las vistas y ViewModels de la shell
- **THEN** los textos visibles localizables se obtienen mediante claves o bindings de recursos
- **AND** los literales restantes se limitan a valores técnicos que no se presentan al usuario

#### Scenario: La shell conserva su estado al cambiar idioma

- **WHEN** se cambia la cultura mientras la shell muestra bienvenida o un repositorio abierto
- **THEN** se actualizan los textos visibles
- **AND** no se pierde la navegación ni el repositorio seleccionado
