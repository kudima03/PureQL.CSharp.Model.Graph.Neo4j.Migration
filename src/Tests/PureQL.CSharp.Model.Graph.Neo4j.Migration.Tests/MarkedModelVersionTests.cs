using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Migration.Tests;

[Collection(Neo4jDatabase.Name)]
public sealed class MarkedModelVersionTests(Neo4jFixture fixture)
{
    [Fact]
    public async Task CreatesMarker()
    {
        await fixture.ClearAsync();
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new MarkedModelVersion(session, new ModelVersion("1.0.0")).ApplyAsync();
        Assert.Equal("1.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task UpdatesTheSingleMarker()
    {
        await fixture.ClearAsync();
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new MarkedModelVersion(session, new ModelVersion("1.0.0")).ApplyAsync();
        await new MarkedModelVersion(session, new ModelVersion("2.0.0")).ApplyAsync();
        Assert.Equal("2.0.0", await fixture.MarkerAsync());
    }

    [Fact]
    public async Task RoundTrips()
    {
        await fixture.ClearAsync();
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        await new MarkedModelVersion(
            session,
            new ModelVersion("0.1.0-preview.14.0.0")
        ).ApplyAsync();
        Assert.Equal(
            new ModelVersion("0.1.0-preview.14.0.0"),
            await new StoredModelVersion(session).ValueAsync()
        );
    }

    [Fact]
    public async Task ThrowsWhenCancelled()
    {
        await fixture.ClearAsync();
        await using IAsyncSession session = fixture.Driver.AsyncSession();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new MarkedModelVersion(session, new ModelVersion("1.0.0")).ApplyAsync(
                new CancellationToken(true)
            )
        );
        Assert.Null(await new StoredModelVersion(session).ValueAsync());
    }

    [Fact]
    public async Task RoundTripsOfStore()
    {
        await fixture.ClearAsync();
        await new MarkedModelVersionOf(
            fixture.Store,
            new ModelVersion("1.0.0")
        ).ApplyAsync();
        await new MarkedModelVersionOf(
            fixture.Store,
            new ModelVersion("2.0.0")
        ).ApplyAsync();
        Assert.Equal(
            new ModelVersion("2.0.0"),
            await new StoredModelVersionOf(fixture.Store).ValueAsync()
        );
        Assert.Equal("2.0.0", await fixture.MarkerAsync());
    }
}
