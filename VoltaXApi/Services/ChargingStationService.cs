
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
    public class ChargingStationService : IChargingStationService
    {
        private readonly IChargingStationRepository _repository;
        private readonly IRepository<Image> _imageRepo;
        private readonly IRepository<ChargingStationImage> _chargingStationImageRepo;
        private readonly IFileManagementService _fileService;

        public ChargingStationService(
          IChargingStationRepository repo,
          IRepository<Image> imageRepository,
          IRepository<ChargingStationImage> chargingStationImageRepo,
          IFileManagementService fileService
        ){
          this._repository = repo;
          this._imageRepo = imageRepository;
          this._fileService = fileService;
          this._chargingStationImageRepo = chargingStationImageRepo;
        }
        public async Task<ChargingStation> CreateChargingStationWithDetails(ChargingStationCreateDto chargingStationDto)
        {
          ChargingStation chargingStation = await _repository.CreateChargingStation(chargingStationDto);

          if (chargingStationDto.ChargingStationImages != null )
          {
              for (int i = 0; i < chargingStationDto.ChargingStationImages.Count(); i++)
              {
                  var image = chargingStationDto.ChargingStationImages.ToList()[i];
                  var fileExtension = Path.GetExtension(image.FileName);
                  var newFileName = $"charging-station-{chargingStation.ID}-{image.FileName}";

                  _fileService.UploadImage(newFileName, "images/charging-stations", image);
                  // Save to database
                  var newImage = new Image
                  {
                      Url = $"images/charging-stations/{newFileName}",
                      UploadDate = DateTime.UtcNow,
                      Format = fileExtension,
                      Priority = ImagePriorityEnum.Principal,
                      IsActive = true,
                      AltText = "Charging Station Image "+chargingStation.ID,
                      Description = "none"
                  };

                  await this._imageRepo.AddAsync(newImage);
                  
                  var chargingStationImage = new ChargingStationImage
                  {
                      ChargingStationID = chargingStation.ID,
                      ImageID = newImage.ID
                  };
                  await this._chargingStationImageRepo.AddAsync(chargingStationImage);
              }
          }

          return chargingStation;
        }
    }
}