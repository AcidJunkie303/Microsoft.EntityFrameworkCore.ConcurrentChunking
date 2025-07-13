using System.Globalization;

namespace Playground;

internal sealed class TestEntity
{
    public int Id { get; set; }
    public string? Value { get; set; }

    public override string ToString() => Id.ToString(CultureInfo.InvariantCulture);
}
