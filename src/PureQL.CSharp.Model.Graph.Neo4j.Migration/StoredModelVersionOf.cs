using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// The model version the query graphs of a store were written with: a
/// <see cref="StoredModelVersion"/> read in its own session.
/// </summary>
/// <param name="Store">The Neo4j database holding the query graphs.</param>
public sealed record StoredModelVersionOf(INeo4jStoredQueries Store)
{
    /// <summary>
    /// Opens a session on the store's driver against its database and reads the stored
    /// model version in a read transaction.
    /// </summary>
    /// <param name="ct">
    /// A token to cancel the read before it starts or while it reads.
    /// </param>
    /// <returns>
    /// The version of the marker node, or <see langword="null"/> when no marker is
    /// stored.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// More than one marker is stored, or the marker holds no version.
    /// </exception>
    public async Task<ModelVersion?> ValueAsync(CancellationToken ct = default)
    {
        await using IAsyncSession session = Store.Driver.AsyncSession(builder =>
            builder.WithDatabase(Store.Database)
        );
        return await session.ExecuteReadAsync(runner =>
            new StoredModelVersion(runner).ValueAsync(ct)
        );
    }
}
