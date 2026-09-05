## ADDED Requirements

### Requirement: Servicios del core disponibles para la presentación Avalonia
La shell SHALL poder obtener desde su composición DI los servicios portables necesarios para abrir y describir un repositorio, sin que los ViewModels o servicios de presentación tengan que referenciar `GitUI`, `ResourceManager` ni `GitExtUtils.WinForms`.

#### Scenario: La presentación resuelve sus servicios portables
- **WHEN** se construye la vista de apertura de repositorio mediante la composición de la shell
- **THEN** sus dependencias portables se resuelven desde el proveedor DI
- **AND** no se carga ningún ensamblado WinForms

#### Scenario: La composición conserva el contrato de delegates
- **WHEN** la shell se inicializa para mostrar la vista de apertura
- **THEN** los delegates de plataforma permanecen instalados para los servicios que los necesiten
- **AND** la presentación no los reemplaza con implementaciones globales propias