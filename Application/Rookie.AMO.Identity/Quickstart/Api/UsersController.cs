using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Rookie.AMO.Identity.Business;
using Rookie.AMO.Identity.Bussiness.Interfaces;
using Rookie.AMO.Identity.Filters;
using Rookie.AMO.Identity.ViewModel.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Rookie.AMO.Identity.Quickstart.Api
{
    [EnableCors("AllowOrigins")]
    [Route("api/[controller]")]
    [ApiController]
    [CustomAuthorize]
    public class UsersController : ControllerBase
    {

        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        // GET: api/<UsersController>
        [HttpGet]
        public async Task<IEnumerable<UserDto>> GetListUser()
        {
            return await _userService.GetAllAsync();
        }

        // GET api/<UsersController>/5
        [CustomAuthorize(Role = "Admin")]
        [HttpGet("{id}")]
        public async Task<UserDto> GetUserById(string id)
        {
            return await _userService.GetUserById(id);
        }

        // POST api/<UsersController>
        [CustomAuthorize(Role = "Admin")]
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUserAsync(UserRegistrationDto newUser)
        {
            var userDto = await _userService.CreateUserAsync(newUser, User.Claims.FirstOrDefault(x => x.Type == "location").Value);
            return Created("api/users", userDto);
        }

        // PUT api/<UsersController>/5

        [CustomAuthorize(Role = "Admin")]
        [HttpPut("{id}")]
        public async Task Update(Guid id, [FromBody] UserUpdateRequest request)
        {
            await _userService.UpdateUserAsync(id, request);
        }


        // DELETE api/<UsersController>/5
        [CustomAuthorize(Role = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                await _userService.DisableUserById(id);

            }
            catch
            {
                return NotFound();
            }
            return Ok();

        }

        // DELETE api/<UsersController>/find
        [CustomAuthorize(Role = "Admin")]
        [HttpGet("find")]
        public async Task<PagedResponseModel<UserDto>> PagedQueryAsync
        (string name, int page, string type, string sort, bool desc, string id, int limit)
        {
            return await _userService.PagedQueryAsync(name, page, type, sort, desc, id, limit);
        }
    }
}
