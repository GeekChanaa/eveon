using VoltaXApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileManagementController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IChargingStationImageService _chargingStationImageService;

        public FileManagementController(IUserService userService, IChargingStationImageService chargingStationImageService)
        {
            _userService = userService;
            _chargingStationImageService = chargingStationImageService;
        }

        // Validation failures surface as 400 through the global exception filter.
        [HttpPost("UploadProfilePicture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile imageFile)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userID)) return Unauthorized();
            if (Request.Form.Files.Count != 1) return BadRequest("Upload exactly one image.");

            await _userService.UploadUserAvatar(Request.Form.Files[0], userID);
            return Ok();
        }

        [HttpPost("UploadChargingStationPicture")]
        public async Task<IActionResult> UploadChargingStationPicture(IFormFile imageFile, int chargingStationID)
        {
            if (Request.Form.Files.Count != 1) return BadRequest("Upload exactly one image.");

            await _chargingStationImageService.UploadChargingStationImages(new[] { Request.Form.Files[0] }, chargingStationID);
            return Ok();
        }
    }
}
