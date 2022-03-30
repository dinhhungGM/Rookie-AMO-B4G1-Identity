using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Rookie.AMO.Identity.ViewModel.UserModels;
using Rookie.AMO.Identity.DataAccessor.Entities;

namespace Rookie.AMO.Identity.Business
{
    public class AutoMapperProfile : AutoMapper.Profile
    {
        public AutoMapperProfile()
        {
            FromDataAccessorLayer();
            FromPresentationLayer();
        }

        private void FromPresentationLayer()
        {
            CreateMap<UserRegistrationDto, User>(MemberList.Destination)
                .ForMember(u => u.FullName, x => x.MapFrom(u => $"{u.FirstName} {u.LastName}"));
        }

        private void FromDataAccessorLayer()
        {
            CreateMap<User, UserDto>(MemberList.Destination);
            CreateMap<IdentityRole, RoleDto>();
        }
    }
}
