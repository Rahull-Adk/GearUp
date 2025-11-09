using System.Text;
using System.Text.Json;
using GearUp.Application.Services;
using Microsoft.Extensions.Caching.Distributed;
using Moq;


namespace GearUp.UnitTests.Application
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _dist = new();

        private class TestObj { public int Value { get; set; } }

        [Fact]
        public async Task GetAsync_ReturnsDeserializedValue()
        {
            var obj = new TestObj { Value = 5 };
            _dist.Setup(c => c.GetAsync("k", It.IsAny<CancellationToken>()))
   .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TestObj { Value = 5 })));
            var svc = new CacheService(_dist.Object);
            var res = await svc.GetAsync<TestObj>("k");
            Assert.NotNull(res);
            Assert.Equal(5, res!.Value);
          

        }

        [Fact]
        public async Task SetAsync_SerializesValue()
        {
            var svc = new CacheService(_dist.Object);
            await svc.SetAsync("k", new TestObj { Value = 7 });
            _dist.Verify(c => c.SetAsync("k", It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains('7')), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_RemovesKey()
        {
            var svc = new CacheService(_dist.Object);
            await svc.RemoveAsync("k");
            _dist.Verify(c => c.RemoveAsync("k", default), Times.Once);
        }
    }
}