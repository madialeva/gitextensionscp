using GitCommands.Git;
using GitCommands.Submodules;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using Microsoft.Extensions.DependencyInjection;

namespace GitCommands;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitCommands(this IServiceCollection services)
    {
        services.AddSingleton<IGitExecutorProvider>(sp =>
        {
            IGitDirectoryResolver resolver = sp.GetRequiredService<IGitDirectoryResolver>();
            return new GitExecutorProvider(resolver);
        });

        services.AddSingleton<ISubmoduleStatusProvider>(sp =>
        {
            IGitExecutorProvider executor = sp.GetRequiredService<IGitExecutorProvider>();
            return new SubmoduleStatusProvider(executor);
        });

        services.AddSingleton<IGitBranchNameNormaliser>(new GitBranchNameNormaliser());

        return services;
    }
}
