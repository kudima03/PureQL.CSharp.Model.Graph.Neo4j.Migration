using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using Testcontainers.Neo4j;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

/// <summary>One Neo4j Community container shared by the integration tests.</summary>
public sealed class Neo4jFixture : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder(
        "neo4j:5-community"
    ).Build();

    private IDriver? _driver;

    public IDriver Driver =>
        _driver ?? throw new InvalidOperationException("The container is not started.");

    public INeo4jStoredQueries Store => new Neo4jStoredQueries(Driver, "neo4j");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _driver = GraphDatabase.Driver(_container.GetConnectionString(), AuthTokens.None);
    }

    public async Task DisposeAsync()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
        }
        await _container.DisposeAsync();
    }

    public async Task<List<IRecord>> RunAsync(string statement, object? parameters = null)
    {
        await using IAsyncSession session = Driver.AsyncSession();
        IResultCursor cursor = await session.RunAsync(statement, parameters);
        return await cursor.ToListAsync();
    }

    public async Task ClearAsync()
    {
        _ = await RunAsync("MATCH (n) DETACH DELETE n");
    }

    public async Task<string?> MarkerAsync()
    {
        List<IRecord> records = await RunAsync(
            "MATCH (v:PureQLModelVersion) RETURN v.value AS value"
        );
        return Assert.Single(records)["value"].As<string>();
    }

    public async Task<List<StoredNode>> NodesAsync()
    {
        List<IRecord> records = await RunAsync(
            "MATCH (n) WHERE n.id IS NOT NULL "
                + "RETURN n.id AS id, labels(n) AS labels, properties(n) AS properties "
                + "ORDER BY id"
        );
        return
        [
            .. records.Select(record => new StoredNode(
                record["id"].As<string>(),
                [.. record["labels"].As<List<string>>().Order(StringComparer.Ordinal)],
                record["properties"].As<Dictionary<string, object>>()
            )),
        ];
    }
}
