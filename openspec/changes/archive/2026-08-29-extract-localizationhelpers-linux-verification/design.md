## Context

La solución primaria ya compila como cross-platform y `GitCommands.Tests` es un proyecto
`net10.0` sin referencia a `ResourceManager`. En el change anterior se quitaron de esa suite
dos tests porque `src/app/ResourceManager/LocalizationHelpers.cs` mezcla dos responsabilidades:
calcular intervalos temporales y convertirlos en frases mediante `ResourceManager.TranslatedStrings`.

La pata Linux de CI ejecuta actualmente `eng/Verify-Linux.ps1` a través de `pwsh`. Ese requisito
no aporta nada específico de PowerShell y dificulta la ejecución directa en el entorno Linux de
desarrollo.

## Goals / Non-Goals

**Goals:**

- Dejar el cálculo de fechas relativas en un proyecto portable (`GitCommands`).
- Mantener el texto localizado existente en la capa que posee los recursos de traducción.
- Recuperar los dos tests con cobertura significativa de límites, signo y unidades bajo
  `net10.0`.
- Ofrecer una verificación Linux ejecutable con Bash tanto localmente como en GitHub Actions.
- Mantener el contrato observable de build, tests, TRX y códigos de salida.

**Non-Goals:**

- No migrar la infraestructura completa de localización ni los ficheros XLIFF.
- No mover `TranslatedStrings` ni introducir `ResourceManager` en `GitCommands`.
- No cambiar el comportamiento visual de fechas en WinForms.
- No sustituir `eng/Verify.ps1`, que sigue siendo la verificación de Windows.
- No añadir una matriz de shells ni soporte para PowerShell en el script Linux nuevo.

## Decisions

### 1. Separar cálculo portable y presentación localizada

`GitCommands` será dueño de `LocalizationHelpers` portable. La API de cálculo devolverá una
unidad y un valor relativos (`seconds`, `minutes`, `hours`, `days`, `weeks`, `months` o `years`),
y conservará las reglas actuales de redondeo, umbrales, signo y `displayWeeks`. El formateo de
fecha completa seguirá siendo portable.

El adaptador que conserva la API de presentación actual permanecerá en `ResourceManager`: toma
el resultado portable y lo traduce mediante los métodos existentes de `TranslatedStrings`.
Los consumidores WinForms seguirán llamando a ese adaptador, de modo que la extracción no
convierte el core en consumidor de recursos de UI.

**Alternativa descartada:** copiar `TranslatedStrings` o los recursos `.resx` dentro de
`GitCommands`. Eso haría portable el ensamblado solo de forma aparente, duplicaría la fuente de
traducciones y trasladaría al core una responsabilidad de presentación.

### 2. Tests portables con un formateador inglés explícito

Los dos tests retirados se restaurarán en `GitCommands.Tests`, pero invocarán la API portable con
un formateador inglés definido por el test. Así conservan las comprobaciones de frases esperadas
para las unidades y cantidades relevantes sin modificar `AppSettings.CurrentTranslation` ni
referenciar `ResourceManager`. Las pruebas cubrirán fechas pasadas y futuras, incluyendo los
límites ya establecidos por el comportamiento actual.

### 3. Script POSIX como fuente de verificación Linux

Se añadirá `eng/Verify-Linux.sh` con estas propiedades:

- `#!/usr/bin/env bash` y `set -uo pipefail`.
- Resolución del root a partir de la ubicación del propio script, por lo que funciona desde
  cualquier directorio.
- Un argumento posicional opcional `Release` o `Debug`, con `Release` como valor predeterminado
  y error claro ante valores desconocidos.
- `dotnet build GitExtensions.slnx` antes de los tests.
- `dotnet test` para `GitCommands.Tests`, con el mismo logger TRX y directorio de resultados
  `artifacts/<Configuration>/TestResults` que el script actual.
- Resumen final y exit code distinto de cero si falla el build o la suite.

El workflow Linux ejecutará `bash eng/Verify-Linux.sh`. La selección de solución y tests queda en
el script local, y CI seguirá siendo un envoltorio de checkout, SDK y ejecución.

**Alternativa descartada:** conservar PowerShell como requisito del runner Linux o mantener dos
scripts con lógica duplicada. El nuevo script elimina la dependencia de `pwsh`; el script
Windows permanece separado porque su entorno y sus suites son distintos.

### 4. Formato de los ficheros

El script shell usará finales de línea LF: Bash interpreta un `CR` al final de las directivas y
argumentos como parte del texto, lo que puede impedir su ejecución. Los ficheros C# y de
configuración seguirán las convenciones de finales de línea existentes del repositorio.

## Risks / Trade-offs

- **Riesgo: diferencias sutiles al separar el cálculo.** Mitigación: conservar los umbrales y
  redondeo actuales y recuperar las dos baterías de casos, además de comparar el texto producido
  por el adaptador WinForms.
- **Riesgo: pérdida accidental de traducción.** Mitigación: `ResourceManager` sigue siendo el
  único propietario de `TranslatedStrings`; solo cambia la fuente del valor y la unidad.
- **Riesgo: incompatibilidad de invocación local.** Mitigación: documentar `./eng/Verify-Linux.sh`
  y `./eng/Verify-Linux.sh Debug`, usar `bash -n` y ejecutar la verificación en Linux antes de
  actualizar CI.
- **Trade-off: dos extensiones de script.** Es intencionado: PowerShell continúa atendiendo la
  verificación Windows y Bash hace explícita la herramienta nativa de Linux.

## Migration Plan

1. Inventariar las llamadas actuales y fijar los casos de prueba antes de mover código.
2. Crear la API portable y el adaptador de `ResourceManager`; actualizar referencias y restaurar
   los tests.
3. Sustituir el script Linux por Bash y ajustar el workflow y la documentación de uso.
4. Ejecutar validaciones de compilación y tests en Linux, `dotnet build` de la solución y la
   verificación Windows cuando el entorno lo permita.
5. Validar el change con OpenSpec; no archivar hasta la confirmación del usuario.
