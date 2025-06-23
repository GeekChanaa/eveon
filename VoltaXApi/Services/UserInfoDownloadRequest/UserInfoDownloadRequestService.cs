
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
using VoltaXApi.Factories;

namespace VoltaXApi.Services
{
  public class UserInfoDownloadRequestService : IUserInfoDownloadRequestService
  {
    private readonly IUserInfoDownloadRequestRepository _userInfoDownloadRequestRepository;
    private readonly IMailRequestFactory _mailRequestFactory;
    private readonly IMailService _mailService;

    public UserInfoDownloadRequestService(
      IUserInfoDownloadRequestRepository userInfoDownloadRequestRepository,
      IMailService mailService,
      IMailRequestFactory mailRequestFactory
    )
    {
      _userInfoDownloadRequestRepository = userInfoDownloadRequestRepository;
      _mailService = mailService;
      _mailRequestFactory = mailRequestFactory;
        }

    public async Task<bool> DenyRequest(int requestID)
    {
      var request = await _userInfoDownloadRequestRepository.GetRequestByID(requestID);
      await this._userInfoDownloadRequestRepository.DenyRequest(requestID);
      MailRequest mailRequest = this._mailRequestFactory.CreateDeniedDownloadInfoRequest(request.Email);

      await _mailService.SendDownloadInfoRequestDenied(mailRequest, request.UserName);
      return true;
    }

    public async Task<bool> ApproveRequest(int requestID)
    {
      var request = await _userInfoDownloadRequestRepository.GetRequestByID(requestID);
      await this._userInfoDownloadRequestRepository.ApproveRequest(requestID);
      MailRequest mailRequest = this._mailRequestFactory.CreateApprovedDownloadInfoRequest(request.Email);

      await _mailService.SendDownloadInfoRequestApproved(mailRequest, request.UserName);
      return true;
    }

  }
}