namespace PureQL.CSharp.Model.Graph.Neo4j.Migration;

/// <summary>
/// A version of <c>PureQL.CSharp.Model</c>: the one stored query graphs were written
/// with, such as <c>0.1.0-preview.14.0.0</c>.
/// </summary>
/// <param name="Value">The version, a non-empty string compared ordinally.</param>
/// <exception cref="ArgumentNullException"><paramref name="Value"/> is null.</exception>
/// <exception cref="ArgumentException">
/// <paramref name="Value"/> is empty or consists only of white space.
/// </exception>
public sealed record ModelVersion(string Value)
{
    /// <summary>
    /// The version, a non-empty string compared ordinally, such as
    /// <c>0.1.0-preview.14.0.0</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value set is null.</exception>
    /// <exception cref="ArgumentException">
    /// The value set is empty or consists only of white space.
    /// </exception>
    public string Value
    {
        get;
        init => field = Valid(value);
    } = Valid(Value);

    private static string Valid(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
