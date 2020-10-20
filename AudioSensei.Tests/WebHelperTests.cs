using Xunit;

namespace AudioSensei.Tests
{
    public class WebHelperTests
    {
        [Fact]
        public void TestUserAgent()
        {
            var userAgent = WebHelper.UserAgent;
            Assert.NotNull(userAgent);
            Assert.False(string.IsNullOrWhiteSpace(userAgent));
        }

        [Fact]
        public void TestFakeUserAgent()
        {
            var userAgent = WebHelper.FakeUserAgent;
            Assert.NotNull(userAgent);
            Assert.False(string.IsNullOrWhiteSpace(userAgent));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestCreateHttpClient(bool fakeUserAgent)
        {
            using (var httpClient = WebHelper.CreateHttpClient(fakeUserAgent))
            {
                Assert.NotNull(httpClient);
                Assert.Equal(fakeUserAgent ? WebHelper.FakeUserAgent : WebHelper.UserAgent, httpClient.DefaultRequestHeaders.UserAgent.ToString());
            }
        }
    }
}
