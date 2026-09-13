using XN1Lab.XN1Finance.Tracking.Preview;

namespace XN1Lab.XN1Finance.Tracking.Tests;

public sealed class PreviewPortTests
{
    [Fact]
    public void Default_preserves_existing_preview_port() => Assert.Equal(7547, PreviewPort.Parse([]));

    [Theory]
    [InlineData("1024", 1024)]
    [InlineData("7548", 7548)]
    [InlineData("65535", 65535)]
    public void Explicit_preview_port_accepts_only_the_bounded_numeric_value(string value, int expected)
        => Assert.Equal(expected, PreviewPort.Parse(["--preview-port", value]));

    [Theory]
    [InlineData("1023")]
    [InlineData("65536")]
    [InlineData("0")]
    [InlineData("0.0.0.0:7548")]
    [InlineData("http://+:7548")]
    [InlineData("7548.5")]
    public void Invalid_ports_and_listener_addresses_are_rejected(string value)
        => Assert.Throws<ArgumentException>(() => PreviewPort.Parse(["--preview-port", value]));

    [Fact]
    public void Generic_URL_overrides_missing_values_and_extra_switches_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => PreviewPort.Parse(["--urls", "http://0.0.0.0:7548"]));
        Assert.Throws<ArgumentException>(() => PreviewPort.Parse(["--preview-port"]));
        Assert.Throws<ArgumentException>(() => PreviewPort.Parse(["--preview-port", "7548", "--urls", "http://+:7548"]));
    }
}
