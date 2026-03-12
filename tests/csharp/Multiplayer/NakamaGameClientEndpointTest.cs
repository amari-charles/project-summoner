namespace Fateforged.Tests.Multiplayer;

using Fateforged.Multiplayer.Backend;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class NakamaGameClientEndpointTest
{
    [TestCase]
    public void ResolveEndpointFromArgs_UsesDefaultsWhenNoOverrides()
    {
        var endpoint = NakamaGameClient.ResolveEndpointFromArgs(
            args: [],
            defaultHost: "127.0.0.1",
            defaultPort: 7350,
            defaultServerKey: "defaultkey"
        );

        AssertThat(endpoint.Host).IsEqual("127.0.0.1");
        AssertThat(endpoint.Port).IsEqual(7350);
        AssertThat(endpoint.ServerKey).IsEqual("defaultkey");
    }

    [TestCase]
    public void ResolveEndpointFromArgs_AppliesOverridesWhenValid()
    {
        var endpoint = NakamaGameClient.ResolveEndpointFromArgs(
            args: ["--nakama-host=localhost", "--nakama-port=8350", "--nakama-server-key=e2e_key"],
            defaultHost: "127.0.0.1",
            defaultPort: 7350,
            defaultServerKey: "defaultkey"
        );

        AssertThat(endpoint.Host).IsEqual("localhost");
        AssertThat(endpoint.Port).IsEqual(8350);
        AssertThat(endpoint.ServerKey).IsEqual("e2e_key");
    }

    [TestCase]
    public void ResolveEndpointFromArgs_KeepsDefaultPortWhenInvalid()
    {
        var endpoint = NakamaGameClient.ResolveEndpointFromArgs(
            args: ["--nakama-port=abc"],
            defaultHost: "127.0.0.1",
            defaultPort: 7350,
            defaultServerKey: "defaultkey"
        );

        AssertThat(endpoint.Port).IsEqual(7350);
    }
}
