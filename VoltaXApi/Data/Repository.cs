using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using LinqKit;

namespace VoltaXApi.Data
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
    {
        protected readonly VoltaXApiDbContext _context;
        protected readonly DbSet<TEntity> dbSet;

        public Repository(VoltaXApiDbContext context)
        {
            _context = context;
            this.dbSet = _context.Set<TEntity>();
        }

        public virtual IQueryable<TEntity> GetAllAsync(GlobalParams objectParams)
        {
            // Field names are resolved against TEntity's real properties and values are bound as
            // parameters; unknown or malformed fields are rejected with a 400.
            return SafeDynamicQuery.Apply(this.dbSet.AsQueryable(), objectParams);
        }

        public virtual async Task<TEntity> GetByIdAsync(int id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }

        public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _context.Set<TEntity>().Where(predicate).ToListAsync();
        }

        public virtual async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _context.Set<TEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public virtual async Task Remove(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task RemoveByID(int ID)
        {
            var entity = await this.GetByIdAsync(ID);
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task RemoveRange(IEnumerable<TEntity> entities)
        {
            _context.Set<TEntity>().RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public virtual async Task Update(TEntity entity)
        {
            _context.Set<TEntity>().Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }


            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate)
        {
            if(predicate == null)
                return await dbSet.CountAsync();
            return await dbSet.CountAsync(predicate);
        }
    }
}

