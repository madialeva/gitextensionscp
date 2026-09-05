## Purpose

Permite que la shell Avalonia abra repositorios Git reales y presente su contexto básico antes de añadir navegación de commits u operaciones de escritura.

## ADDED Requirements

### Requirement: Selección de repositorio
La shell SHALL permitir al usuario abrir un repositorio seleccionando una entrada del historial reciente o eligiendo una carpeta mediante el selector de carpetas de la plataforma.

#### Scenario: Abrir un repositorio reciente
- **WHEN** el usuario selecciona una entrada válida del historial
- **THEN** la shell abre ese repositorio
- **AND** muestra su información básica

#### Scenario: Explorar una carpeta
- **WHEN** el usuario activa la acción de abrir repositorio y elige una carpeta
- **THEN** la shell intenta abrir la carpeta seleccionada como repositorio

#### Scenario: Cancelar la selección
- **WHEN** el usuario cierra o cancela el selector de carpetas
- **THEN** la shell conserva la vista actual
- **AND** no muestra un error de apertura

### Requirement: Validación y carga del repositorio
La shell SHALL validar que la ruta seleccionada corresponde a un repositorio Git y SHALL representar de forma explícita los estados de carga, carpeta inválida y error de lectura.

#### Scenario: Cargar un repositorio válido
- **WHEN** la ruta seleccionada contiene un repositorio Git válido
- **THEN** la shell muestra un estado de carga mientras obtiene la información
- **AND** cambia a la vista del repositorio cuando la carga termina

#### Scenario: Seleccionar una carpeta que no es repositorio
- **WHEN** la ruta seleccionada no es un repositorio Git válido
- **THEN** la shell permanece operativa
- **AND** muestra un mensaje que identifica la ruta como no válida
- **AND** permite seleccionar otra carpeta

#### Scenario: Fallo al leer el repositorio
- **WHEN** la lectura del repositorio falla por un error de Git o del sistema de archivos
- **THEN** la shell muestra un estado de error comprensible
- **AND** permite volver a intentar o seleccionar otro repositorio

### Requirement: Información básica del repositorio
La vista del repositorio SHALL mostrar la ruta abierta, la rama actual, los remotes configurados y un resumen del estado del working tree.

#### Scenario: Repositorio con rama y remotes
- **WHEN** se abre un repositorio con rama actual y remotes configurados
- **THEN** la vista muestra la ruta, la rama actual y cada remote identificable
- **AND** muestra el estado del working tree

#### Scenario: Repositorio sin remote
- **WHEN** se abre un repositorio sin remotes configurados
- **THEN** la vista muestra la rama actual y el estado del working tree
- **AND** indica que no hay remotes configurados
