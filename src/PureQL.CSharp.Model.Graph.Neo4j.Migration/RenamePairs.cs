namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// Old and new names as a statement parameter: a list of maps with the keys
/// <c>old</c> and <c>new</c>.
/// </summary>
/// <param name="Names">The new name of each old one.</param>
internal sealed record RenamePairs(IReadOnlyDictionary<string, string> Names)
{
    /// <summary>The parameter value, one map per pair.</summary>
    public List<object> Value =>
        [
            .. Names.Select(pair => new Dictionary<string, object>
            {
                ["old"] = pair.Key,
                ["new"] = pair.Value,
            }),
        ];
}
