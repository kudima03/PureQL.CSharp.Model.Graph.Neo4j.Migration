namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

/// <summary>
/// A node read back from the database: its id, sorted labels and properties.
/// </summary>
public sealed record StoredNode(
    string Id,
    IReadOnlyList<string> Labels,
    IReadOnlyDictionary<string, object> Properties
);
