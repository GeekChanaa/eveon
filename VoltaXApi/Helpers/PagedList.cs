using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Helpers
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public PagedList(IQueryable<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount= count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double) pageSize);
            Console.WriteLine("this is the items");
            Console.WriteLine(items);
            this.AddRange(items);
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count =  await source.CountAsync();
            if(pageSize > 0){
                var items = source.Skip((pageNumber - 1 ) * pageSize).Take(pageSize).AsQueryable<T>();
                return new PagedList<T>(items, count, pageNumber, pageSize);
            }
            else{
                var items = source.AsQueryable<T>();
                return new PagedList<T>(items, count, pageNumber, pageSize);
            }
            
        }

        public static PagedList<T> Create(IList<T> source, int pageNumber, int pageSize)
        {
            var count =  source.Count();
            if(pageSize > 0){
                var items = source.Skip((pageNumber - 1 ) * pageSize).Take(pageSize).AsQueryable<T>();
                return new PagedList<T>(items, count, pageNumber, pageSize);
            }
            else{
                var items = source.AsQueryable<T>();
                return new PagedList<T>(items, count, pageNumber, pageSize);
            }
            
        }

    }
}