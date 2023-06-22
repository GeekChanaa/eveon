using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<List<CustomerNameDto>> GetAllCustomersNames();
        Task<List<CustomerNameDto>> GetAllCustomersNamesByName(string name);
        Task<Customer> GetCustomerByUserID(int UserID);
    }
}