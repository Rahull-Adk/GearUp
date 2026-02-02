using System.Security.Claims;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Enums;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/cars")]
    [ApiController]
    public class CarController : ControllerBase
    {
        private readonly ICarService _carService;
        public CarController(ICarService carService)
        {
            _carService = carService;
        }
        [Authorize(Policy = "DealerOnly")]
        [HttpPost("")]
        public async Task<IActionResult> CreateCar([FromForm] CreateCarRequestDto req)
        {
            var result = await _carService.CreateCarAsync(req, Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value));

            return StatusCode(result.Status, result.ToApiResponse());
        }


        [Authorize(Policy = "DealerOnly")]
        [HttpPut("{carId:guid}")]
        public async Task<IActionResult> UpdateCar([FromRoute] Guid carId, [FromForm] UpdateCarDto req)
        {
            var result = await _carService.UpdateCarAsync(carId, req, Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value));
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAllCars([FromQuery] string? cursor)
        {
            var result = await _carService.GetAllCarsAsync(cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchCars([FromQuery] CarSearchDto searchDto, [FromQuery] string? cursor)
        {
            var result = await _carService.SearchCarsAsync(searchDto, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [HttpGet("{carId:guid}")]
        public async Task<IActionResult> GetCarById([FromRoute] Guid carId)
        {
            var result = await _carService.GetCarByIdAsync(carId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpDelete("{carId:guid}")]
        public async Task<IActionResult> DeleteCarById([FromRoute] Guid carId)
        {
            var result = await _carService.DeleteCarByIdAsync(carId, Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value));
            return StatusCode(result.Status, result.ToApiResponse());
        }

        [Authorize(Policy = "DealerOnly")]
        [HttpGet($"my-car")]
        public async Task<IActionResult> GetMyCars([FromQuery] CarValidationStatus status, [FromQuery] string? cursor)
        {
            var currentUserId = User.FindFirst(c => c.Type == "id")?.Value;
            var result = await _carService.GetMyCarsAsync(Guid.Parse(currentUserId!), status, cursor);
            return  StatusCode(result.Status, result.ToApiResponse());
        }
    }
}
