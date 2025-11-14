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
        public async Task<List<Uri>> UploadImageListAsync(List<MemoryStream> imageStreams, string path)
        {
            var imageUrls = new List<Uri>();
            foreach (var stream in imageStreams)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription($"{Guid.NewGuid()}.jpeg", stream),
                    Folder = path,
                    UniqueFilename = true,
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                var imageUrl = uploadResult.SecureUrl;
                imageUrls.Add(imageUrl);
            }

            return imageUrls;
        }

        public async Task<List<Uri>> UploadPdfAsync(List<MemoryStream> pdfStreams, string path)
        {
            var pdfUrl = new List<Uri>();
            foreach (var pdf in pdfStreams)
            {
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription($"{Guid.NewGuid()}.pdf", pdf),
                    Folder = path,
                    UniqueFilename = true,
                    Overwrite = true
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                pdfUrl.Add(uploadResult.SecureUrl);
            }
            return pdfUrl;
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
