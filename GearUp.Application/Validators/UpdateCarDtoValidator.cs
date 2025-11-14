using FluentValidation;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Enums;

namespace GearUp.Application.Validators
{
    public class UpdateCarDtoValidator : AbstractValidator<UpdateCarDto>
    {
        public UpdateCarDtoValidator()
        {
            RuleFor(x => x).Must(dto =>
                !string.IsNullOrWhiteSpace(dto.Title) ||
                !string.IsNullOrWhiteSpace(dto.Description) ||
                !string.IsNullOrWhiteSpace(dto.Model) ||
                !string.IsNullOrWhiteSpace(dto.Make) ||
                dto.Year > 0 ||
                dto.Price > 0 ||
                !string.IsNullOrWhiteSpace(dto.Color) ||
                dto.Mileage > 0 ||
                dto.SeatingCapacity > 0 ||
                dto.EngineCapacity > 0 ||
                (dto.CarImages != null && dto.CarImages.Any(f => f?.Length > 0)) ||
                 (dto.FuelType != default) ||
                (dto.CarCondition != default) ||
                (dto.TransmissionType != default)
        ).WithMessage("At least one field must be provided for update.");

        }
    }
}
