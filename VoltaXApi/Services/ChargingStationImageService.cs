
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class ChargingStationImageService : IChargingStationImageService
    {
        private readonly IChargingStationImageRepository _repository;
        private readonly IRepository<Image> _imageRepo;
        private readonly IRepository<ChargingStationImage> _chargingStationImageRepo;
        private readonly IFileManagementService _fileService;

        public ChargingStationImageService(
          IChargingStationImageRepository repo,
          IRepository<Image> imageRepository,
          IRepository<ChargingStationImage> chargingStationImageRepo,
          IFileManagementService fileService
        ){
          this._repository = repo;
          this._imageRepo = imageRepository;
          this._fileService = fileService;
          this._chargingStationImageRepo = chargingStationImageRepo;
        }
        public async Task UploadChargingStationImages(IEnumerable<IFormFile> chargingStationImages, int chargingStationID )
        {

          if (chargingStationImages != null )
          {
              for (int i = 0; i < chargingStationImages.Count(); i++)
              {
                  var image = chargingStationImages.ToList()[i];
                  var fileExtension = Path.GetExtension(image.FileName);
                  var newFileName = $"charging-station-{chargingStationID}-{image.FileName}";

                  _fileService.UploadImage(newFileName, "images/charging-stations", image);
                  // Save to database
                  var newImage = new Image
                  {
                      Url = $"images/charging-stations/{newFileName}",
                      UploadDate = DateTime.UtcNow,
                      Format = fileExtension,
                      Priority = ImagePriorityEnum.Principal,
                      IsActive = true,
                      AltText = "Charging Station Image "+chargingStationID,
                      Description = "none"
                  };

                  await this._imageRepo.AddAsync(newImage);
                  
                  var chargingStationImage = new ChargingStationImage
                  {
                      ChargingStationID = chargingStationID,
                      ImageID = newImage.ID
                  };
                  await this._chargingStationImageRepo.AddAsync(chargingStationImage);
              }
          }
        }
    }
}