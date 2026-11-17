using Microsoft.Extensions.Configuration;
using ssoproxy.Repository.Redis;
using ssoproxy.Services.Auth;
using Xunit;

namespace ssoproxy.Tests;

public class TokenServiceTests
{
    [Fact]
    public void GenerateJweToken_ShouldBeAcceptedByCheckJWEToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwe:Secret"] = "sso-development-jwe-secret-32-bytes",
                ["Jwe:TokenExpiryMinutes"] = "30"
            })
            .Build();

        var tokenService = new TokenService(new FakeRedisDatabase(), configuration);
        var token = tokenService.GenerateJweToken("admin");

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(tokenService.CheckJWEToken(token));
    }

    private sealed class FakeRedisDatabase : IRedisDatabase
    {
        public Task<string?> GetStringAsync(string key) => Task.FromResult<string?>(null);

        public Task SetStringAsync(string key, string value, TimeSpan? expiry = null) => Task.CompletedTask;

        public Task<bool> RemoveAsync(string key) => Task.FromResult(true);
    }
}
