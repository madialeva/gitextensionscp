## 1. Extraer la lógica portable

- [x] 1.1 Confirmar los consumidores actuales de `LocalizationHelpers` y congelar los casos
  límite cubiertos por los dos tests retirados.
- [x] 1.2 Crear en `GitCommands` la representación portable de unidad/valor relativo y mover
  el algoritmo de redondeo, umbrales, signo y `displayWeeks`.
- [x] 1.3 Mover el formato de fecha completa al núcleo portable sin introducir dependencias de
  `ResourceManager` o WinForms.
- [x] 1.4 Mantener en `ResourceManager` un adaptador que traduzca la representación portable
  usando `TranslatedStrings` y actualizar `GitUI`/`ResourceManager` sin cambiar el resultado
  visible.
- [x] 1.5 Verificar que los proyectos portables conservan `UseWindowsForms=false` y que
  `GitCommands` no referencia `ResourceManager`.

## 2. Recuperar cobertura cross-platform

- [x] 2.1 Restaurar los dos tests en `GitCommands.Tests` usando un formateador inglés local,
  sin `AppSettings.CurrentTranslation` ni referencia a `ResourceManager`.
- [x] 2.2 Ejecutar los tests afectados bajo `net10.0` y comprobar fechas pasadas/futuras,
  unidades, límites y `displayWeeks`.
- [x] 2.3 Ejecutar `dotnet build GitExtensions.slnx` y la suite portable `GitCommands.Tests` completa en Linux; los tests Windows-only quedan marcados con `[Platform(Include = "Win")]`.

## 3. Migrar la verificación Linux a Bash

- [x] 3.1 Crear `eng/Verify-Linux.sh` con descubrimiento robusto del root, configuración
  `Release`/`Debug`, build, tests, TRX, resumen y códigos de salida equivalentes.
- [x] 3.2 Eliminar `eng/Verify-Linux.ps1` y marcar el script Bash como ejecutable.
- [x] 3.3 Cambiar `fork-ci.yml` para invocar `bash eng/Verify-Linux.sh` manteniendo checkout,
  setup del SDK y publicación de artifacts.
- [x] 3.4 Actualizar la documentación y ejemplos de uso local del verificador Linux.

## 4. Verificación final

- [x] 4.1 Ejecutar `bash -n eng/Verify-Linux.sh`.
- [x] 4.2 Ejecutar la variante `Debug` y comprobar que genera resultados en el directorio
  esperado sin fallos en los tests portables.
- [x] 4.3 Verificar que un fallo de build, test o descubrimiento vacío produce código distinto de cero y conserva
  los logs TRX.
- [ ] 4.4 Ejecutar la verificación Windows y confirmar que la solución WinForms de referencia
  y su script no han sido alterados.
- [x] 4.5 Ejecutar `openspec validate` sin errores.
