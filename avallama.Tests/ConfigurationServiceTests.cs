using avallama.Services;
using Xunit;

namespace avallama.Tests;

public class ConfigurationServiceTests
{
    [Fact]
    public void ReadSetting_UnknownKey_ReturnsEmptyString()
    {
        var svc = new ConfigurationService();
        var value = svc.ReadSetting("__nonexistent_key__");
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void SaveSetting_ThenReadSetting_ReturnsSavedValue()
    {
        var svc = new ConfigurationService();
        var key = "unit-test-key";
        var expected = "unit-test-value";

        svc.SaveSetting(key, expected);
        var actual = svc.ReadSetting(key);

        Assert.Equal(expected, actual);
    }
}
