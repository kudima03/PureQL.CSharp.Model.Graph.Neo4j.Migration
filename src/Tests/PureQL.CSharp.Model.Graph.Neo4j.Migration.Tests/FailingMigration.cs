using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

/// <summary>A migration that writes a node, then fails.</summary>
public sealed record FailingMigration(ModelVersion From, ModelVersion To)
    : IGraphMigration
{
    public async Task ApplyAsync(IAsyncQueryRunner runner, CancellationToken ct = default)
    {
        IResultCursor cursor = await runner.RunAsync("CREATE (:Written {id: 'written'})");
        _ = await cursor.ConsumeAsync();
        throw new InvalidOperationException("The migration failed.");
    }
}
