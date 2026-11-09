using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

[Collection(Neo4jDatabase.Name)]
public sealed class RelabeledRecordsTests(Neo4jFixture fixture)
{
    private static readonly ModelVersion V1 = new ModelVersion("1.0.0");

    private static readonly ModelVersion V2 = new ModelVersion("2.0.0");

    [Fact]
    public async Task RelabelsNodes()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:Old:PureQLNode {id: 'a', x: 1})-[:Left]->(:Old {id: 'b'}), "
                + "(:Other {id: 'c'})"
        );
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new RelabeledRecords(
            V1,
            V2,
            new Dictionary<string, string> { ["Old"] = "New" }
        ).ApplyAsync(session);
        List<StoredNode> nodes = await fixture.NodesAsync();
        Assert.Equal(["New", "PureQLNode"], nodes[0].Labels);
        Assert.Equal(1L, nodes[0].Properties["x"]);
        Assert.Equal(["New"], nodes[1].Labels);
        Assert.Equal(["Other"], nodes[2].Labels);
        List<IRecord> relationships = await fixture.RunAsync(
            "MATCH (:New {id: 'a'})-[r:Left]->(:New {id: 'b'}) RETURN r"
        );
        _ = Assert.Single(relationships);
    }

    [Fact]
    public async Task SwapsAndChainsLabels()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:A {id: 'a'}), (:B {id: 'b'}), (:C {id: 'c'}), (:D {id: 'd'})"
        );
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new RelabeledRecords(
            V1,
            V2,
            new Dictionary<string, string>
            {
                ["A"] = "B",
                ["B"] = "A",
                ["C"] = "D",
                ["D"] = "E",
            }
        ).ApplyAsync(session);
        List<StoredNode> nodes = await fixture.NodesAsync();
        Assert.Equal(
            [
                ["B"],
                ["A"],
                ["D"],
                ["E"],
            ],
            nodes.Select(node => node.Labels).ToList()
        );
    }

    [Fact]
    public async Task LeavesGraphWithoutOldLabels()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:Other {id: 'a'})");
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new RelabeledRecords(
            V1,
            V2,
            new Dictionary<string, string> { ["Old"] = "New" }
        ).ApplyAsync(session);
        Assert.Equal(["Other"], Assert.Single(await fixture.NodesAsync()).Labels);
    }

    [Fact]
    public async Task AppliesNoPairs()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:Other {id: 'a'})");
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new RelabeledRecords(V1, V2, new Dictionary<string, string>()).ApplyAsync(
            session
        );
        Assert.Equal(["Other"], Assert.Single(await fixture.NodesAsync()).Labels);
    }

    [Fact]
    public async Task ThrowsOnNullRunner()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new RelabeledRecords(V1, V2, new Dictionary<string, string>()).ApplyAsync(
                null!
            )
        );
    }

    [Fact]
    public async Task ThrowsWhenCancelled()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:Old {id: 'a'})");
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new RelabeledRecords(
                V1,
                V2,
                new Dictionary<string, string> { ["Old"] = "New" }
            ).ApplyAsync(session, new CancellationToken(true))
        );
        Assert.Equal(["Old"], Assert.Single(await fixture.NodesAsync()).Labels);
    }
}
