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



            foreach (var user in theSameUsernameLoginList)
            {
                if (user.UserName == userNameLogin.ToString())
                {
                    continue;
                }

                // Neu ma la binhnv1
                bool res = int.TryParse(user.UserName.Split(userNameLogin.ToString()).Last(), out int _);
                if (!res)
                {
                    theSameUsernameLoginList = theSameUsernameLoginList.Where(x => x.UserName != user.UserName);
                }
            }



            // lastUsername = usernamelogin + ordernumber ~ binhnv1 = binhnv + 1


            userNameLogin.Append((theSameUsernameLoginList.Count()).ToString());
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

        public async Task<PagedResponseModel<UserDto>> PagedQueryAsync(string? name, int page, int limit)
        {
            var query = _userManager.Users;

            query = query.Where(m => string.IsNullOrEmpty(name) || m.UserName == name);
            query = query.OrderByDescending(x => x.UserName);

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
