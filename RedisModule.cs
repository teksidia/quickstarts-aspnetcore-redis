using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Protobuf;
using System.Linq;

namespace ContosoTeamStats
{
    public static class ServicesConfiguration
    {
        /// <summary>
        /// See https://github.com/imperugo/StackExchange.Redis.Extensions/blob/master/doc/README.md
        /// </summary>
        public static void AddRedis(this IServiceCollection services, IConfiguration config)
        {
            var redisConfig = config.GetSection("Redis").Get<RedisSettings>();

            var configuration = new RedisConfiguration
            {
                Database = 0,
                SyncTimeout = redisConfig.SyncTimeout,
                AbortOnConnectFail = redisConfig.AbortOnConnectFail,
                Password = redisConfig.Password,
                Hosts = redisConfig.Hosts.Select(h => new RedisHost() { Host = h.Host, Port = h.Port }).ToArray(),
                Ssl = true
            };

            services.AddStackExchangeRedisExtensions<ProtobufSerializer>(configuration);
            services.AddSingleton<ICacheService, RedisCacheService>();
        }


    }
}
