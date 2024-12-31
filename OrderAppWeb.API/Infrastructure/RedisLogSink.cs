using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Serilog.Core;
using Serilog.Events;
using StackExchange.Redis;

namespace OrderAppWeb.API.Infrastructure
{
    public class RedisLogSink : ILogEventSink
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly string _logKey = "logs";
        public RedisLogSink(IConfiguration configuration)
        {
            var cachingConnetionString = configuration.GetSection("RedisSetting:ConnectionString").Value;
            if (!String.IsNullOrEmpty(cachingConnetionString))
            {
                _connectionMultiplexer = ConnectionMultiplexer.Connect(cachingConnetionString);
            }
            else throw new Exception("Invalid RedisSetting:ConnectionString");

        }

        public void Emit(LogEvent logEvent)
        {
            var logMessage = new
            {
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage()
            };

            if (logEvent.Level == LogEventLevel.Information)
            {
                // Ghi log vào Redis (dạng JSON)
                _connectionMultiplexer.GetDatabase().ListRightPush($"{_logKey}_infor", Newtonsoft.Json.JsonConvert.SerializeObject(logMessage));
            }
            else if (logEvent.Exception != null)
            {
                // Ghi log vào Redis (dạng JSON)
                _connectionMultiplexer.GetDatabase().ListLeftPush($"{_logKey}_exception", Newtonsoft.Json.JsonConvert.SerializeObject(logMessage));
            }


        }
    }
}
