using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using StackExchange.Redis;

namespace ContosoTeamStats
{
    public interface ICacheService
    {
        ValueTask AddOrUpdateAsync(string key, object value, TimeSpan? expiresIn = null);

        ValueTask<T> GetAsync<T>(string key) where T : class;
        ValueTask<IReadOnlyCollection<T>> GetByKeyMatchAsync<T>(string pattern) where T : class;

        ValueTask RemoveAsync(string key);
        ValueTask RemoveByKeyMatchAsync(string pattern);

        Task SortedSetAddAsync<T>(string key, T entry, int score);
        ValueTask<IEnumerable<T>> SortedSetRangeByRankWithScoresAsync<T>(string key, long start = 0, long stop = -1, Order order = Order.Ascending) where T : class;
    }

    public class RedisCacheService : ICacheService
    {
        private readonly IRedisDatabase _redisDatabase;

        public RedisCacheService(IRedisDatabase redisDatabase)
        {
            _redisDatabase = redisDatabase;
        }

        public async ValueTask<T?> GetAsync<T>(string key) where T : class => await _redisDatabase.GetAsync<T?>(key);
        public async ValueTask AddOrUpdateAsync(string key, object value, TimeSpan? expiresIn = null) => await _redisDatabase.AddAsync(key, value, expiresIn.Value);
        public async ValueTask RemoveAsync(string key) => await _redisDatabase.RemoveAsync(key);
        public async ValueTask RemoveByKeyMatchAsync(string pattern)
        {
            // get keys (SCAN)
            var keys = await _redisDatabase.SearchKeysAsync(pattern);
            await _redisDatabase.RemoveAllAsync(keys.ToArray());
        }
        public async ValueTask<IReadOnlyCollection<T>> GetByKeyMatchAsync<T>(string pattern) where T : class
        {
            // get keys (SCAN)
            var keys = await _redisDatabase.SearchKeysAsync(pattern);

            // get results (MGET)
            var results = await _redisDatabase.GetAllAsync<T?>(keys.ToHashSet());

            return results.Where(v =>
            {
                var (key, value) = v;
                return (value != null);

            }).Select(v => v.Value).ToArray();
        }

        public async ValueTask<IEnumerable<T>> SortedSetRangeByRankWithScoresAsync<T>(string key, long start = 0, long stop = -1, Order order = Order.Ascending) where T : class
        {
            var result = await _redisDatabase.SortedSetRangeByRankWithScoresAsync<T>(key, start, stop, order);
            return result.Select(r => r.Element);
        }

        public async Task SortedSetAddAsync<T>(string key, T entry, int score) => await _redisDatabase.SortedSetAddAsync(key, entry, score);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
