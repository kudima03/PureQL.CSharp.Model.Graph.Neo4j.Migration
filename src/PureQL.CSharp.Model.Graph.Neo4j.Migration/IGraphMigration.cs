using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// A change of the stored query graphs from one model version to the next: what a
/// new version of <c>PureQL.CSharp.Model</c> renamed or reshaped, applied to the graphs
/// written with the previous one.
/// </summary>
/// <remarks>
/// <see cref="MigratedQueries"/> chains migrations by their versions: it applies the
/// migration whose <see cref="From"/> is the stored version, marks the graphs with
/// its <see cref="To"/>, and continues from there.
/// </remarks>
public interface IGraphMigration
{
    /// <summary>
    /// The model version the graphs this migration changes were written with.
    /// </summary>
    public ModelVersion From { get; }

    /// <summary>
    /// The model version the graphs match once this migration is applied.
    /// </summary>
    public ModelVersion To { get; }

    /// <summary>
    /// Changes the stored graphs from <see cref="From"/> to <see cref="To"/>.
    /// </summary>
    /// <param name="runner">
    /// The session or transaction to run the statements on; the version marker is left
    /// to the caller.
    /// </param>
    /// <param name="ct">A token to cancel the migration between statements.</param>
    /// <returns>A task that completes once every statement has run.</returns>
    public Task ApplyAsync(IAsyncQueryRunner runner, CancellationToken ct = default);
}
