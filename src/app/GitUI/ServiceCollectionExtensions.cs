using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.CommandsDialogs;
using GitUI.ConsoleEmulation;
using GitUI.ConsoleEmulation.ConEmu;
using GitUI.ConsoleEmulation.Mintty;
using GitUI.Hotkey;
using GitUI.Models;
using GitUI.ScriptsEngine;
using Microsoft.Extensions.DependencyInjection;
using ResourceManager;

namespace GitUI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitUI(this IServiceCollection services)
    {
        ScriptsManager scriptsManager = new();
        HotkeySettingsManager hotkeySettingsManager = new(scriptsManager);

        OutputHistoryModel outputHistoryModel = new(AppSettings.OutputHistoryDepth.Value);

        services.AddSingleton<IWindowsJumpListManager>(sp =>
            new WindowsJumpListManager(sp.GetRequiredService<IRepositoryDescriptionProvider>()));
        services.AddSingleton<IScriptsManager>(scriptsManager);
        services.AddSingleton<IScriptsRunner>(scriptsManager);
        services.AddSingleton<IHotkeySettingsManager>(hotkeySettingsManager);
        services.AddSingleton<IHotkeySettingsLoader>(hotkeySettingsManager);
        services.AddSingleton<ISimplePromptCreator>(new SimplePromptCreator());
        services.AddSingleton<IFilePromptCreator>(new FilePromptCreator());
        services.AddSingleton<IOutputHistoryProvider>(outputHistoryModel);
        services.AddSingleton<IOutputHistoryRecorder>(outputHistoryModel);

        services.AddSingleton<IRepositoryCurrentBranchNameCache>(sp =>
        {
            IGitExecutorProvider executor = sp.GetRequiredService<IGitExecutorProvider>();
            return new RepositoryCurrentBranchNameCache(new RepositoryCurrentBranchNameProvider(executor));
        });

        InvalidRepositoryRemover invalidRepositoryRemover = new();
        services.AddSingleton<IInvalidRepositoryRemover>(invalidRepositoryRemover);
        services.AddSingleton<IRepositoryHistoryUIService>(sp =>
        {
            IGitExecutorProvider executor = sp.GetRequiredService<IGitExecutorProvider>();
            IRepositoryCurrentBranchNameCache branchNameCache = sp.GetRequiredService<IRepositoryCurrentBranchNameCache>();
            return new RepositoryHistoryUIService(executor, branchNameCache, invalidRepositoryRemover);
        });

        services.AddSingleton<IConsoleEmulatorsRegistry>(
            new ConsoleEmulatorsRegistry(
                consoleEmulators: [new ConEmuConsoleEmulator(), new MinttyConsoleEmulator()],
                useConsoleEmulation: AppSettings.UseConsoleEmulatorForCommands,
                consoleEmulatorName: AppSettings.ConsoleEmulatorName,
                consoleEmulatorTheme: AppSettings.ConEmuStyle,
                consoleFont: () => AppSettings.ConEmuConsoleFont));

        return services;
    }

    public static void WireTraceListener(IServiceProvider serviceProvider)
    {
        ISubscribableTraceListener traceListener = serviceProvider.GetRequiredService<ISubscribableTraceListener>();
        OutputHistoryModel outputHistoryModel = (OutputHistoryModel)serviceProvider.GetRequiredService<IOutputHistoryProvider>();

        traceListener.TraceReceived += (in string message) =>
        {
#if DEBUG
            const char noBreakSpace = '\u00a0';
            if (message.Contains("Exception") || message.Contains($":{noBreakSpace}"))
#endif
            {
                outputHistoryModel.RecordHistory(message);
            }
        };
    }
}
