using System.Globalization;
using CloudinaryDotNet;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using SendGrid.Helpers.Mail;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class CarController : ControllerBase
    {
        private readonly ICarService _carService;
        public CarController(ICarService carService)
        {
            _carService = carService;
        }
        [Authorize(Policy = "DealerOnly")]
        [HttpPost("cars")]
        public async Task<IActionResult> CreateCar([FromForm] CreateCarRequestDto req)
        {
            var result = await _carService.CreateCarAsync(req, Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value));

            return StatusCode(result.Status, result.ToApiResponse());
        }


        [Authorize(Policy = "DealerOnly")]
        [HttpPut("cars/{carId:guid}")]
        public async Task<IActionResult> UpdateCar([FromRoute] Guid carId, [FromForm] UpdateCarDto req)
        {
            var result = await _carService.UpdateCarAsync(carId, req, Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value));
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("cars")]
        public async Task<IActionResult> GetAllCars([FromQuery] int pageNum)
        {
            var result = await _carService.GetAllCarsAsync(pageNum);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("cars/search")]
        public async Task<IActionResult> SearchCars([FromQuery] CarSearchDto searchDto)
        {
            var result = await _carService.SearchCarsAsync(searchDto);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("cars/{carId:guid}")]
        public async Task<IActionResult> GetCarById([FromRoute] Guid carId)
        {
            var result = await _carService.GetCarByIdAsync(carId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpDelete("cars/{carId:guid}")]
        public async Task<IActionResult> DeleteCarById([FromRoute] Guid carId)
        {
            var result = await _carService.DeleteCarByIdAsync(carId, Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value));
            return StatusCode(result.Status, result.ToApiResponse());
        }
    }
}
