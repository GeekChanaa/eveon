
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.Models;
using AutoMapper;

namespace VoltaXApi.Services
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly IChargingStationRepository _repository;
        private readonly IRepository<Image> _imageRepo;
        private readonly IRepository<ChargingStationImage> _chargingStationImageRepo;
        private readonly IFileManagementService _fileService;
        private readonly IMapper _mapper;

        public ChargingStationService(
          IChargingStationRepository repo,
          IRepository<Image> imageRepository,
          IRepository<ChargingStationImage> chargingStationImageRepo,
          IFileManagementService fileService,
          IMapper mapper
        ){
          this._repository = repo;
          this._imageRepo = imageRepository;
          this._fileService = fileService;
          this._chargingStationImageRepo = chargingStationImageRepo;
          this._mapper = mapper;
        }
        public async Task<ChargingStation> CreateChargingStationWithDetails(ChargingStationCreateDto chargingStationDto)
        {
          ChargingStation chargingStation = _mapper.Map<ChargingStationCreateDto, ChargingStation>(chargingStationDto);
          chargingStation.Name = await GenerateStationName();
          foreach(var chargePoint in chargingStation.ChargePoints)
          {
            chargePoint.ChargePointId = await GenerateChargePointId();
          }
          await _repository.AddAsync(chargingStation);

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

        private async Task<string> GenerateStationName()
        {
            var latestStation = await _repository.GetLatestStationNumberAsync();
            int nextNumber = latestStation + 1;
            
            return $"VCS-{nextNumber:D4}"; 
        }

        private async Task<string> GenerateChargePointId()
        {
            var latestChargePointNumber = await _repository.GetLatestStationNumberAsync();
            int nextNumber = latestChargePointNumber + 1;
            
            return $"VOLTAX-{nextNumber:D3}"; 
        }
    }
}