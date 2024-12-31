using Microsoft.EntityFrameworkCore.Storage;
using OrderAppWeb.API.Infrastructure;
using Serilog.Configuration;
using Serilog;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Serilog.Core;

namespace OrderAppWeb.API.Extension
{
    public static class RedisExtension
    {
        public static LoggerConfiguration Redis(
       this LoggerSinkConfiguration loggerConfiguration,
       IConfiguration configuration)
        {
            return loggerConfiguration.Sink(new RedisLogSink(configuration));
        }
    }
}
