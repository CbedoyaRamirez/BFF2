using StackExchange.Redis;

namespace Chubb.Bot.AI.Assistant.Infrastructure.Redis;

public class RedisConnectionFactory
{
    private static Lazy<ConnectionMultiplexer>? _lazyConnection;
    private static readonly object _lock = new object();

    public static void Initialize(string connectionString)
    {
        lock (_lock)
        {
            if (_lazyConnection == null)
            {
                _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
                {
                    var options = ConfigurationOptions.Parse(connectionString);
                    options.ConnectTimeout = 5000;
                    options.SyncTimeout = 5000;
                    options.AbortOnConnectFail = false;
                    return ConnectionMultiplexer.Connect(options);
                });
            }
        }
    }

    public static ConnectionMultiplexer Connection
    {
        get
        {
            if (_lazyConnection == null)
            {
                throw new InvalidOperationException("RedisConnectionFactory must be initialized before use");
            }
            return _lazyConnection.Value;
        }
    }

    public static IDatabase GetDatabase(int db = -1)
    {
        return Connection.GetDatabase(db);
    }
}
