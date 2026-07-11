# Tasks — Change 0.2: limpiar los tipos WinForms de la API de Extensibility

> Orden por capas (design D6). Al cerrar cada grupo: `eng/Verify.ps1` en verde y la app
> WinForms arranca. Los inventarios (1.1, 4.1, 5.1) se hacen con grep/compilador y se anotan
> aquí mismo como notas bajo la tarea.

## 1. Inventario

- [ ] 1.1 Inventariar en `GitUI`, `src/plugins` y tests todos los consumidores de:
      `IWin32Window` de la API, `IGitPlugin.Icon`/`GitPluginBase.Icon`, `CustomControl`,
      `CreateControlBinding`, `MessageBoxes` y `ShowModelessForm`; anotar recuentos y casos
      no mecánicos como notas en este fichero

## 2. Capa iconos

- [ ] 2.1 `IGitPlugin.Icon: Image?` → `IconData: byte[]?` (y `GitPluginBase`); helper para
      leer el PNG embebido del ensamblado del plugin
- [ ] 2.2 Migrar los iconos de los 12+ plugins integrados a `EmbeddedResource` PNG
- [ ] 2.3 Adaptar los puntos de consumo en `GitUI` (menú/página de plugins) para
      materializar `byte[]` → `Image`; verificar visualmente que los iconos aparecen

## 3. Capa owners

- [ ] 3.1 Crear `IWindow` en `Extensibility`; sustituir `IWin32Window` en `IGitUICommands`
      (~60 métodos), `GitUIEventArgs.OwnerForm`, `GitUIPostActionEventArgs` y cualquier otra
      firma pública
- [ ] 3.2 Implementar `IWindow` en los Forms base de `GitUI` y crear el helper interno de
      traducción `IWindow` → `IWin32Window` en la implementación de `GitUICommands`
- [ ] 3.3 Arreglar los call sites no mecánicos que el compilador señale (los `this` deben
      compilar sin cambios); `eng/Verify.ps1` en verde

## 4. Capa settings

- [ ] 4.1 Confirmar con el inventario qué plugins personalizan `CustomControl` y decidir su
      equivalente en el registro de bindings de GitUI (anotar aquí la decisión por plugin)
- [ ] 4.2 Dejar `ISetting` y settings concretos como datos puros: eliminar
      `CreateControlBinding()` y las propiedades `CustomControl`
- [ ] 4.3 Mover `ISettingControlBinding`, `SettingControlBinding<,>` y `CredentialsControl`
      (con Designer y resx) a `GitUI`; completar el proveedor de bindings de GitUI como único
      mecanismo de renderizado
- [ ] 4.4 Verificar la tubería de traducción del `CredentialsControl` reubicado (el resx debe
      seguir entrando en los `.xlf`) y el escenario de credenciales; `eng/Verify.ps1` en verde

## 5. Capa varios y barrido

- [ ] 5.1 Mover `MessageBoxes` a `GitUI`; migrar los usos desde plugins según inventario
- [ ] 5.2 Retirar `ShowModelessForm(Func<Form>)` de `IGitUICommands` y recolocar el mecanismo
      en GitUI para sus consumidores; barrido final de `using System.Windows.Forms|Drawing`
      en `Extensibility` (debe quedar a cero)

## 6. Guardarraíl y cierre

- [ ] 6.1 Fijar `<UseWindowsForms>false</UseWindowsForms>` en
      `GitExtensions.Extensibility.csproj` y compilar la solución completa
- [ ] 6.2 `eng/Verify.ps1` completo en verde + smoke test manual del escenario de la spec:
      arrancar app → abrir repo → ejecutar un plugin → settings del plugin → editar y guardar
- [ ] 6.3 Actualizar la hoja de ruta (0.2 completado) y registrar en el registro de
      decisiones interno las decisiones tomadas durante la implementación
