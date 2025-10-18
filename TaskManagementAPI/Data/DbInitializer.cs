using Microsoft.EntityFrameworkCore;

namespace TaskManagementAPI.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.Migrate();
    }
}