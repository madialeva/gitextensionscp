## Why

La shell Avalonia ya presenta textos visibles, pero todavía no tiene una tubería de
localización propia. Seguir construyendo vistas, empezando por la lista de commits 1.3,
con textos hardcodeados aumentaría la deuda y haría más costoso separar después la
presentación Avalonia de la infraestructura WinForms.

## What Changes

- Añadir una capacidad de localización portable para la shell Avalonia, con recursos
  identificados por clave y resolución de cultura.
- Leer los recursos de traducción existentes en formato XLIFF sin introducir una
  dependencia de `ResourceManager` o WinForms en `GitExtensions.Avalonia`.
- Conectar los textos de la shell actual a bindings de localización, incluyendo el
  estado de bienvenida, apertura de repositorio, errores y comandos visibles.
- Definir fallback determinista a inglés cuando falte una traducción o un catálogo.
- Permitir cambiar la cultura de la shell y actualizar los textos observables sin
  reiniciar la aplicación.
- Añadir pruebas headless para carga, fallback, cambio de cultura y resolución de
  claves desde XAML/ViewModels.
- Mantener sin cambios el comportamiento de localización de la aplicación WinForms.

## Capabilities

### New Capabilities

- `avalonia-localization`: recursos, resolución de cultura, fallback y bindings de
  localización para la shell Avalonia.

### Modified Capabilities

- `avalonia-shell`: los textos visibles de la shell deben proceder de la capacidad de
  localización en lugar de depender de literales fijos.

## Impact

- Afecta al proyecto `GitExtensions.Avalonia`, sus ViewModels, vistas XAML y pruebas
  headless.
- Añade un lector/adaptador portable para los catálogos XLIFF existentes, sin modificar
  `ResourceManager`, `TranslatedStrings` ni los proyectos WinForms.
- Puede requerir una pequeña abstracción compartida de recursos o servicios, pero no
  añade dependencias de UI Windows-only al core portable.
- La migración completa de toda la infraestructura XLIFF, plugins, edición de
  traducciones y localización de la aplicación WinForms queda fuera de este change.
