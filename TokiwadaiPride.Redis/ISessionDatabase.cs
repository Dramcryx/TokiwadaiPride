using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace TokiwadaiPride.Redis;

public interface ISessionDatabase
{
    Task<string> CreateSessionAsync(long chatId);

    Task<long?> GetChatIdAsync(string sessionId);
}

internal class SessionDatabase : ISessionDatabase
{
    private readonly ILogger<SessionDatabase> _logger;

    private readonly IDatabase _redis;

    public SessionDatabase(ILogger<SessionDatabase> logger, IConnectionMultiplexer multiplexer)
    {
        _logger = logger;
        _redis = multiplexer.GetDatabase();
    }

    public async Task<string> CreateSessionAsync(long userId)
    {
        var sessionId = Guid.NewGuid().ToString();

        var setTask = _redis.StringSetAsync(sessionId, userId.ToString());
        var expireTask = _redis.KeyExpireAsync(sessionId, TimeSpan.FromSeconds(3600));
        await Task.WhenAll(setTask, expireTask);

        return sessionId;
    }

    public async Task<long?> GetChatIdAsync(string sessionId)
    {
        var result = await _redis.StringGetAsync(sessionId);
        return result.Equals(RedisValue.Null) ? null : long.Parse(result);
    }
}
