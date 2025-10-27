using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GearUp.Application.Interfaces.Services;


namespace GearUp.Infrastructure.Helpers
{
    public class CloudinaryImageUploader : ICloudinaryImageUploader
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryImageUploader(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
          
        }
        public async Task<Uri?> UploadImageAsync(MemoryStream imageStream, string path)
        {

            var uploadParams = new ImageUploadParams() {
                File = new FileDescription($"{Guid.NewGuid()}.jpeg", imageStream),
                Folder = path,
                UniqueFilename = true,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            var imageUrl = uploadResult.SecureUrl;

            return imageUrl;
            
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }

        public string ExtractPublicId(string cloudinaryUrl)
        {
            if (string.IsNullOrWhiteSpace(cloudinaryUrl))
                throw new ArgumentException("Invalid Cloudinary URL");

            var uri = new Uri(cloudinaryUrl);
            var path = uri.AbsolutePath;

            var match = System.Text.RegularExpressions.Regex.Match(path, @"/v\d+/(.+)");
            if (!match.Success)
                throw new InvalidOperationException("Could not extract public ID");

            var publicIdWithExt = match.Groups[1].Value;

            var publicId = System.IO.Path.ChangeExtension(publicIdWithExt, null);

            return publicId;
        }


    }
}
