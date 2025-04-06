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
using AutoMapper;

namespace VoltaXApi.Data
{
    public class SystemReportCommentRepository : Repository<SystemReportComment>,ISystemReportCommentRepository
    {
        private readonly IMapper _mapper;
        public SystemReportCommentRepository(
            VoltaXApiDbContext context,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public IQueryable<SystemReportCommentDisplayDto> GetSystemReportComments(int systemReportID)
        {
          return  this._context.SystemReportComments.Where(sr => sr.SystemReportID == systemReportID)
                          .Select(sr => new SystemReportCommentDisplayDto{
                            ID = sr.ID,
                            Content = sr.Content,
                            Images = sr.SystemReportCommentImages.Select(u => u.Image.Url).ToList(),
                            UserName = sr.User.FullName
                          });
        }

    }
}

