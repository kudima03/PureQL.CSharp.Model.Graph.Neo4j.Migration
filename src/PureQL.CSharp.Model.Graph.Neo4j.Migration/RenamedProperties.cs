using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// A migration renaming properties of the nodes with one label: the value of each old
/// property moves to the new name, as when a model version renames a record's
/// constructor parameter.
/// </summary>
/// <param name="From">
/// The model version the graphs this migration changes were written with.
/// </param>
/// <param name="To">
/// The model version the graphs match once the properties are renamed.
/// </param>
/// <param name="Label">The label of the nodes whose properties are renamed.</param>
/// <param name="Properties">The new name of each old property.</param>
/// <remarks>
/// <para>
/// A node without an old property is left as it is for that property; one with it
/// loses it and holds its value under the new name, replacing any value stored there.
/// All pairs are applied at once per node, so their order does not matter and they may
/// swap or chain names. Nodes without the label are left as they are. The statement
/// uses the dynamic labels and property keys of Neo4j 5.26 and later:
/// </para>
/// <code>
/// MATCH (n:$($label))
/// WITH n, [p IN $properties WHERE n[p.old] IS NOT NULL | [p.new, n[p.old]]] AS moved
/// WHERE size(moved) > 0
/// FOREACH (p IN $properties | SET n[p.old] = null)
/// FOREACH (m IN moved | SET n[m[0]] = m[1])
/// </code>
/// </remarks>
public sealed record RenamedProperties(
    ModelVersion From,
    ModelVersion To,
    string Label,
    IReadOnlyDictionary<string, string> Properties
) : IGraphMigration
{
    private const string Statement = """
        MATCH (n:$($label))
        WITH n, [p IN $properties WHERE n[p.old] IS NOT NULL | [p.new, n[p.old]]] AS moved
        WHERE size(moved) > 0
        FOREACH (p IN $properties | SET n[p.old] = null)
        FOREACH (m IN moved | SET n[m[0]] = m[1])
        """;

    /// <summary>
    /// Renames the properties of the stored nodes with <see cref="Label"/>.
    /// </summary>
    /// <param name="runner">The session or transaction to run the statement on.</param>
    /// <param name="ct">A token to cancel the migration before it starts.</param>
    /// <returns>A task that completes once the properties are renamed.</returns>
    public async Task ApplyAsync(IAsyncQueryRunner runner, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ct.ThrowIfCancellationRequested();
        IResultCursor cursor = await runner.RunAsync(
            Statement,
            new Dictionary<string, object>
            {
                ["label"] = Label,
                ["properties"] = new RenamePairs(Properties).Value,
            }
        );
        _ = await cursor.ConsumeAsync();
    }
}
