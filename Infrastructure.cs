
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Moneki.Api.Data
{
    public class Tramite
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tramite> Tramites { get; set; } = null!;
    }
}

namespace Moneki.Api.Repositories
{
    using Moneki.Api.Data;
    using System.Linq;

    public class TramiteRepository
    {
        private readonly AppDbContext _db;

        public TramiteRepository(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<Tramite>> GetAllAsync()
        {
            return await _db.Tramites.AsNoTracking().ToListAsync().ConfigureAwait(false);
        }

        public async Task<Tramite?> GetByIdAsync(int id)
        {
            return await _db.Tramites.FindAsync(id).AsTask().ConfigureAwait(false);
        }

        public async Task<Tramite> AddAsync(Tramite entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var entry = await _db.Tramites.AddAsync(entity).ConfigureAwait(false);
            return entry.Entity;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}