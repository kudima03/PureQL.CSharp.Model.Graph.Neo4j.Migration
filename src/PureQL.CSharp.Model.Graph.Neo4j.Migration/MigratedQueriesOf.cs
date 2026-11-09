using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// The query graphs of a store migrated as far as a chain of migrations reaches: a
/// <see cref="MigratedQueries"/> run in one write transaction, so that a failed chain
/// leaves the store as it was.
/// </summary>
/// <param name="Store">The Neo4j database holding the query graphs.</param>
/// <param name="Migrations">
/// The migrations to choose from, in any order; at most one may start at each version.
/// </param>
public sealed record MigratedQueriesOf(
    INeo4jStoredQueries Store,
    IEnumerable<IGraphMigration> Migrations
)
{
    /// <summary>
    /// Opens a session on the store's driver against its database and applies the chain
    /// of migrations starting at the stored version, with the marker updates, in one
    /// write transaction.
    /// </summary>
    /// <param name="ct">A token to cancel the chain between statements.</param>
    /// <returns>A task that completes once the whole chain is committed.</returns>
    /// <exception cref="InvalidOperationException">
    /// Two migrations start at the same version, the chain from the stored version
    /// comes back to a version it passed, no model version marker is stored, or more
    /// than one is.
    /// </exception>
    public async Task ApplyAsync(CancellationToken ct = default)
    {
        await using IAsyncSession session = Store.Driver.AsyncSession(builder =>
            builder.WithDatabase(Store.Database)
        );
        await session.ExecuteWriteAsync(runner =>
            new MigratedQueries(runner, Migrations).ApplyAsync(ct)
        );
    }
}
