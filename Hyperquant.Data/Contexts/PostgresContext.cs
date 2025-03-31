#region

using Hyperquant.Models.Repository;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Hyperquant.Data.Contexts;

public class PostgresContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<FuturesDifference> FuturesUpdates => Set<FuturesDifference>();
}