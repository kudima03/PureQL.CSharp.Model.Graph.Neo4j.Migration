using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

[Collection(Neo4jDatabase.Name)]
public sealed class MigratedQueriesTests(Neo4jFixture fixture)
{
    private static readonly ModelVersion V1 = new ModelVersion("1.0.0");

    private static readonly ModelVersion V2 = new ModelVersion("2.0.0");

    private static readonly ModelVersion V3 = new ModelVersion("3.0.0");

    [Fact]
    public async Task AppliesChainInOrder()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:PureQLModelVersion {value: '1.0.0'}), "
                + "(:A {id: 'a', old: 1}), (:Other {id: 'b', old: 2})"
        );
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new MigratedQueries(
            session,
            [
                new RenamedProperties(
                    V2,
                    V3,
                    "B",
                    new Dictionary<string, string> { ["old"] = "new" }
                ),
                new RelabeledRecords(
                    V1,
                    V2,
                    new Dictionary<string, string> { ["A"] = "B" }
                ),
            ]
        ).ApplyAsync();
        List<StoredNode> nodes = await fixture.NodesAsync();
        Assert.Equal(["B"], nodes[0].Labels);
        Assert.Equal(1L, nodes[0].Properties["new"]);
        Assert.False(nodes[0].Properties.ContainsKey("old"));
        Assert.Equal(["Other"], nodes[1].Labels);
        Assert.Equal(2L, nodes[1].Properties["old"]);
        Assert.Equal("3.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task AppliesOnlyMigrationsOfTheChain()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:PureQLModelVersion {value: '2.0.0'})");
        List<string> applied = [];
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new MigratedQueries(
            session,
            [
                new RecordingMigration(V3, new ModelVersion("4.0.0"), applied),
                new RecordingMigration(V1, V2, applied),
                new RecordingMigration(V2, V3, applied),
                new RecordingMigration(new ModelVersion("9.0.0"), V1, applied),
            ]
        ).ApplyAsync();
        Assert.Equal(["2.0.0->3.0.0", "3.0.0->4.0.0"], applied);
        Assert.Equal("4.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task LeavesMarkerWithoutMatchingMigration()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:PureQLModelVersion {value: '3.0.0'})");
        List<string> applied = [];
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new MigratedQueries(
            session,
            [new RecordingMigration(V1, V2, applied)]
        ).ApplyAsync();
        Assert.Empty(applied);
        Assert.Equal("3.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task ThrowsOnDuplicateFrom()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:PureQLModelVersion {value: '1.0.0'})");
        List<string> applied = [];
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MigratedQueries(
                session,
                [
                    new RecordingMigration(V1, V2, applied),
                    new RecordingMigration(new ModelVersion("1.0.0"), V3, applied),
                ]
            ).ApplyAsync()
        );
        Assert.Empty(applied);
        Assert.Equal("1.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task ThrowsOnCycle()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:PureQLModelVersion {value: '1.0.0'})");
        List<string> applied = [];
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MigratedQueries(
                session,
                [
                    new RecordingMigration(V1, V2, applied),
                    new RecordingMigration(V2, V1, applied),
                ]
            ).ApplyAsync()
        );
        Assert.Empty(applied);
        Assert.Equal("1.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task ThrowsWithoutMarker()
    {
        await fixture.ClearAsync();
        List<string> applied = [];
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MigratedQueries(
                session,
                [new RecordingMigration(V1, V2, applied)]
            ).ApplyAsync()
        );
        Assert.Empty(applied);
    }

    [Fact]
    public async Task MigratesStore()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:PureQLModelVersion {value: '1.0.0'}), (:A {id: 'a'})"
        );
        await new MigratedQueriesOf(
            fixture.Store,
            [
                new RelabeledRecords(
                    V1,
                    V2,
                    new Dictionary<string, string> { ["A"] = "B" }
                ),
                new RelabeledRecords(
                    V2,
                    V3,
                    new Dictionary<string, string> { ["B"] = "C" }
                ),
            ]
        ).ApplyAsync();
        Assert.Equal(["C"], Assert.Single(await fixture.NodesAsync()).Labels);
        Assert.Equal(V3, await new StoredModelVersionOf(fixture.Store).ValueAsync());
    }

    [Fact]
    public async Task RollsBackFailedChainOfStore()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:PureQLModelVersion {value: '1.0.0'}), (:A {id: 'a'})"
        );
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MigratedQueriesOf(
                fixture.Store,
                [
                    new RelabeledRecords(
                        V1,
                        V2,
                        new Dictionary<string, string> { ["A"] = "B" }
                    ),
                    new FailingMigration(V2, V3),
                ]
            ).ApplyAsync()
        );
        Assert.Equal(["A"], Assert.Single(await fixture.NodesAsync()).Labels);
        Assert.Equal("1.0.0", await fixture.MarkerAsync());
    }
}
