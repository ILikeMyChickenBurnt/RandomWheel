using RandomWheel.Services;
using Xunit;

namespace RandomWheel.Tests
{
    public class RandomServiceTests
    {
        [Fact]
        public void NextIndex_ReturnsWithinBounds()
        {
            var rng = new RandomService();
            for (int n = 1; n <= 100; n++)
            {
                var idx = rng.NextIndex(n);
                Assert.InRange(idx, 0, n - 1);
            }
        }
    }
}
