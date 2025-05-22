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
    public class NoticeRepository : Repository<Notice>, INoticeRepository
    {
        public NoticeRepository(VoltaXApiDbContext context) : base(context)
        {
        }

        public IQueryable<NoticeListDto> GetNotices(GlobalParams globalParams)
        {
            var partners = GetAllAsync(globalParams).Select(u => new NoticeListDto{
                ID = u.ID,
                Title = u.Title,
                Type = u.Type.ToString(),
                IsEmail = u.IsEmail,
                IsSms = u.IsSms,
                IsPushNotification = u.IsPushNotification,
                ForAdmins = u.ForAdmins,
                ForSupports = u.ForSupports,
                ForPartners = u.ForPartners,
                ForUsers = u.ForUsers
            });
            return partners;
        }

        public async Task<Notice> CreateNotice(CreateNoticeDto notice)
        {
            Notice createNotice = new Notice{
                Type = notice.Type,
                Title = notice.Title,
                IsEmail = notice.IsEmail,
                IsSms = notice.IsSms,
                IsPushNotification = notice.IsPushNotification,
                ForAdmins = notice.ForAdmins,
                ForSupports = notice.ForSupports,
                ForPartners = notice.ForPartners,
                ForUsers = notice.ForUsers,
                Text = notice.Text
            };

            await dbSet.AddAsync(createNotice);
            await _context.SaveChangesAsync();
            return createNotice;
        }

        public async Task<DisplayNoticeDto> GetNoticeByID(int id)
        {
            var notice1 = await _context.Notices.ToListAsync();
            var notice = await _context.Notices.Select(u => new DisplayNoticeDto{
                ID = u.ID,
                Title = u.Title,
                Type = u.Type.ToString(),
                IsEmail = u.IsEmail,
                IsSms = u.IsSms,
                IsPushNotification = u.IsPushNotification,
                ForAdmins = u.ForAdmins,
                ForSupports = u.ForSupports,
                ForPartners = u.ForPartners,
                ForUsers = u.ForUsers,
                Text = u.Text,  
                EmailTemplatePath = "dd",
            })
            .FirstOrDefaultAsync(u => u.ID == id);

            return notice;
        }
    }
    

}

