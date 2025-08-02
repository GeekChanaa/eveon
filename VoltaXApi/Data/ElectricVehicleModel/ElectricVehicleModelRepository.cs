using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using AutoMapper;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public class ElectricVehicleModelRepository : Repository<ElectricVehicleModel>, IElectricVehicleModelRepository
    {
        public ElectricVehicleModelRepository(VoltaXApiDbContext context) : base(context)
        {
        }

        public async  Task<List<EVModelForSelecttDto>> GetAllElectricVehicleModelsForSelect()
        {
            return await _context.ElectricVehicleModels.Select(u => new EVModelForSelecttDto
            {
                ID = u.ID,
                Make = u.Make,
                Model = u.Model,
            }).ToListAsync();
        }
    }
    

}

