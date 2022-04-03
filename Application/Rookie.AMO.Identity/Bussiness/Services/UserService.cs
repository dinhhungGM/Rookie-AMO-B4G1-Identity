using AutoMapper;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rookie.AMO.Identity.Business;
using Rookie.AMO.Identity.Bussiness.Interfaces;
using Rookie.AMO.Identity.Contants;
using Rookie.AMO.Identity.DataAccessor.Entities;
using Rookie.AMO.Identity.ViewModel.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PasswordGenerator;
using Microsoft.AspNetCore.Identity.UI.Services;
using EnsureThat;

namespace Rookie.AMO.Identity.Bussiness.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IEmailSender _emailSender;

        public UserService(UserManager<User> userManager, IMapper mapper, IEmailSender emailSender)
        {
            _userManager = userManager;
            _mapper = mapper;
            _emailSender = emailSender;
        }
        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var user = _userManager.Users;
            return _mapper.Map<IEnumerable<UserDto>>(await user.ToListAsync());
        }
        public async Task<UserDto> GetUserById(string id)
        {
            var user = _userManager.FindByIdAsync(id);
            return _mapper.Map<UserDto>(await user);
        }
        public async Task<UserDto> CreateUserAsync(UserRegistrationDto request, string adminLocation)
        {
            var user = _mapper.Map<User>(request);
            user.JoinedDate = DateTime.UtcNow;
            user.Location = adminLocation;
            user.UserName = GenerateUserName(user.FirstName, user.LastName);

            if (user.Type != "Admin")
            {
                user.CodeStaff = GenerateStaffCode();
            }
            user.EmailConfirmed = true;
            var passwordGenerator = new Password(includeLowercase: true, includeUppercase: true, includeNumeric: true, includeSpecial: true, passwordLength: 8);
            var randomPassword = passwordGenerator.Next();

            var createUserResult = await _userManager.CreateAsync(user, randomPassword);

            if (!createUserResult.Succeeded)
            {
                throw new Exception(createUserResult.Errors.First().Description);
            }

            var claims = new List<Claim>()
            {
                new Claim(IdentityModel.JwtClaimTypes.GivenName, user.FirstName),
                new Claim(IdentityModel.JwtClaimTypes.FamilyName, user.LastName),
                new Claim(IdentityModel.JwtClaimTypes.Role, user.Type),
                new Claim("location", adminLocation)
            };

            var addClaimsResult = await _userManager.AddClaimsAsync(user, claims);

            if (!addClaimsResult.Succeeded)
            {
                throw new Exception("Unexpected errors! Add claims operation is not success.");
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, request.Type);

            if (!addRoleResult.Succeeded)
            {
                throw new Exception("Unexpected errors! Add role operation is not success.");
            }

            //----------Send password-------------------------
            await _emailSender.SendEmailAsync(
                request.Email,
                "Rookies - AMO",
                $"<h1>Rookie - AMO</h1><p>Your username is: {user.UserName},Use this password for your first login: {randomPassword}</p>"
            );
            //------------------------------------------------

            return _mapper.Map<UserDto>(user);
        }

        private string GenerateStaffCode()
        {
            var staffCode = new StringBuilder();
            var userList = _userManager.Users
                                    .OrderByDescending(x => Convert.ToInt32(
                                        x.CodeStaff.Replace(UserContants.PrefixStaffCode, "")
                                     ));
            if (!userList.Any())
            {
                return UserContants.PrefixStaffCode + "0001";
            }

            var latestCode = userList.First().CodeStaff;
            var nextNumber = Convert.ToInt32(latestCode.Replace(UserContants.PrefixStaffCode, "")) + 1;
            staffCode.Append(UserContants.PrefixStaffCode);
            staffCode.Append(nextNumber.ToString("0000"));
            return staffCode.ToString();
        }
        private string GenerateUserName(string firstName, string lastName)
        {
            firstName = firstName.Trim().ToLower();
            firstName = Regex.Replace(firstName, @"\s", "");

            lastName = lastName.Trim().ToLower();

            var userNameLogin = new StringBuilder(firstName);
            var words = lastName.Split(" ");

            foreach (var word in words)
            {
                userNameLogin.Append(word[0]);
            }

            var theSameUsernameLoginList = _userManager.Users
                .Where(x => x.UserName.StartsWith(userNameLogin.ToString())
                );

            if (!theSameUsernameLoginList.Any())
            {
                return userNameLogin.ToString();
            }


            List<string> loginListWithUserHaveTheSameUserName = new List<string>();

            foreach (var user in theSameUsernameLoginList)
            {
                if (user.UserName == userNameLogin.ToString())
                {
                    loginListWithUserHaveTheSameUserName.Add(user.UserName);
                    continue;
                }

                // Neu ma la binhnv1
                bool res = int.TryParse(user.UserName.Split(userNameLogin.ToString()).Last(), out int _);
                if (!res)
                {
                    loginListWithUserHaveTheSameUserName.Add(user.UserName);
                }
            }

            int loginListWithUserHaveTheSameUserNameLength = loginListWithUserHaveTheSameUserName.Count();

            if (loginListWithUserHaveTheSameUserNameLength == 0)
            {
                return userNameLogin.ToString();
            }

            userNameLogin.Append(loginListWithUserHaveTheSameUserNameLength.ToString());
            return userNameLogin.ToString();
        }
        public async Task DisableUserById(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                throw new Exception("User not found!");
            }

            user.Disable = true;
            await _userManager.UpdateAsync(user);
        }
        public async Task UpdateUserAsync(Guid id, UserUpdateRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            Ensure.Any.IsNotNull(user, nameof(user));

            var claims = await _userManager.GetClaimsAsync(user);

            if (user.Type != request.Type)
            {
                await _userManager.RemoveFromRoleAsync(user, user.Type);
                await _userManager.AddToRoleAsync(user, request.Type);
                var newClaim = new Claim(IdentityModel.JwtClaimTypes.Role, request.Type);
                await _userManager.ReplaceClaimAsync(user, claims.First(x => x.Type == IdentityModel.JwtClaimTypes.Role), newClaim);
                user.Type = request.Type;
            }

            bool fullNameChange = false;

            if (user.FirstName != request.FirstName)
            {
                var newClaim = new Claim(IdentityModel.JwtClaimTypes.GivenName, request.FirstName);
                await _userManager.ReplaceClaimAsync(user, claims.First(x => x.Type == IdentityModel.JwtClaimTypes.GivenName), newClaim);
                user.FirstName = request.FirstName;
                fullNameChange = true;
            }

            if (user.LastName != request.LastName)
            {
                var newClaim = new Claim(IdentityModel.JwtClaimTypes.FamilyName, request.LastName);
                await _userManager.ReplaceClaimAsync(user, claims.First(x => x.Type == IdentityModel.JwtClaimTypes.FamilyName), newClaim);
                user.LastName = request.LastName;
                fullNameChange = true;
            }

            if (fullNameChange)
            {
                var requestFullName = $"{request.FirstName} {request.LastName}";
                user.FullName = requestFullName;
            }

            user.Gender = request.Gender;
            user.JoinedDate = request.JoinedDate;
            user.DateOfBirth = request.DateOfBirth;

            await _userManager.UpdateAsync(user);
        }

        public async Task<PagedResponseModel<UserDto>> PagedQueryAsync(string? name, int page, string? type, int limit = 5)
        {
            var query = _userManager.Users;
            query = query.Where(x => String.IsNullOrEmpty(type)
                          || type.ToLower().Contains(x.Type.ToLower()))
                        .Where(x => (String.IsNullOrEmpty(name))
                         || x.FullName.ToLower().Contains(name.ToLower())
                         || x.CodeStaff.Contains(name.ToLower()))
                                .Where(x => x.Disable == false);
            /*if (!string.IsNullOrEmpty(state))
            {
                
                foreach (var includeProperty in state.Split
                (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Where(m => m.Type == includeProperty);
                }
            }*/
            query = query.OrderBy(x => x.CodeStaff);

            var assets = await query
                .AsNoTracking()
                .PaginateAsync(page, limit);

            return new PagedResponseModel<UserDto>
            {
                CurrentPage = assets.CurrentPage,
                TotalPages = assets.TotalPages,
                TotalItems = assets.TotalItems,
                Items = _mapper.Map<IEnumerable<UserDto>>(assets.Items)
            };
        }
    }
}
