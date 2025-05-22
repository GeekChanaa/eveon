using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface INoticeRepository : IRepository<Notice>
    {
        IQueryable<NoticeListDto> GetNotices(GlobalParams globalParams);
        Task<Notice> CreateNotice(CreateNoticeDto notice);
        Task<DisplayNoticeDto> GetNoticeByID(int id);

    }
}