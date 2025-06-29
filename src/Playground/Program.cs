using Microsoft.EntityFrameworkCore.ConcurrentChunking;

namespace Playground;

// TODO: remove
#pragma warning disable

internal static class Program
{
    private static async Task Main()
    {
        InitializeDbContext();

        await using var ctx = new MyDbContext();

        // act
        var query = ctx.MyEntities
                       .Where(a => a.Id % 100 >= 50)
                       .Where(a => a.Id > 0)
                       .OrderBy(a => a.Id);

        await foreach (var chunk in query.LoadChunkedAsync(() => new MyDbContext(), 40, 30))
        {
            Console.WriteLine($"Retrieved chunk with {chunk.Entities.Count} entities from {chunk.ChunkIndex}.");
        }

        /*
        var chunk0 = query.Skip(0).Take(1000).Test(()=> new MyDbContext());
        var chunk1 = query.Skip(1000).Take(1000).Test(()=> new MyDbContext());
        var chunk2 = query.Skip(2000).Take(1000).Test(()=> new MyDbContext());
        var chunk3 = query.Skip(3000).Take(1000).Test(()=> new MyDbContext());
*/
        // assert
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
