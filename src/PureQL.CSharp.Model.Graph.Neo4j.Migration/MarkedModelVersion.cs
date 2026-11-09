using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// The stored query graphs marked as written with a model version: the single node
/// labelled <c>PureQLModelVersion</c>, created if absent, holds the version in its
/// <c>value</c> property.
/// </summary>
/// <param name="Runner">The session or transaction to write the marker on.</param>
/// <param name="Version">The model version the stored graphs match.</param>
/// <remarks>
/// The statement is <c>MERGE (v:PureQLModelVersion) SET v.value = $value</c>: it updates
/// the marker in place and creates one only when none is stored.
/// </remarks>
public sealed record MarkedModelVersion(IAsyncQueryRunner Runner, ModelVersion Version)
{
    private const string Statement = "MERGE (v:PureQLModelVersion) SET v.value = $value";

    /// <summary>Writes the marker.</summary>
    /// <param name="ct">A token to cancel the write before it starts.</param>
    /// <returns>A task that completes once the marker is written.</returns>
    public async Task ApplyAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IResultCursor cursor = await Runner.RunAsync(
            Statement,
            new Dictionary<string, object>
            {
                [StoredModelVersion.ValueProperty] = Version.Value,
            }
        );
        _ = await cursor.ConsumeAsync();
    }
}
