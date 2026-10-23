using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.Dtos;
using VoltaXApi.Services.PartnerPortal;

namespace VoltaXApi.Controllers.PartnerPortal
{
    /// <summary>
    /// The public information of the partner's own stations (what drivers see): read, edit, photos. Another partner's
    /// station answers 404. Changes are validated and audited; status, charge points and ownership are not editable here.
    /// </summary>
    [ApiController]
    [Route("api/partner-portal/stations")]
    [PartnerPortal]
    public sealed class PartnerStationsController : ControllerBase
    {
        private readonly PartnerStationService _stations;

        public PartnerStationsController(PartnerStationService stations)
        {
            _stations = stations;
        }

        private int PartnerId => this.PartnerId();

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Station(int id, CancellationToken ct) =>
            await _stations.StationAsync(PartnerId, id, ct) is { } station ? Ok(station) : NotFound();

        /// <summary>Replaces the station's public information: 409 when another station has the name.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePartnerStationRequest request, CancellationToken ct)
        {
            try
            {
                return await _stations.UpdateAsync(PartnerId, id, request, ct) is { } station ? Ok(station) : NotFound();
            }
            catch (StationNameTakenException ex)
            {
                return Conflict(new { error = ex.Message, code = "NameTaken" });
            }
        }

        /// <summary>Adds up to 5 photos (PNG, JPEG or WebP checked by content, 5 MB each); a station keeps at most 8.</summary>
        [HttpPost("{id:int}/photos")]
        [RequestSizeLimit(26 * 1024 * 1024)]
        public async Task<IActionResult> AddPhotos(int id, CancellationToken ct)
        {
            if (!Request.HasFormContentType)
                return BadRequest(new { error = "Upload the photos as multipart/form-data." });
            var form = await Request.ReadFormAsync(ct);
            return await _stations.AddPhotosAsync(PartnerId, id, form.Files.ToList(), ct) is { } station ? Ok(station) : NotFound();
        }

        [HttpDelete("{id:int}/photos/{photoId:int}")]
        public async Task<IActionResult> RemovePhoto(int id, int photoId, CancellationToken ct) =>
            await _stations.RemovePhotoAsync(PartnerId, id, photoId, ct) ? NoContent() : NotFound();
    }
}
