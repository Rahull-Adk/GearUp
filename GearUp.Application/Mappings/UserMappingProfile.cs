using AutoMapper;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, RegisterResponseDto>();
            CreateMap<User, UpdateUserResponseDto>();

            CreateMap<KycSubmissions, KycResponseDto>()
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
