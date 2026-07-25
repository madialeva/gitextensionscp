using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitUI;
using Microsoft.VisualStudio.Threading;

namespace GitExtensions.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ──────────────────────────────────────────────────────────────────
            // JoinableTaskContext initialization with AvaloniaSynchronizationContext
            // ──────────────────────────────────────────────────────────────────
            //
            // WHY IS THIS HERE?
            //   GitExtensions uses Microsoft.VisualStudio.Threading (VS-Threading)
            //   extensively: ThreadHelper.FileAndForget, SwitchToMainThreadAsync,
            //   JoinableTaskFactory.Run, etc. — 323 usages across the codebase.
            //   All of these depend on a shared JoinableTaskContext that captures
            //   the UI thread's SynchronizationContext so background work can
            //   switch back to the main thread.
            //
            //   In WinForms, this is done in Program.cs by creating a dummy Form:
            //     using (new Form()) { ThreadHelper.JoinableTaskContext = new JoinableTaskContext(); }
            //   The Form installs WindowsFormsSynchronizationContext, which the
            //   JoinableTaskContext constructor captures via SynchronizationContext.Current.
            //
            // WHY HERE AND NOT EARLIER?
            //   Avalonia installs its SynchronizationContext (AvaloniaSynchronizationContext)
            //   during AppBuilder.StartWithClassicDesktopLifetime(), which runs AFTER
            //   the App constructor but BEFORE OnFrameworkInitializationCompleted().
            //   If we initialize JoinableTaskContext too early (e.g. in the constructor
            //   or in Initialize()), SynchronizationContext.Current would be null or
            //   the default .NET context — and SwitchToMainThreadAsync would not
            //   return to the Avalonia UI thread.
            //
            //   By the time we reach this method, AppBuilder has already called
            //   AvaloniaSynchronizationContext.AutoInstall(), so
            //   SynchronizationContext.Current is the correct Avalonia context.
            //
            // VALIDATION (2026-07-25):
            //   A spike button was added during implementation (change 1.1b) that
            //   exercised the full flow: FileAndForget → background Task.Delay →
            //   SwitchToMainThreadAsync → update UI. The test passed: the UI updated
            //   correctly from the main thread. JoinableTaskContext is compatible
            //   with AvaloniaSynchronizationContext.
            //
            // SEE ALSO:
            //   openspec/changes/archive/2026-07-25-jtf-replumbing/design.md
            //   AVALONIA_MIGRATION_ANALYSIS.md §10.8
            //   WinForms init: src/app/GitExtensions/Program.cs:113-118
            // ──────────────────────────────────────────────────────────────────
            ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
