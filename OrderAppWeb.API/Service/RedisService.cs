namespace OrderAppWeb.API.Service
{
    using StackExchange.Redis;

    public class RedisService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task SetValueAsync(string key, string value)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.StringSetAsync(key, value);
        }

        public async Task<string?> GetValueAsync(string key)
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.StringGetAsync(key);
        }

        public IDatabase GetDatabase()
        {
            return _connectionMultiplexer.GetDatabase();
        }

    }

}
