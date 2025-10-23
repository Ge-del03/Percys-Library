using System;
using ComicReader.ContinuousReader;
using Xunit;

namespace ComicReader.Tests
{
    public class CacheManagerTests
    {
        [Fact]
        public void SetAndGetValue()
        {
            var cache = new CacheManager<string, string>(maxItems: 10, ttl: TimeSpan.FromSeconds(2));
            cache.Set("k1", "v1");
            Assert.True(cache.TryGet("k1", out var v));
            Assert.Equal("v1", v);
        }

        [Fact]
        public void ExpirationWorks()
        {
            var cache = new CacheManager<string, string>(maxItems: 10, ttl: TimeSpan.FromMilliseconds(100));
            cache.Set("k2", "v2");
            System.Threading.Thread.Sleep(200);
            Assert.False(cache.TryGet("k2", out var v));
        }
    }
}
