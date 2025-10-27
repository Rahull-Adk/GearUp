
using GearUp.Application.Common;
using Microsoft.AspNetCore.Http;

namespace GearUp.Application.ServiceDtos
{
    public interface IImageProcessor
    {
        public Task<Result<MemoryStream>> ProcessImage(IFormFile image, int targetWidth, int targetHeight);
    }
}
