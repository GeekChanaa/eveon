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
using Newtonsoft.Json;

namespace VoltaXApi.OCPP.Controllers;

[ApiController]
[Route("api/logs")]
public class LogUploadController : ControllerBase
{
    private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedLogs");
    private readonly ILogger<LogUploadController> _logger;
    public LogUploadController(
        ILogger<LogUploadController> logger
    )
    {
        _logger = logger;
        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        _logger.LogInformation("LogUploadController : Upload Log File to cms start.");
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        string filePath = Path.Combine(_uploadFolder, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "File uploaded successfully", path = filePath });
    }
}
