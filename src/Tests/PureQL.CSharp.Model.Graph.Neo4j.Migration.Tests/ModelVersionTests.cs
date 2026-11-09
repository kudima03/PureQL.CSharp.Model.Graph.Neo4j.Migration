namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

public sealed record ModelVersionTests
{
    [Fact]
    public void KeepsValue()
    {
        Assert.Equal(
            "0.1.0-preview.14.0.0",
            new ModelVersion("0.1.0-preview.14.0.0").Value
        );
    }

    [Fact]
    public void ThrowsOnNull()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new ModelVersion(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void ThrowsOnEmptyValue(string value)
    {
        _ = Assert.Throws<ArgumentException>(() => new ModelVersion(value));
    }

    [Fact]
    public void EqualsWithSameValue()
    {
        Assert.Equal(new ModelVersion("1.0.0"), new ModelVersion("1.0.0"));
    }

    [Fact]
    public void HashCodeEqualsWithSameValue()
    {
        Assert.Equal(
            new ModelVersion("1.0.0").GetHashCode(),
            new ModelVersion("1.0.0").GetHashCode()
        );
    }

    [Fact]
    public void DiffersWithOtherValue()
    {
        Assert.NotEqual(new ModelVersion("1.0.0"), new ModelVersion("1.0.1"));
    }

    [Fact]
    public void ComparesValuesOrdinally()
    {
        Assert.NotEqual(
            new ModelVersion("1.0.0-Preview"),
            new ModelVersion("1.0.0-preview")
        );
    }

    [Fact]
    public void ReplacesValueWithExpression()
    {
        Assert.Equal(
            new ModelVersion("2.0.0"),
            new ModelVersion("1.0.0") with
            {
                Value = "2.0.0",
            }
        );
    }

    [Fact]
    public void ThrowsOnNullValueWithExpression()
    {
        ModelVersion version = new ModelVersion("1.0.0");
        _ = Assert.Throws<ArgumentNullException>(() => version with { Value = null! });
    }

    [Fact]
    public void ThrowsOnEmptyValueWithExpression()
    {
        ModelVersion version = new ModelVersion("1.0.0");
        _ = Assert.Throws<ArgumentException>(() => version with { Value = "" });
    }

    [Fact]
    public void DeconstructsValue()
    {
        new ModelVersion("1.0.0").Deconstruct(out string value);
        Assert.Equal("1.0.0", value);
    }
}
