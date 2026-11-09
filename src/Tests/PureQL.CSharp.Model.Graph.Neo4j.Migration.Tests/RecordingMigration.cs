using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

/// <summary>A migration that only records that it was applied.</summary>
public sealed record RecordingMigration(
    ModelVersion From,
    ModelVersion To,
    List<string> Applied
) : IGraphMigration
{
    public Task ApplyAsync(IAsyncQueryRunner runner, CancellationToken ct = default)
    {
        Applied.Add($"{From.Value}->{To.Value}");
        return Task.CompletedTask;
    }
}
