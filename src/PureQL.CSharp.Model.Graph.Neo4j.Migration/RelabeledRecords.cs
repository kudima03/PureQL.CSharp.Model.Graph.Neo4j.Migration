using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// A migration renaming record labels: every node carrying an old label loses it and
/// gets the new one instead, as when a model version renames a record.
/// </summary>
/// <param name="From">
/// The model version the graphs this migration changes were written with.
/// </param>
/// <param name="To">The model version the graphs match once relabeled.</param>
/// <param name="Labels">The new label of each old one.</param>
/// <remarks>
/// <para>
/// All pairs are applied at once in one statement, so their order does not matter and
/// they may swap or chain labels (<c>A → B</c> with <c>B → C</c> moves <c>A</c> to
/// <c>B</c> and <c>B</c> to <c>C</c>): the nodes of every old label are collected first,
/// then every old label is removed, then every new one set. Other labels and nodes are
/// left as they are. The statement uses the dynamic labels of Neo4j 5.26 and later:
/// </para>
/// <code>
/// UNWIND $labels AS label
/// MATCH (n:$(label.old))
/// WITH collect([n, label.old, label.new]) AS rows
/// CALL (rows) {
///   UNWIND rows AS row
///   WITH row[0] AS n, row[1] AS old
///   REMOVE n:$(old)
/// }
/// UNWIND rows AS row
/// WITH row[0] AS n, row[2] AS new
/// SET n:$(new)
/// </code>
/// </remarks>
public sealed record RelabeledRecords(
    ModelVersion From,
    ModelVersion To,
    IReadOnlyDictionary<string, string> Labels
) : IGraphMigration
{
    private const string Statement = """
        UNWIND $labels AS label
        MATCH (n:$(label.old))
        WITH collect([n, label.old, label.new]) AS rows
        CALL (rows) {
          UNWIND rows AS row
          WITH row[0] AS n, row[1] AS old
          REMOVE n:$(old)
        }
        UNWIND rows AS row
        WITH row[0] AS n, row[2] AS new
        SET n:$(new)
        """;

    /// <summary>Relabels the stored nodes.</summary>
    /// <param name="runner">The session or transaction to run the statement on.</param>
    /// <param name="ct">A token to cancel the migration before it starts.</param>
    /// <returns>A task that completes once the nodes are relabeled.</returns>
    public async Task ApplyAsync(IAsyncQueryRunner runner, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ct.ThrowIfCancellationRequested();
        IResultCursor cursor = await runner.RunAsync(
            Statement,
            new Dictionary<string, object> { ["labels"] = new RenamePairs(Labels).Value }
        );
        _ = await cursor.ConsumeAsync();
    }
}
