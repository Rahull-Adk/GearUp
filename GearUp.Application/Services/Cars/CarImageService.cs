using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Domain.Entities.Cars;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Cars
{
    public sealed class CarImageService : ICarImageService
    {
        private readonly ICloudinaryImageUploader _uploader;
        private readonly IDocumentProcessor _docProcessor;
        private readonly ICommonRepository _commonRepo;
        private readonly ICarRepository _carRepo;
        private readonly ILogger<CarImageService> _logger;

        public CarImageService(ICloudinaryImageUploader uploader, IDocumentProcessor docProcessor, ICommonRepository commonRepo, ICarRepository carRepo, ILogger<CarImageService> logger)
        {
            _uploader = uploader;
            _docProcessor = docProcessor;
            _commonRepo = commonRepo;
            _carRepo = carRepo;
            _logger = logger;
        }

        public async Task<Result<List<CarImage>>> ProcessForCreateAsync(ICollection<IFormFile> files, Guid dealerId, Guid carId)
        {
            try
            {
                var streams = await ConvertToStreamsAsync(files);
                var uploadPath = $"gearup/dealers/{dealerId}/cars";
                var uris = await _uploader.UploadImageListAsync(streams, uploadPath);
                var images = uris.Select(u => CarImage.CreateCarImage(carId, u.ToString())).ToList();
                return Result<List<CarImage>>.Success(images, "Images processed", 200);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process car images");
                return Result<List<CarImage>>.Failure("Failed to upload car images. Please try again.", 500);
            }
        }

        public async Task<Result<List<CarImage>>> ProcessForUpdateAsync(Car existingCar, ICollection<IFormFile>? files, Guid dealerId)
        {
            if (files == null || files.Count == 0)
                return Result<List<CarImage>>.Success(existingCar.Images.ToList(), "No new images", 200);


            try
            {
                foreach (var f in files)
                {
                    var ext = Path.GetExtension(f.FileName)?.ToLowerInvariant() ?? string.Empty;
                    var res = _docProcessor.ValidateFileType(f, ext);
                    if (!res.IsSuccess)
                        return Result<List<CarImage>>.Failure(res.ErrorMessage, res.Status);
                }
                foreach (var img in existingCar.Images)
                {
                    var publicId = _uploader.ExtractPublicId(img.Url);
                    if (!string.IsNullOrEmpty(publicId))
                        await _uploader.DeleteImageAsync(publicId);
                }
                _carRepo.RemoveCarImageByCarId(existingCar);
                await _commonRepo.SaveChangesAsync();

                var streams = await ConvertToStreamsAsync(files);
                var uploadPath = $"gearup/dealers/{dealerId}/cars";
                var uris = await _uploader.UploadImageListAsync(streams, uploadPath);
                var images = uris.Select(u => CarImage.CreateCarImage(existingCar.Id, u.ToString())).ToList();
                await _carRepo.AddCarImagesAsync(images);
                return Result<List<CarImage>>.Success(null!, "Images processed", 200);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update car images");
                return Result<List<CarImage>>.Failure("Failed to update car images. Please try again.", 500);
            }
        }

        private static async Task<List<MemoryStream>> ConvertToStreamsAsync(ICollection<IFormFile> files)
        {
            var result = new List<MemoryStream>();
            foreach (var file in files)
            {
                var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;
                result.Add(ms);
            }
            return result;
        }
    }
}
