using GearUp.Application.Common;
using GearUp.Application.ServiceDtos;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace GearUp.Infrastructure.Helpers
{
    public class ImageProcessor : IImageProcessor
    {
        public async Task<Result<MemoryStream>> ProcessImage(IFormFile image, int targetWidth, int targetHeight)
        {

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(image.FileName.ToLower());

            if (!allowedExtensions.Contains(fileExtension))
            {
                return Result<MemoryStream>.Failure("Invalid image format. Only .jpg, .jpeg, and .png are allowed.", 400);
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                return Result<MemoryStream>.Failure("Image size exceeds the maximum limit of 5MB.", 400);
            }

            if (image.OpenReadStream().Length == 0)
            {
                return Result<MemoryStream>.Failure("Image file is empty.", 400);
            }

            using var originalStream = image.OpenReadStream();
            using var imageFile = Image.Load(originalStream);

            double targetAspectRatio = (double)targetWidth / targetHeight;

            int cropWith = imageFile.Width;
            int cropHeight = imageFile.Height;

            if(imageFile.Width / (double)imageFile.Height > targetAspectRatio)
            {
                cropWith = (int)(imageFile.Height * targetAspectRatio);
            }
            else
            {
                cropHeight = (int)(imageFile.Width / targetAspectRatio);
            }

            int cropX = (imageFile.Width - cropWith) / 2;
            int cropY = (imageFile.Height - cropHeight) / 2;

            using var processedImage = imageFile.Clone(ctx => { 
                ctx.Crop(new Rectangle(cropX, cropY, cropWith, cropHeight));
                ctx.Resize(targetWidth, targetHeight);
            });
            
            var ms = new MemoryStream();
            await processedImage.SaveAsync(ms, new JpegEncoder { Quality = 85 });
            ms.Position = 0;

            return ms.Length > 0 ? Result<MemoryStream>.Success(ms, "Image processed successfully", 200) : Result<MemoryStream>.Failure("Image size exceeds the maximum limit of 5MB.", 400);

        }
    }
}
