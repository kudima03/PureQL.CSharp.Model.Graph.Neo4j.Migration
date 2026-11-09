namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

[CollectionDefinition(Name)]
public sealed class Neo4jDatabase : ICollectionFixture<Neo4jFixture>
{
    public const string Name = "Neo4j";
}
