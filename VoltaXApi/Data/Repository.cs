using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

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

        public async Task<IQueryable<TEntity>> GetAllAsync(GlobalParams objectParams)
        {
            var data = this.dbSet.AsQueryable();
            // List of parameters of M
            var props = typeof(TEntity).GetProperties();

            // Sorting
            if (!string.IsNullOrEmpty(objectParams.OrderBy))
            {
                Console.WriteLine(objectParams.OrderBy);
                foreach (var prop in props)
                {
                    if (prop.Name.ToLower() == objectParams.OrderBy.ToLower())
                    {
                        data = objectParams.ReverseOrder == "y" ? data.OrderBy(prop.Name + " descending") : data.OrderBy(prop.Name);
                    }
                }
            }

            // Searching for an occurence of a string Only string Objects 
            if (!string.IsNullOrEmpty(objectParams.SearchBy))
            {
                foreach (var prop in props)
                {
                    if (prop.Name == objectParams.SearchBy && !objectParams.SearchBy.Contains('.'))
                    {
                        string filterQuery1 = "(" + prop.Name + ".Contains(\"" + objectParams.SearchValue + "\"))";
                        data = data.Where(filterQuery1);
                    }
                    else if (objectParams.SearchBy.Contains('.'))
                    {
                        var navigation = objectParams.SearchBy.Split('.').First();
                        var navigationProp = objectParams.SearchBy.Split('.').Last();
                        data = data.Where(navigation + "." + navigationProp + ".Contains(\"" + objectParams.SearchValue + "\")");
                    }
                }
            }

            // Filtering
            if (objectParams.FilterBy != null)
            {

                string filterQuery1 = "";
                for (int i = 0; i < objectParams.FilterBy.Length; i++)
                {
                    foreach (var prop in props)
                    {
                        if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?) || prop.PropertyType == typeof(bool))
                        {
                            if (prop.Name == objectParams.FilterBy[i])
                            {
                                if (i == 0)
                                {
                                    filterQuery1 = prop.Name + " == \"" + objectParams.FilterValue[i] + "\"";
                                }
                                if (i != 0)
                                    filterQuery1 = filterQuery1 + " " + objectParams.FilterMethod + " " + prop.Name + " == \"" + objectParams.FilterValue[i] + "\"";
                                if (i == objectParams.FilterValue.Length - 1)
                                {
                                    data = data.Where("( " + filterQuery1 + " )");
                                }
                            }
                            else if (objectParams.FilterBy[i].Contains('.'))
                            {
                                var navigation = objectParams.FilterBy[i].Split('.').First();
                                var navigationProp = objectParams.FilterBy[i].Split('.').Last();
                                data = data.Where(navigation + ".Any(" + navigationProp + " == \"" + objectParams.FilterValue[i] + "\")");
                            }
                            else if (objectParams.FilterBy[i].Contains('-'))
                            {
                                var navigation = objectParams.FilterBy[i].Split('-').First();
                                var navigationProp = objectParams.FilterBy[i].Split('-').Last();
                                data = data.Where(navigation + "." + navigationProp + " == \"" + objectParams.FilterValue[i] + "\"");
                            }

                        }
                    }
                }


            }

            return data;
        }

        public async Task<TEntity> GetByIdAsync(int id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _context.Set<TEntity>().Where(predicate).ToListAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _context.Set<TEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRange(IEnumerable<TEntity> entities)
        {
            _context.Set<TEntity>().RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task Update(TEntity entity)
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

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await dbSet.CountAsync(predicate);
        }
    }
}

