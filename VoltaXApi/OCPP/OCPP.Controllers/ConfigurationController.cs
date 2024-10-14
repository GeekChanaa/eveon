using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using VoltaXApi.Services;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Exceptions;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    public class ConfigurationController : Controller
    {
        private readonly IConfigurationService _configService;

        public ConfigurationController(
            IConfigurationService configService
        ){
            _configService = configService;
        }


        [HttpPost("SetNetworkProfile/{chargePointID}")]
        public async Task<IActionResult> SetNetworkProfile(string chargePointID, SetNetworkProfileRequest request)
        {
            try
            {
                await this._configService.SetNetworkProfile(chargePointID,request);
                return Ok("Message Sent");
            }
            catch (WebSocketNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("ClearDisplayMessage/{chargePointID}")]
    public async Task<IActionResult> ClearDisplayMessage(string chargePointID, ClearDisplayMessageRequest request)
    {
        try
        {
            await _configService.ClearDisplayMessage(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("GetDisplayMessages/{chargePointID}")]
    public async Task<IActionResult> GetDisplayMessages(string chargePointID, GetDisplayMessagesRequest request)
    {
        try
        {
            await _configService.GetDisplayMessages(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("PublishFirmware/{chargePointID}")]
    public async Task<IActionResult> PublishFirmware(string chargePointID, PublishFirmwareRequest request)
    {
        try
        {
            await _configService.PublishFirmware(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("SetDisplayMessage/{chargePointID}")]
    public async Task<IActionResult> SetDisplayMessage(string chargePointID, SetDisplayMessageRequest request)
    {
        try
        {
            await _configService.SetDisplayMessage(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("UnpublishFirmware/{chargePointID}")]
    public async Task<IActionResult> UnpublishFirmware(string chargePointID, UnpublishFirmwareRequest request)
    {
        try
        {
            await _configService.UnpublishFirmware(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("UpdateFirmware/{chargePointID}")]
    public async Task<IActionResult> UpdateFirmware(string chargePointID, UpdateFirmwareRequest request)
    {
        try
        {
            await _configService.UpdateFirmware(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("Reset/{chargePointID}")]
    public async Task<IActionResult> Reset(string chargePointID, ResetRequest request)
    {
        try
        {
            await _configService.Reset(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("ChangeAvailability/{chargePointID}")]
    public async Task<IActionResult> ChangeAvailability(string chargePointID, ChangeAvailabilityRequest request)
    {
        try
        {
            await _configService.ChangeAvailability(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("TriggerMessage/{chargePointID}")]
    public async Task<IActionResult> TriggerMessage(string chargePointID, TriggerMessageRequest request)
    {
        try
        {
            await _configService.TriggerMessage(chargePointID, request);
            return Ok("Message Sent");
        }
        catch (WebSocketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    }
}
