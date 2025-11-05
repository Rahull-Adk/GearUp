using GearUp.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading.Tasks;

namespace GearUp.UnitTests.Infrastructure.Helpers
{
    public class DocumentProcessorTest
    {
        private readonly DocumentProcessor _documentProcessor;

        public DocumentProcessorTest()
        {
            _documentProcessor = new DocumentProcessor();
        }

        [Fact]
        public async Task ProcessDocuments_ShouldReturnSuccessResult_WhenAllDocumentsAreValid()
        {
            var formFiles = new List<IFormFile>();

            for (int i = 0; i < 2; i++)
            {
                var image = new Image<Rgba32>(300, 400);
                var ms = new MemoryStream();
                await image.SaveAsJpegAsync(ms);
                ms.Position = 0;
                var formFile = new FormFile(ms, 0, ms.Length, "image", $"test{i}.jpg");
                formFiles.Add(formFile);
            }

            var pdf = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("This is a test PDF document."));
            var pdfFormFile = new FormFile(pdf, 0, pdf.Length, "document", "test.pdf");
            formFiles.Add(pdfFormFile);

            var result = await _documentProcessor.ProcessDocuments(formFiles, 300, 300);
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);

        }

        [Fact]
        public async Task ProcessDocuments_ShouldReturnErrorResult_WhenAnyDocumentIsInvalid()
        {
            var formFiles = new List<IFormFile>();
            var image = new Image<Rgba32>(300, 400);

            var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);

            ms.Position = 0;
            var validFormFile = new FormFile(ms, 0, ms.Length, "image", "test.jpg");
            formFiles.Add(validFormFile);

            var invalidPdf = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("This is a test PDF document."));
            var invalidPdfFormFile = new FormFile(invalidPdf, 0, invalidPdf.Length + 15 * 1024 * 1024, "document", "test.pdf");

            formFiles.Add(invalidPdfFormFile);
            var result = await _documentProcessor.ProcessDocuments(formFiles, 300, 300);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);

        }

        [Fact]
        public async Task ProcessDocuments_ShouldReturnErrorResult_WhenNoDocumentsProvided()
        {
            var formFiles = new List<IFormFile>();
            var result = await _documentProcessor.ProcessDocuments(formFiles, 300, 300);
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
        }

        [Fact]
        public async Task ProcessDocuments_ShouldReturnErrorResult_WhenInvalidExtensionProvide()
        {
            var formFiles = new List<IFormFile>();
            var content = "This is a test document.";
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            ms.Position = 0;
            var formFile = new FormFile(ms, 0, ms.Length, "document", "test.txt");
            formFiles.Add(formFile);
            var result = await _documentProcessor.ProcessDocuments(formFiles, 300, 300);
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
        }

        [Fact]
        public async Task ProcessImage_ShouldReturnErrorResult_WhenImageIsEmpty()
        {
           var ms = new MemoryStream();
            var formFile = new FormFile(ms, 0, 0, "image", "test.jpg");
            var result = await _documentProcessor.ProcessImage(formFile, 100, 100);
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }


        [Fact]
        public async Task ProcessImage_ShouldReturnSuccessResult_WhenImageIsValid()
        {
            // Arrange
            var image = new Image<Rgba32>(300, 400);
            var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);
            ms.Position = 0;

            var formFile = new FormFile(ms, 0, ms.Length, "image", "test.jpg");

            // Act
            var result = await _documentProcessor.ProcessImage(formFile, 100, 100);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task ProcessImage_ShouldReturnErrorResult_WhenImageIsTooLarge()
        {
            // Arrange
            var image = new Image<Rgba32>(50, 50);
            var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);
            ms.Position = 0;

            var formFile = new FormFile(ms, 0, ms.Length + 5 * 1024 * 1024, "image", "test.jpg");

            // Act
            var result = await _documentProcessor.ProcessImage(formFile, 100, 100);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task ProcessImage_ShouldReturnErrorResult_WhenInvalidExtension()
        {
            // Arrange
            var content = "This is a test document.";
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            ms.Position = 0;
            var formFile = new FormFile(ms, 0, ms.Length, "document", "test.txt");
            // Act
            var result = await _documentProcessor.ProcessImage(formFile, 100, 100);
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }
    }   
}
