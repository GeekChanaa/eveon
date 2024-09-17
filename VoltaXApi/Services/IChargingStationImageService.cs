
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
namespace VoltaXApi.Services
{
    public interface IChargingStationImageService
    {
      Task UploadChargingStationImages(IEnumerable<IFormFile> chargingStationImages, int chargingStationID);
    }
}