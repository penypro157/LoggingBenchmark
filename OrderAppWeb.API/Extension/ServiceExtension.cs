using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
namespace OrderAppWeb.API.Extension
{
    public static class ServiceExtensions
    {

        public static IServiceCollection AddCachingConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var cachingConnetionString = configuration.GetSection("RedisSetting:ConnectionString").Value;
            if (!String.IsNullOrEmpty(cachingConnetionString))
            {
                // Đăng ký StackExchange.Redis ConnectionMultiplexer
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    return ConnectionMultiplexer.Connect(cachingConnetionString);
                });
            }
            else throw new ArgumentNullException("Caching ConnectionString is not configured");
            return services;
        }
    }
}
