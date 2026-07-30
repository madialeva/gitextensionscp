using System.IO.Abstractions;
using GitCommands;
using GitCommands.Git;
using GitExtUtils;
using Microsoft.Extensions.DependencyInjection;

namespace GitExtensions.Avalonia;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAvaloniaServices(this IServiceCollection services)
    {
        services.AddGitExtUtils();

        FileSystem fileSystem = new();
        GitDirectoryResolver gitDirectoryResolver = new(fileSystem);

        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<IGitDirectoryResolver>(gitDirectoryResolver);

        services.AddGitCommands();

        return services;
    }
}
