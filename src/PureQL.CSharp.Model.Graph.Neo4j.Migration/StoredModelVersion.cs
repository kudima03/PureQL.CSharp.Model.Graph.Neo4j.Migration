using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// The model version the stored query graphs were written with, read from the single
/// node labelled <c>PureQLModelVersion</c>.
/// </summary>
/// <param name="Runner">The session or transaction to read the marker on.</param>
public sealed record StoredModelVersion(IAsyncQueryRunner Runner)
{
    /// <summary>
    /// The label of the node holding the model version of the stored graphs.
    /// </summary>
    internal const string MarkerLabel = "PureQLModelVersion";

    /// <summary>The property of the marker node holding the version.</summary>
    internal const string ValueProperty = "value";

    private const string Statement =
        "MATCH (v:PureQLModelVersion) RETURN v.value AS value LIMIT 2";

    /// <summary>Reads the stored model version.</summary>
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
        ct.ThrowIfCancellationRequested();
        IResultCursor cursor = await Runner.RunAsync(Statement);
        List<IRecord> records = await cursor.ToListAsync(ct);
        return records.Count > 1
                ? throw new InvalidOperationException(
                    $"More than one node labelled {MarkerLabel} is stored."
                )
            : records.Count == 0 ? null
            : Version(records[0]);
    }

    private static ModelVersion Version(IRecord record)
    {
        return record[ValueProperty] is string value && !string.IsNullOrWhiteSpace(value)
            ? new ModelVersion(value)
            : throw new InvalidOperationException(
                $"The node labelled {MarkerLabel} holds no {ValueProperty}."
            );
    }
}
