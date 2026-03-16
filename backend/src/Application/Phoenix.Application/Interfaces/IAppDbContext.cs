using Microsoft.EntityFrameworkCore;
using Phoenix.Domain.Entities;

namespace Phoenix.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Asset> Assets { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
