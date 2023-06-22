using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;


namespace VoltaXApi.Data
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository   
    {
        public CustomerRepository(VoltaXApiDbContext context) : base(context)
        {

        }
        public async Task<List<CustomerNameDto>> GetAllCustomersNames()
        {
            return await _context.Customers.Select(u => new CustomerNameDto{ID = u.ID, Name = u.User.FirstName+ " "+ u.User.LastName}).ToListAsync();
        }

        // Search Customers By Names
        public async Task<List<CustomerNameDto>> GetAllCustomersNamesByName(string name)
        {
            return await _context.Customers
                .Where(u => u.User.FirstName.Contains(name) || u.User.LastName.Contains(name))
                .Select(u => new CustomerNameDto{ID = u.ID, Name = u.User.FirstName +" "+u.User.LastName})
                .ToListAsync();

        }

        // Get Customer By User ID
        public async Task<Customer> GetCustomerByUserID(int UserID)
        {
            return await _context.Customers.Where(u => UserID == u.UserID).FirstOrDefaultAsync();
        }

    } 
}