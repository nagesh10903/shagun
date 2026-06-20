
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shagun.Services.Interfaces.IUserService;

namespace Shagun.Controlers
{
    [ApiController]
    [Route("/users")]
    //[Authorize] 
    public class UserControler : ControllerBase
    {
        private readonly ILogger<UserControler> _logger;
        private readonly IUserService _userService;

        public UserControler(ILogger<UserControler> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsers();
            if (users == null || users.Count == 0)
            {
                return NotFound("No users found.");
            }   
            return Ok(users);
        }
        [HttpGet]
        [Route("/user/{id}")]    
        public async Task<IActionResult> GetUsers(string id)
        {
            var user = await _userService.GetUserByPhone(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok(user);
        }
    }
}