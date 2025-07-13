namespace Playground;

#pragma warning disable

internal static class Program
{
    private static async Task Main()
    {
        InitializeDbContext();

        await using var ctx = new MyDbContext();

        // do your test stuff here
    }

    private static void InitializeDbContext()
    {
        const int entityCount = 100001;

        using var ctx = new MyDbContext();

        if (ctx.MyEntities.Any())
        {
            return;
        }

        for (var i = 0; i < entityCount; i++)
        {
            var entity = new MyEntity
            {
                Id = i,
                Value = $"Entity {i}"
            };
            ctx.MyEntities.Add(entity);
        }

        ctx.SaveChanges();
    }
}
