using FluentValidation;
using GearUp.Application.ServiceDtos.Car;

namespace GearUp.Application.Validators
{
    public class CarRequestDtoValidator : AbstractValidator<CreateCarRequestDto>
    {
        public CarRequestDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.");


            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.Year)
                .InclusiveBetween(1886, DateTime.Now.Year + 1).WithMessage("Year must be between 1886 and next year.");

            RuleFor(x => x.Color)
                .NotEmpty().WithMessage("Color is required.");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("Model is required.");

            RuleFor(x => x.Make)
                .NotEmpty().WithMessage("Make is required.");
            RuleFor(x => x.CarImages)
                .NotNull().WithMessage("Car images are required.")
                .Must(images => images!.Any()).WithMessage("At least one car image must be provided.")
                .Must(images => images!.All(file => file.Length < 5 * 1024 * 1024))
                .WithMessage("Each car image must be less than 5MB in size.");


            RuleFor(x => x.Mileage)
                    .GreaterThanOrEqualTo(0).WithMessage("Mileage cannot be negative.");

            RuleFor(x => x.SeatingCapacity)
                .GreaterThan(0).WithMessage("Seating capacity must be greater than zero.");

            RuleFor(x => x.EngineCapacity)
                .GreaterThan(0).WithMessage("Engine capacity must be greater than zero.");

            RuleFor(x => x.VIN)
                .NotEmpty().WithMessage("VIN is required.")
                .Length(17).WithMessage("VIN must be exactly 17 characters long.");

            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("License plate is required.");

            RuleFor(x => x.FuelType)
                .IsInEnum().WithMessage("Invalid fuel type.");

            RuleFor(x => x.CarCondition)
                .IsInEnum().WithMessage("Invalid car condition.");

            RuleFor(x => x.TransmissionType)
                .IsInEnum().WithMessage("Invalid transmission type.");

            RuleFor(x => x.CarStatus)
                .IsInEnum().WithMessage("Invalid car status.");
        }
    }
}
