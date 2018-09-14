using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetSgPhotos.Data.Repositories
{
    public class ApplicationRepository<T> : IRepository<T> where T : BaseEntity
    {
        private ApplicationDbContext dbContext;

        private DbSet<T> entities;

        public ApplicationRepository(ApplicationDbContext context)
        {
            this.dbContext = context;
            entities = dbContext.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            await dbContext.AddAsync(entity);
            await dbContext.SaveChangesAsync();
        }

        // *** For Cosmos DB only ***
        public async Task AddToCosmosDbAsync(Photo entity)
        {
            dbContext.Photos.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        public async Task AddMultipleAsync(IEnumerable<T> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }

            foreach (var entity in entities)
            {
                await dbContext.AddAsync(entity);
            }

            await dbContext.SaveChangesAsync();
        }

        public IQueryable<T> GetAll()
        {
            return entities.AsQueryable();
        }

        public async Task<T> GetAsync(long id)
        {
            return await entities.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            entity.UpdatedAt = DateTime.Now;

            await dbContext.SaveChangesAsync();
        }

        public async Task UpdateMultipleAsync(IEnumerable<T> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }

            foreach (var entity in entities)
            {
                entity.UpdatedAt = DateTime.Now;
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var selectedEntity = await GetAsync(id);

            if (selectedEntity == null)
            {
                throw new ArgumentNullException("entity");
            }

            entities.Remove(selectedEntity);
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteMultipleAsync(IEnumerable<T> deletingEntities)
        {
            if (deletingEntities == null)
            {
                throw new ArgumentNullException("entities");
            }

            foreach (var entity in deletingEntities)
            {
                entities.Remove(entity);
            }

            await dbContext.SaveChangesAsync();
        }

        public Task<T> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
