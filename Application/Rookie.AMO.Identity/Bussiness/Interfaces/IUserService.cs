using Rookie.AMO.Identity.Business;
using Rookie.AMO.Identity.ViewModel.UserModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rookie.AMO.Identity.Bussiness.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto> GetUserById(string id);
        Task<UserDto> CreateUserAsync(UserRegistrationDto request, string adminLocation);
        Task DisableUserById(Guid id);
        Task<PagedResponseModel<UserDto>> PagedQueryAsync(string? name, int page,string state, int limit=5);
    }
}
