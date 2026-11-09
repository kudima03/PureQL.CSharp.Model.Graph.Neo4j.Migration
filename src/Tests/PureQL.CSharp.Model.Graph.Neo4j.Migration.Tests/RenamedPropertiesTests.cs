using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

[Collection(Neo4jDatabase.Name)]
public sealed class RenamedPropertiesTests(Neo4jFixture fixture)
{
    private static readonly ModelVersion V1 = new ModelVersion("1.0.0");

    private static readonly ModelVersion V2 = new ModelVersion("2.0.0");

    [Fact]
    public async Task RenamesPropertiesOfLabelledNodes()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:Record {id: 'a', old: 'x', kept: true}), "
                + "(:Record {id: 'b', kept: false}), "
                + "(:Other {id: 'c', old: 'y'})"
        );
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new RenamedProperties(
            V1,
            V2,
            "Record",
            new Dictionary<string, string> { ["old"] = "new" }
        ).ApplyAsync(session);
        List<StoredNode> nodes = await fixture.NodesAsync();
        Assert.Equal(
            new Dictionary<string, object>
            {
                ["id"] = "a",
                ["new"] = "x",
                ["kept"] = true,
            },
            nodes[0].Properties
        );
        Assert.Equal(
            new Dictionary<string, object> { ["id"] = "b", ["kept"] = false },
            nodes[1].Properties
        );
        Assert.Equal(
            new Dictionary<string, object> { ["id"] = "c", ["old"] = "y" },
            nodes[2].Properties
        );
    }

    [Fact]
    public async Task SwapsChainsAndReplacesProperties()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:Record {id: 'a', p: 1, q: 2, r: [3, 4], s: 5, t: 6})"
        );
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new RenamedProperties(
            V1,
            V2,
            "Record",
            new Dictionary<string, string>
            {
                ["p"] = "q",
                ["q"] = "p",
                ["r"] = "s",
                ["s"] = "u",
                ["missing"] = "t",
            }
        ).ApplyAsync(session);
        IReadOnlyDictionary<string, object> properties = Assert
            .Single(await fixture.NodesAsync())
            .Properties;
        Assert.Equal(
            ["id", "p", "q", "s", "t", "u"],
            properties.Keys.Order(StringComparer.Ordinal)
        );
        Assert.Equal(2L, properties["p"]);
        Assert.Equal(1L, properties["q"]);
        Assert.Equal([3L, 4L], ((List<object>)properties["s"]).Cast<long>());
        Assert.Equal(5L, properties["u"]);
        Assert.Equal(6L, properties["t"]);
    }

    [Fact]
    public async Task ThrowsOnNullRunner()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new RenamedProperties(
                V1,
                V2,
                "Record",
                new Dictionary<string, string>()
            ).ApplyAsync(null!)
        );
    }

    [Fact]
    public async Task ThrowsWhenCancelled()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:Record {id: 'a', old: 1})");
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new RenamedProperties(
                V1,
                V2,
                "Record",
                new Dictionary<string, string> { ["old"] = "new" }
            ).ApplyAsync(session, new CancellationToken(true))
        );
        Assert.Equal(1L, Assert.Single(await fixture.NodesAsync()).Properties["old"]);
    }
}
