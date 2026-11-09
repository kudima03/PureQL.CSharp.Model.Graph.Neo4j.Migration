using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// The query graphs of a store marked as written with a model version: a
/// <see cref="MarkedModelVersion"/> written in its own session.
/// </summary>
/// <param name="Store">The Neo4j database holding the query graphs.</param>
/// <param name="Version">The model version the stored graphs match.</param>
public sealed record MarkedModelVersionOf(INeo4jStoredQueries Store, ModelVersion Version)
{
    /// <summary>
    /// Opens a session on the store's driver against its database and writes the marker
    /// in a write transaction.
    /// </summary>
    /// <param name="ct">A token to cancel the write before it starts.</param>
    /// <returns>A task that completes once the marker is committed.</returns>
    public async Task ApplyAsync(CancellationToken ct = default)
    {
        await using IAsyncSession session = Store.Driver.AsyncSession(builder =>
            builder.WithDatabase(Store.Database)
        );
        await session.ExecuteWriteAsync(runner =>
            new MarkedModelVersion(runner, Version).ApplyAsync(ct)
        );
    }
}
