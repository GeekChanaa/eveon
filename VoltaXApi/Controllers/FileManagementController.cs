using VoltaXApi.Services;
using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using VoltaXApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;

namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileManagementController : ControllerBase
    {
        private Microsoft.AspNetCore.Hosting.IHostingEnvironment Environment;

        private readonly IFileManagementService _fileManagementService;
        public FileManagementController(Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment, IFileManagementService FileManagementService)
        {
            Environment = _environment;
            _fileManagementService = FileManagementService;
        }

        [HttpPost("UploadProfilePicture")]
        public IActionResult UploadProfilePicture(IFormFile imageFile)
        {
            var userID = Request.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            try
            {
                if (Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    string folderName = "ProfilePictures/";
                    string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    fileName = userID +""+ fileName.Substring(fileName.LastIndexOf("."),fileName.Length - fileName.LastIndexOf("."));
                    this._fileManagementService.UploadFile(fileName, folderName, file);
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine( ex.Message);
                return BadRequest();
            }
        }


        [HttpPost("UploadChargingStationPicture")]
        public IActionResult UploadChargingStationPicture(IFormFile imageFile, int chargingStationID)
        {
            Console.WriteLine("this is getting in here b3da");
            try
            {
                if (Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    string folderName = "ChargingStationPictures/";
                    Console.WriteLine("Folder Name : "+folderName);
                    string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    Console.WriteLine("fileName : " + fileName);
                    fileName = chargingStationID +""+ fileName.Substring(fileName.LastIndexOf("."),fileName.Length - fileName.LastIndexOf("."));
                    Console.WriteLine("profile pictures 3");
                    this._fileManagementService.UploadFile(fileName, folderName, file);
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine( ex.Message);
                return BadRequest();
            }
        }

        
    }
    
}