using System.IO.Abstractions;
using GitCommands;
using GitCommands.Git;
using GitCommands.UserRepositoryHistory;
using GitExtUtils;
using GitUI;
using Microsoft.Extensions.DependencyInjection;
using ResourceManager;

namespace GitExtensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitExtensions(this IServiceCollection services)
    {
        services.AddGitExtUtils();

        FileSystem fileSystem = new();
        GitDirectoryResolver gitDirectoryResolver = new(fileSystem);
        RepositoryDescriptionProvider repositoryDescriptionProvider = new(gitDirectoryResolver);

        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<IGitDirectoryResolver>(gitDirectoryResolver);
        services.AddSingleton<IRepositoryDescriptionProvider>(repositoryDescriptionProvider);
        services.AddSingleton<IAppTitleGenerator>(new AppTitleGenerator(repositoryDescriptionProvider));
        services.AddSingleton<ILinkFactory>(new LinkFactory());

        services.AddGitCommands();
        services.AddGitUI();

        return services;
    }
}
