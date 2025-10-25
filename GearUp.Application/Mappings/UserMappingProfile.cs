using AutoMapper;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.User;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, RegisterResponseDto>(); 
            CreateMap<User, UpdateUserResponseDto>();
        }
    }
}
