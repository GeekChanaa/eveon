
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.Models;
using OCPP.Core.Server;
using VoltaXApi.OCPP.Messages;
using System.Data.Entity;

namespace VoltaXApi.Services
{
    public class NoticeService : INoticeService
    {
        private readonly INoticeRepository _noticeRepository;
        private readonly IMailService _mailService;
        private readonly IUserRepository _userRepository;
        private readonly IFileManagementService _fileService;

        public NoticeService(
          INoticeRepository NoticeRepository,
          IMailService mailService,
          IFileManagementService fileService,
          IUserRepository userRepository
        ){
          _noticeRepository = NoticeRepository;
          _mailService = mailService;
          _fileService = fileService;
          _userRepository = userRepository;
        }

        public async Task CreateNotice(CreateNoticeDto createNoticeDto)
        {
            Notice notice = await _noticeRepository.CreateNotice(createNoticeDto);

            if(createNoticeDto.EmailTemplate != null){
              var emailTemplate = createNoticeDto.EmailTemplate;
              var newFileName = $"notice-{notice.ID}-{emailTemplate.FileName}";
              _fileService.UploadEmailTemplate(newFileName, "emailTemplates/notices", emailTemplate);
              var url = $"images/charging-stations/{newFileName}";
              notice.EmailTemplatePath = url;
              await _noticeRepository.Update(notice);
            }

            // Sending Email to the appropriate users
            await SendNoticeEmailForUsers(createNoticeDto);

        }

        private async Task SendNoticeEmailForUsers(CreateNoticeDto notice)
        {
          var toEmails = new List<string>();
          var iuser = _userRepository.GetAdminsQueryable()
                  .Where(u => u.IsEmailVerified)
                  .Select(u => u.Email)
                  .ToList();

          if (notice.ForAdmins)
              toEmails.AddRange( _userRepository.GetAdminsQueryable()
                  .Where(u => u.IsEmailVerified)
                  .Select(u => u.Email)
                  .ToList());

          if (notice.ForPartners)
              toEmails.AddRange( _userRepository.GetPartnersQueryable()
                  .Where(u => u.IsEmailVerified)
                  .Select(u => u.Email)
                  .ToList());

          if (notice.ForSupports)
              toEmails.AddRange( _userRepository.GetSupportsQueryable()
                  .Where(u => u.IsEmailVerified)
                  .Select(u => u.Email)
                  .ToList());

          if (notice.ForUsers)
              toEmails.AddRange( _userRepository.GetCustomersQueryable()
                  .Where(u => u.IsEmailVerified)
                  .Select(u => u.Email)
                  .ToList());

          MailRequest mailRequest = new()
          {
              Name = "VoltaX Notice",
              ToEmails = toEmails,
              Subject = notice.Title
          };

          await _mailService.SendNoticeEmail(mailRequest,notice.Text);
        }



  }
}