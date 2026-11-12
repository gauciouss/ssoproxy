using ssoproxy.Repository;
using Xunit;

namespace ssoproxy.Tests;

public class SqlInjectionGuardTests
{
    [Theory]
    [InlineData("admin' OR 1=1 --")]
    [InlineData("'; DROP TABLE users; --")]
    [InlineData("union select password from users")]
    public void IsRequestValueSuspicious_ShouldRejectDangerousSqlInput(string input)
    {
        var result = SqlInjectionGuard.IsRequestValueSuspicious(input);
        Assert.True(result);
    }

    [Theory]
    [InlineData("<script>alert('x')</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("<svg/onload=alert(1)>")]
    [InlineData("<body onload=alert(1)>")]
    public void IsRequestValueSuspicious_ShouldRejectDangerousXssInput(string input)
    {
        var result = SqlInjectionGuard.IsRequestValueSuspicious(input);
        Assert.True(result);
    }

    [Theory]
    [InlineData("normal-user-name")]
    [InlineData("hello world")]
    [InlineData("test-123")]
    [InlineData("text/html, */*")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("https://example.com/path?x=1&y=2")]
    public void IsRequestValueSuspicious_ShouldAllowSafeInput(string input)
    {
        var result = SqlInjectionGuard.IsRequestValueSuspicious(input);
        Assert.False(result);
    }
}
