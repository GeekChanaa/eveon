
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

namespace VoltaXApi.Services
{
    public class UserInfoDownloadRequestService : IUserInfoDownloadRequestService
    {
        private readonly IUserInfoDownloadRequestRepository _userInfoDownloadRequestRepository;
        private readonly IMailService _mailService;

        public UserInfoDownloadRequestService(
          IUserInfoDownloadRequestRepository userInfoDownloadRequestRepository,
          IMailService mailService
        ){
          _userInfoDownloadRequestRepository = userInfoDownloadRequestRepository;
          _mailService = mailService;
        }

        public async Task<bool> DenyRequest(int requestID)
        {
            var request = await _userInfoDownloadRequestRepository.GetRequestByID(requestID);
            await this._userInfoDownloadRequestRepository.DenyRequest(requestID);
            MailRequest mailRequest = new()
            {
                Name = "VoltaX Informations Download Request",
                ToEmails = new List<string> { request.Email},
                Subject = "Denied Request"
            };
            await _mailService.SendDownloadInfoRequestDenied(mailRequest, request.UserName);
            return true;
        }

        public async Task<bool> ApproveRequest(int requestID)
        {
            var request = await _userInfoDownloadRequestRepository.GetRequestByID(requestID);
            await this._userInfoDownloadRequestRepository.ApproveRequest(requestID);
            MailRequest mailRequest = new()
            {
                Name = "VoltaX Informations Download Request",
                ToEmails = new List<string> { request.Email},
                Subject = "Approved Request"
            };
            await _mailService.SendDownloadInfoRequestApproved(mailRequest, request.UserName);
            return true;
        }

  }
}