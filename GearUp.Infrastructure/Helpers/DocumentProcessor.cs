using System.Reflection.Metadata;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace GearUp.Infrastructure.Helpers
{
    public class DocumentProcessor : IDocumentProcessor
    {
        private const long MaxImageSize = 3 * 1024 * 1024;
        private const long MaxPdfSize = 5 * 1024 * 1024;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };

        public async Task<Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>> ProcessDocuments(List<IFormFile> documents, int targetWidth, int targetHeight)
        {

            if (documents == null || documents.Count == 0)
                return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure("No documents provided.", 400);

            var imageStreams = new List<MemoryStream>();
            var pdfStreams = new List<MemoryStream>();

            foreach (var document in documents)
            {
                var ext = Path.GetExtension(document.FileName)?.ToLower();
                var isValidResult = ValidateFileType(document, ext!);

                if (!isValidResult.IsSuccess)
                    return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure(isValidResult.ErrorMessage, isValidResult.Status);

                if (ext == ".pdf")
                {
                    var ms = new MemoryStream();
                    await document.CopyToAsync(ms);
                    ms.Position = 0;
                    pdfStreams.Add(ms);
                }
                else
                {
                    var imageResult = await ProcessImage(document, targetWidth, targetHeight);

                    if (!imageResult.IsSuccess)
                        return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure(imageResult.ErrorMessage, imageResult.Status);

                    imageStreams.Add(imageResult.Data);

                }
            }
            return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Success((imageStreams, pdfStreams), "All documents processed successfully", 200);

        }

        public async Task<Result<MemoryStream>> ProcessImage(
    IFormFile image, int targetWidth, int targetHeight, bool forceSquare = false)
        {
            if (image == null || image.Length == 0)
                return Result<MemoryStream>.Failure("Image file is empty.", 400);

            var ext = Path.GetExtension(image.FileName)?.ToLower();
            var validation = ValidateFileType(image, ext!);

            if (!validation.IsSuccess)
                return Result<MemoryStream>.Failure(validation.ErrorMessage, validation.Status);

            using var originalStream = image.OpenReadStream();
            using var imageFile = await Image.LoadAsync(originalStream);

            (int cropWidth, int cropHeight) = GetCropDimensions(imageFile.Width, imageFile.Height, targetWidth, targetHeight, forceSquare);
            int cropX = (imageFile.Width - cropWidth) / 2;
            int cropY = (imageFile.Height - cropHeight) / 2;

            using var processed = imageFile.Clone(ctx =>
            {
                ctx.AutoOrient();
                ctx.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight));
                ctx.Resize(targetWidth, targetHeight);
            });

            var output = new MemoryStream();
            await processed.SaveAsync(output, new JpegEncoder { Quality = 85 });
            output.Position = 0;

            if (output.Length > MaxImageSize)
                return Result<MemoryStream>.Failure($"Processed image exceeds {MaxImageSize / 1024 / 1024}MB limit.", 400);

            return Result<MemoryStream>.Success(output, "Image processed successfully.", 200);
        }

        public Result<bool> ValidateFileType(IFormFile file, string ext)
        {
            if (ext == ".pdf")
            {
                if (file.Length > MaxPdfSize)
                    return Result<bool>.Failure("PDF exceeds the 5MB limit.", 400);

                return Result<bool>.Success(true, "Valid PDF file.", 200);
            }

            if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
                return Result<bool>.Failure("Invalid image format. Only .jpg, .jpeg, and .png are allowed.", 400);

            if (file.Length > MaxImageSize)
                return Result<bool>.Failure("Image exceeds the 3MB limit.", 400);

            return Result<bool>.Success(true, "Valid image file.", 200);
        }

        private static (int width, int height) GetCropDimensions(
        int originalWidth, int originalHeight, int targetWidth, int targetHeight, bool forceSquare)
        {
            if (forceSquare)
            {
                int size = Math.Min(originalWidth, originalHeight);
                return (size, size);
            }

            double targetAspect = (double)targetWidth / targetHeight;
            int cropWidth = originalWidth;
            int cropHeight = originalHeight;

            if (originalWidth / (double)originalHeight > targetAspect)
                cropWidth = (int)(originalHeight * targetAspect);
            else
                cropHeight = (int)(originalWidth / targetAspect);

            return (cropWidth, cropHeight);
        }
    }
}

