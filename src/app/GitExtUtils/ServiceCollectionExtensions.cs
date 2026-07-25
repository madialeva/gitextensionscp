using Microsoft.Extensions.DependencyInjection;

namespace GitExtUtils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitExtUtils(this IServiceCollection services)
    {
        services.AddSingleton<ISubscribableTraceListener>(new SubscribableTraceListener());
        return services;
    }
}
