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
        private string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

        public async Task<Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>> ProcessDocuments(List<IFormFile> documents, int targetWidth, int targetHeight)
        {
            allowedExtensions = [.. allowedExtensions, ".pdf"];
            var imageStreams = new List<MemoryStream>();
            var pdfStreams = new List<MemoryStream>();

            if (documents == null || documents.Count == 0)
                return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure("No documents provided.", 400);


            foreach (var document in documents)
            {
                if (document.Length == 0)
                    return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure("One of the document is empty.", 400);

                var ext = Path.GetExtension(document.FileName)?.ToLower();
                if (allowedExtensions.Contains(ext) && document.Length > MaxImageSize)
                    return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure("One of the image documents exceeds the maximum size of 3MB.", 400);

                if (ext == ".pdf" && document.Length > MaxPdfSize)
                    return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure("Pdf file exceeds the maximun size of 5MB.", 400);


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
                    if (imageResult.IsSuccess)
                    {
                        imageStreams.Add(imageResult.Data);

                    }
                    else
                    {
                        return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Failure(imageResult.ErrorMessage, imageResult.Status);
                    }
                }
            }
            return Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>.Success((imageStreams, pdfStreams), "All documents processed successfully", 200);

        }

        public async Task<Result<MemoryStream>> ProcessImage(IFormFile image, int targetWidth, int targetHeight, bool forceSquare = false)
        {
            if (image == null || image.Length == 0)
                return Result<MemoryStream>.Failure("Image file is empty.", 400);

            var ext = Path.GetExtension(image.FileName)?.ToLower();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                return Result<MemoryStream>.Failure("Invalid image format. Only .jpg, .jpeg, and .png are allowed.", 400);

            if (image.Length > MaxImageSize)
                return Result<MemoryStream>.Failure($"Image exceeds {MaxImageSize / 1024 / 1024}MB limit.", 400);

            using var originalStream = image.OpenReadStream();
            using var imageFile = await Image.LoadAsync(originalStream);

            int cropWidth = imageFile.Width;
            int cropHeight = imageFile.Height;

            if (forceSquare)
            {
                int size = Math.Min(imageFile.Width, imageFile.Height);
                int cropX = (imageFile.Width - size) / 2;
                int cropY = (imageFile.Height - size) / 2;
                cropWidth = size;
                cropHeight = size;
                cropX = (imageFile.Width - cropWidth) / 2;
                cropY = (imageFile.Height - cropHeight) / 2;
            }
            else
            {
                double targetAspectRatio = (double)targetWidth / targetHeight;
                if (imageFile.Width / (double)imageFile.Height > targetAspectRatio)
                    cropWidth = (int)(imageFile.Height * targetAspectRatio);
                else
                    cropHeight = (int)(imageFile.Width / targetAspectRatio);
            }

            int cropXCenter = (imageFile.Width - cropWidth) / 2;
            int cropYCenter = (imageFile.Height - cropHeight) / 2;

            using var processedImage = imageFile.Clone(ctx =>
            {
                ctx.AutoOrient();
                ctx.Crop(new Rectangle(cropXCenter, cropYCenter, cropWidth, cropHeight));
                ctx.Resize(targetWidth, targetHeight);
            });

            var ms = new MemoryStream();
            await processedImage.SaveAsync(ms, new JpegEncoder { Quality = 85 });
            ms.Position = 0;

            if (ms.Length > MaxImageSize)
                return Result<MemoryStream>.Failure($"Processed image exceeds {MaxImageSize / 1024 / 1024}MB limit.", 400);

            return Result<MemoryStream>.Success(ms, "Image processed successfully", 200);
        }
    }
}

