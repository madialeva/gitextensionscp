using GitCommands;
using GitCommands.Git;
using GitCommands.Submodules;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.ConsoleEmulation;
using GitUI.ConsoleEmulation.PlainText;
using GitUI.Hotkey;
using GitUI.Models;
using GitUI.ScriptsEngine;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ResourceManager;

namespace GitExtensions.UITests;

public static class GlobalServiceContainer
{
    public static IServiceProvider CreateDefaultMockServiceProvider(Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new();

        services.AddSingleton<IOutputHistoryProvider>(Substitute.For<IOutputHistoryProvider>());

        services.AddSingleton<IAppTitleGenerator>(Substitute.For<IAppTitleGenerator>());
        services.AddSingleton<IWindowsJumpListManager>(Substitute.For<IWindowsJumpListManager>());
        services.AddSingleton<ILinkFactory>(Substitute.For<ILinkFactory>());
        services.AddSingleton<IRepositoryHistoryUIService>(Substitute.For<IRepositoryHistoryUIService>());

        IScriptsManager scriptsManager = Substitute.For<IScriptsManager>();
        scriptsManager.GetScripts().Returns([]);
        services.AddSingleton<IScriptsManager>(scriptsManager);

        services.AddSingleton<IScriptsRunner>(Substitute.For<IScriptsRunner>());

        services.AddSingleton<IHotkeySettingsManager>(Substitute.For<IHotkeySettingsManager>());
        services.AddSingleton<IHotkeySettingsLoader>(Substitute.For<IHotkeySettingsLoader>());

        services.AddSingleton<ISubmoduleStatusProvider>(Substitute.For<ISubmoduleStatusProvider>());

        IGitBranchNameNormaliser branchNameNormaliser = Substitute.For<IGitBranchNameNormaliser>();
        branchNameNormaliser.Normalise(Arg.Any<string?>(), Arg.Any<GitBranchNameOptions>())
            .Returns(callInfo => callInfo.Arg<string?>());
        services.AddSingleton<IGitBranchNameNormaliser>(branchNameNormaliser);

        services.AddSingleton<IGitExecutorProvider>(new GitExecutorProvider(new GitDirectoryResolver()));

        services.AddSingleton<IConsoleEmulatorsRegistry>(PlainTextConsoleEmulatorsRegistry.Instance);

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
