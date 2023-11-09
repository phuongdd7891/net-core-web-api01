using System.ComponentModel.DataAnnotations;
using WebApi.Services;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.ResourceModels;
using CoreLibrary.Models;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private UserManager<ApplicationUser> _userManager;
    private RoleManager<ApplicationRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ApiKeyService _apiKeyService;
    private readonly CacheService _cacheService;
    private readonly RoleActionRepository _roleActionRepository;

    public OperationsController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        JwtService jwtService,
        ApiKeyService apiKeyService,
        CacheService cacheSrevice,
        RoleActionRepository roleActionRepository
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _apiKeyService = apiKeyService;
        _cacheService = cacheSrevice;
        _roleActionRepository = roleActionRepository;
    }

    [HttpPost("CreateUser")]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userManager.CreateAsync(
            new ApplicationUser()
            {
                UserName = user.Username,
                Email = user.Email
            },
            user.Password
        );

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.Password = "";
        return Created("", user);
    }

    [HttpPost("Login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(AuthenticationRequest request, [FromQuery(Name = "t")] string? tokenType)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user == null)
        {
            return NotFound("Not found user");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            return BadRequest("Bad credentials");
        }
        if (tokenType == "jwt")
        {
            var token = await _jwtService.CreateToken(user);
            return Ok(token);
        }
        else
        {
            var token = await _apiKeyService.CreateRedisToken(user);
            return Ok(token);
        }
    }

    #region Roles
    [HttpPost("CreateRole")]
    public async Task<IActionResult> CreateRole([FromBody] string name)
    {
        IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole() { Name = name });
        if (result.Succeeded)
        {
            await _cacheService.LoadUserRoles();
            return Ok("Role Created Successfully");
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }

    [HttpPost("UserRoles")]
    public async Task<IActionResult> AddUserRoles(UserRolesRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.Username);

        if (user == null)
        {
            return NotFound("Not found user");
        }
        var result = await _userManager.AddToRolesAsync(user, req.Roles);
        if (result.Succeeded)
            return Ok("Add user role Successfully");
        else
        {
            return BadRequest(result.Errors);
        }
    }

    #endregion

    [HttpPost("RoleAction")]
    public async Task<IActionResult> AddRoleAction(RoleAction request)
    {
        await _roleActionRepository.Add(request.RequestAction, request.Roles);
        await _cacheService.LoadRoleActions();
        return Ok();
    }
}