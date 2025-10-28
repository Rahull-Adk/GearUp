using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Services
{
    public interface ICloudinaryImageUploader
    {
        public Task<List<Uri>> UploadImageListAsync(List<MemoryStream> imageStreams, string path);
        public Task DeleteImageAsync(string publicId);
        public string ExtractPublicId(string cloudinaryUrl);

    }
}
