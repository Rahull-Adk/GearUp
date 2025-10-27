using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Services
{
    public interface ICloudinaryImageUploader
    {
        public Task<Uri?> UploadImageAsync(MemoryStream imageStream, string path);
        public Task DeleteImageAsync(string publicId);
        public string ExtractPublicId(string cloudinaryUrl);

    }
}
