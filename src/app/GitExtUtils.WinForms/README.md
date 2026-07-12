# GitExtUtils.WinForms

Windows/WinForms-only sibling of `GitExtUtils`, created in change 0.3 (`extract-ui-interops`,
see `openspec/changes/archive/`). It contains the code that talks directly to Windows or
WinForms and therefore can never be cross-platform:

- **Win32 interops** (`GitUI/Interops/`): P/Invoke declarations against `user32.dll` and
  `comctl32.dll`, plus their support structs and constants.
- **WinForms control extensions** (`GitUI/`): helpers for `Control`, `ToolStrip`, `ComboBox`,
  `ListView`, `PropertyGrid`, `TableLayoutPanel`, `ImageList`, DPI scaling (`DpiUtil`),
  hotkeys, cursors.
- **GDI+ theming** (`GitUI/Theming/`): the *painting* half of the theming subsystem
  (`ThemeFix`, `TabControlRenderer`, `ColorHelper`, …). The *data* half (`Theme`, `ThemeId`,
  `AppColor`, palettes) stays in `GitExtUtils` because the core reads it.
- **WinForms-bound utilities** (root): `MessageBoxes`, `ClipboardUtil`, `FontParser` (GDI+
  `Font`), `UIExtensions`, and the `Control`-based halves of `ThreadHelper`/`TaskManager`
  (`ControlThreadHelper`).

## Why the namespaces do not match the assembly name

Types here keep their original namespaces (`GitExtUtils.*`, `GitUI.*`,
`GitExtUtils.GitUI.Theming`, …). This was a deliberate decision (design D2 of the change):
the extraction changed the *packaging*, not the *contract*, and keeping the namespaces meant
zero source changes in the hundreds of consumer files — only project references moved.
C# does not require namespaces to match assembly names.

## Dependency rules

- This project references `GitExtUtils` and `GitExtensions.Extensibility` — never the other
  way around: both must stay UI-technology neutral (they compile with
  `UseWindowsForms=false` as a permanent guardrail).
- `GitCommands` references this project **temporarily** (it still shows message boxes
  directly); change 0.4 abstracts those user prompts and removes the reference so the core
  can retarget to plain `net10.0`.
