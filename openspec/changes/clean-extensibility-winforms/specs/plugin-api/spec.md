# plugin-api — Contrato de extensibilidad neutro respecto a la UI

## ADDED Requirements

### Requirement: API sin tipos de tecnología de UI
El ensamblado `GitExtensions.Extensibility` SHALL NOT exponer tipos de `System.Windows.Forms`
ni de `System.Drawing` en ninguna firma pública (parámetros, retornos, propiedades, eventos,
clases base), y SHALL compilar con `UseWindowsForms=false` como verificación permanente.

#### Scenario: Guardarraíl de compilación
- **WHEN** se compila la solución con `GitExtensions.Extensibility` configurado con
  `UseWindowsForms=false`
- **THEN** la compilación termina sin errores

#### Scenario: Regresión de acoplamiento
- **WHEN** un cambio futuro introduce un tipo WinForms/GDI+ en una firma pública de la API
- **THEN** el proyecto no compila y el error señala el tipo ofensor

### Requirement: Owner de diálogos abstracto
La API SHALL expresar el dueño de un diálogo mediante la interfaz neutra `IWindow` (definida
en `Extensibility`), y la shell WinForms SHALL traducirla a `IWin32Window` internamente para
preservar modalidad y centrado de los diálogos.

#### Scenario: Plugin abre un diálogo con owner
- **WHEN** un plugin invoca `StartCommitDialog(owner, ...)` pasando el `IWindow` recibido de
  la shell
- **THEN** el diálogo de commit se muestra modal y centrado sobre esa ventana, como antes de
  la migración

#### Scenario: Invocación sin owner
- **WHEN** se invoca un método `Start*Dialog` con `owner: null`
- **THEN** el diálogo se muestra igual que antes de la migración (sin ventana dueña)

### Requirement: Iconos de plugin como datos
`IGitPlugin` SHALL exponer su icono como datos de imagen neutros (bytes de un PNG embebido),
y cada shell SHALL materializarlos a su tipo de imagen nativo en el punto de consumo.

#### Scenario: Iconos visibles en la UI de plugins
- **WHEN** se abre la lista/menú de plugins en la app WinForms tras la migración
- **THEN** cada plugin integrado muestra el mismo icono que mostraba antes

### Requirement: Settings declarativos
Los tipos de setting de la API (`ISetting` y derivados) SHALL ser datos puros (nombre,
caption, valor, default) sin referencias a controles de UI; la vinculación setting→control
SHALL residir en la capa de UI (en `GitUI` para la shell WinForms).

#### Scenario: Página de settings de un plugin
- **WHEN** se abre la página de settings de un plugin integrado, se modifica un valor y se
  guarda
- **THEN** el valor editado se muestra, persiste y se relee igual que antes de la migración

#### Scenario: Setting de credenciales
- **WHEN** un plugin usa `CredentialsSetting` para pedir usuario/contraseña
- **THEN** el control de credenciales (ahora residente en GitUI) se renderiza y guarda en el
  almacén de credenciales igual que antes

### Requirement: Paridad funcional de la shell WinForms
La limpieza SHALL NOT cambiar el comportamiento visible de la aplicación WinForms: los
mismos diálogos, iconos y páginas de settings funcionan igual tras el change.

#### Scenario: Verificación completa
- **WHEN** se ejecuta `eng/Verify.ps1` tras completar el change
- **THEN** la build está limpia y los 15 proyectos de unit tests pasan

#### Scenario: Smoke test de plugins
- **WHEN** se arranca la app, se abre un repositorio y se recorre: menú Plugins → ejecutar un
  plugin → abrir su página de settings → editar y guardar
- **THEN** todo el recorrido funciona sin errores ni pérdida de funcionalidad
