using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace TokiwadaiPride.Redis;

public static class RedisExtensions
{
    public static void AddSessionDatabase(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost"));
        serviceCollection.AddSingleton<ISessionDatabase, SessionDatabase>();
    }
}