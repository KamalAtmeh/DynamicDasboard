using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing users.
    /// Provides endpoints to perform CRUD operations on users.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : AppControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        /// <param name="userService">The user service to interact with user data.</param>
        /// <param name="logsService">The logs service for exception handling.</param>
        public UsersController(IUserService userService, ILogsService logsService)
            : base(logsService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Adds a new user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            try
            {
                var result = await _userService.AddUserAsync(user);
                return CreatedAtAction(nameof(GetAllUsers), new { id = result }, user);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets a user by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByID(int id)
        {
            try
            {
                var user = await _userService.GetUserByIDAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }
    }
}
