# avalonia-headless-testing Specification

## Purpose
Proporciona una red de seguridad determinista para el arranque de la shell Avalonia y sus integraciones con el core, ejecutable en la solución multiplataforma sin abrir ventanas reales.

## Requirements
### Requirement: Cobertura headless del arranque
El proyecto SHALL incluir pruebas headless que verifiquen que la shell puede inicializarse con su ciclo de vida de Avalonia y resolver los servicios registrados sin requerir una ventana visible ni WinForms.

#### Scenario: Inicialización headless
- **WHEN** una prueba inicia el ciclo de vida Avalonia en modo headless
- **THEN** la aplicación completa su inicialización sin mostrar una ventana real
- **AND** el proveedor de servicios de la shell puede resolver los servicios portables esperados

### Requirement: Verificación del threading de Avalonia
Las pruebas headless SHALL verificar que el contexto de tareas de la shell se inicializa sobre el contexto de sincronización de Avalonia y que una operación que requiere el hilo de UI puede ejecutarse en el contexto de prueba.

#### Scenario: Contexto de threading inicializado
- **WHEN** termina la inicialización headless de la aplicación
- **THEN** el contexto de tareas está disponible
- **AND** está asociado al contexto de sincronización de Avalonia usado por la shell

### Requirement: Verificación de delegates sin UI real
Las pruebas SHALL cubrir la instalación y la invocación de los delegates de excepción, mensajes y selección de carpetas usando dobles de plataforma, sin mostrar diálogos ni depender de un selector de carpetas nativo.

#### Scenario: Delegates invocables sin UI
- **WHEN** la prueba invoca los delegates instalados con datos controlados
- **THEN** los handlers de prueba reciben los datos esperados
- **AND** no se abre un diálogo real

### Requirement: Ejecución portable y determinista
Las pruebas headless SHALL ejecutarse en la solución primaria `GitExtensions.slnx`, SHALL usar únicamente dependencias compatibles con `net10.0` y SHALL dejar documentados los requisitos de ciclo de vida que eviten orden o estado global no determinista.

#### Scenario: Ejecución en Linux
- **WHEN** se ejecutan las pruebas headless en Linux
- **THEN** terminan sin referencias a WinForms ni display nativo
- **AND** producen el mismo resultado determinista que en Windows
