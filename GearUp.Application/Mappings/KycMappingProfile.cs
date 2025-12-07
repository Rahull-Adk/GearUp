using AutoMapper;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities;
namespace GearUp.Application.Mappings
{
    public class KycMappingProfile : Profile
    {
        public KycMappingProfile()
        {
            CreateMap<KycSubmissions, ToAdminKycResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.SubmittedBy.Name ?? string.Empty))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.SubmittedBy.Email ?? string.Empty))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.SubmittedBy.PhoneNumber ?? string.Empty))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.SubmittedBy.DateOfBirth))
                .ForMember(dest => dest.DocumentType, opt => opt.MapFrom(src => src.DocumentType))
                .ForMember(dest => dest.SelfieUrl, opt => opt.MapFrom(src => src.SelfieUrl))
                .ForMember(dest => dest.DocumentUrls, opt => opt.MapFrom(src => src.DocumentUrls))
                .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.SubmittedAt))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            CreateMap<KycSubmissions, KycUserResponseDto>()
                .ForMember(dest => dest.SubmittedBy, opt => opt.MapFrom(src => new UserDto
                {
                    Id = src.SubmittedBy.Id,
                    Username = src.SubmittedBy.Username,
                    Email = src.SubmittedBy.Email,
                    Role = src.SubmittedBy.Role.ToString(),
                    AvatarUrl = src.SubmittedBy.AvatarUrl
                }))
                   .ForMember(dest => dest.DocumentUrls, opt => opt.MapFrom(src => src.DocumentUrls))
                   .ForMember(dest => dest.SelfieUrl, opt => opt.MapFrom(src => src.SelfieUrl))
                   .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                   .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.SubmittedAt));
        }
    }

}
