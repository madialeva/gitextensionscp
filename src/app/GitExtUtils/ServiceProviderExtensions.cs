namespace GitExtUtils;

/// <summary>
/// Extension methods for getting services from an <see cref="IServiceProvider" />.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
    public static T? GetService<T>(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return (T?)provider.GetService(typeof(T));
    }
}
