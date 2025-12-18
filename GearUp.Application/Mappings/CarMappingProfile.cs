using AutoMapper;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;

namespace GearUp.Application.Mappings
{
    public class CarMappingProfile : Profile
    {
        public CarMappingProfile()
        {
            CreateMap<Car, CarResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Make, opt => opt.MapFrom(src => src.Make))
                .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model))
                .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.Year))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.Mileage, opt => opt.MapFrom(src => src.Mileage))
                .ForMember(dest => dest.SeatingCapacity, opt => opt.MapFrom(src => src.SeatingCapacity))
                .ForMember(dest => dest.EngineCapacity, opt => opt.MapFrom(src => src.EngineCapacity))
                .ForMember(dest => dest.VIN, opt => opt.MapFrom(src => src.VIN))
                .ForMember(dest => dest.CarImages, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.CarCondition, opt => opt.MapFrom(src => src.Condition))
                .ForMember(dest => dest.FuelType, opt => opt.MapFrom(src => src.FuelType))
                .ForMember(dest => dest.TransmissionType, opt => opt.MapFrom(src => src.Transmission))
                .ForMember(dest => dest.CarStatus, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CarValidationStatus, opt => opt.MapFrom(src => src.ValidationStatus))
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.LicensePlate));

            CreateMap<CarImage, CarImageDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url));
        }
    }
}
