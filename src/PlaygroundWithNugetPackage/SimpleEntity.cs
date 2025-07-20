using System.Globalization;

namespace PlaygroundWithNugetPackage;

public sealed record SimpleEntity(
    int Id,
    string Value1,
    string Value2,
    string Value3,
    string Value4,
    string Value5
)
{
    public override string ToString() => Id.ToString(CultureInfo.InvariantCulture);
}
