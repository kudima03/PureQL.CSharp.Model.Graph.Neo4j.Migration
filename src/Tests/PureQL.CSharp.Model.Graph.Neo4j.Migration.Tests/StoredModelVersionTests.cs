using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

[Collection(Neo4jDatabase.Name)]
public sealed class StoredModelVersionTests(Neo4jFixture fixture)
{
    [Fact]
    public async Task IsNullWithoutMarker()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:Other {value: '1.0.0'})");
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        Assert.Null(await new StoredModelVersion(session).ValueAsync());
    }

    [Fact]
    public async Task ReadsMarker()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:PureQLModelVersion {value: '1.0.0'})");
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        Assert.Equal(
            new ModelVersion("1.0.0"),
            await new StoredModelVersion(session).ValueAsync()
        );
    }

    [Fact]
    public async Task ThrowsWithTwoMarkers()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(
            "CREATE (:PureQLModelVersion {value: '1.0.0'}), "
                + "(:PureQLModelVersion {value: '2.0.0'})"
        );
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new StoredModelVersion(session).ValueAsync()
        );
    }

    [Theory]
    [InlineData("CREATE (:PureQLModelVersion)")]
    [InlineData("CREATE (:PureQLModelVersion {value: ''})")]
    [InlineData("CREATE (:PureQLModelVersion {value: 1})")]
    public async Task ThrowsWithMarkerWithoutVersion(string statement)
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync(statement);
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new StoredModelVersion(session).ValueAsync()
        );
    }

    [Fact]
    public async Task ThrowsWhenCancelled()
    {
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new StoredModelVersion(session).ValueAsync(new CancellationToken(true))
        );
    }

    [Fact]
    public async Task ReadsMarkerOfStore()
    {
        await fixture.ClearAsync();
        _ = await fixture.RunAsync("CREATE (:PureQLModelVersion {value: '1.0.0'})");
        Assert.Equal(
            new ModelVersion("1.0.0"),
            await new StoredModelVersionOf(fixture.Store).ValueAsync()
        );
    }

    [Fact]
    public async Task IsNullWithoutMarkerOfStore()
    {
        await fixture.ClearAsync();
        Assert.Null(await new StoredModelVersionOf(fixture.Store).ValueAsync());
    }
}
