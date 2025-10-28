using GearUp.Application.Common;
using Microsoft.AspNetCore.Http;

namespace GearUp.Application.Interfaces.Services
{
    public interface IDocumentProcessor
{
        public Task<Result<MemoryStream>> ProcessImage(IFormFile image, int targetWidth, int targetHeight, bool forcedSquare);
        public Task<Result<(List<MemoryStream> imageStreams, List<MemoryStream> pdfStreams)>> ProcessDocuments(List<IFormFile> documents, int targetWidth, int targetHeight);
    }
}
