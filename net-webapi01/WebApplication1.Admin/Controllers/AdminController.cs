// using System.Security.Claims;
// using CoreLibrary.Utils;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Newtonsoft.Json;
// using WebApi.Models;
// using WebApi.Models.Requests;
// using WebApi.Services;
// using WebApi.Authentication;
// using WebApi.Data;

// namespace WebApi.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// public class AdminController : ControllerBase
// {
//     private readonly AdminRepository _adminService;
//     private readonly JwtService _jwtService;

//     public AdminController(
//         AdminRepository adminService,
//         JwtService jwtService
//     )
//     {
//         _adminService = adminService;
//         _jwtService = jwtService;
//     }

//     private AdminUser? GetUserClaim()
//     {
//         var userData = User.FindFirstValue(ClaimTypes.UserData);
//         if (!string.IsNullOrEmpty(userData))
//         {
//             return JsonConvert.DeserializeObject<AdminUser>(userData)!;
//         }
//         return null;
//     }





//     [HttpPost("Login")]
//     public async Task<IActionResult> Login(AuthenticationRequest request)
//     {
//         ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.UserName));
//         ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
//         var user = await _adminService.GetUser(request.UserName);
//         ErrorStatuses.ThrowNotFound("User not found", user == null);
//         ErrorStatuses.ThrowInternalErr("Invalid user", !user!.IsSystem && !user.IsCustomer);
//         ErrorStatuses.ThrowInternalErr("Account is disabled", user.Disabled);
//         var pwdResult = await _adminService.VerifyPassword(request.UserName, request.Password);
//         ErrorStatuses.ThrowBadRequest("Bad credentials", !pwdResult);

//         var refreshToken = _jwtService.GenerateRefreshToken();
//         user.RefreshToken = refreshToken;
//         user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
//         await _adminService.UpdateUser(user);
//         var token = await _jwtService.CreateAdminToken(user!);
//         token.RefreshToken = refreshToken;
//         return Ok(new DataResponse<AuthenticationResponse>
//         {
//             Data = token
//         });
//     }

//     [HttpPost("Logout")]
//     [Authorize]
//     public DataResponse Logout()
//     {
//         var username = HttpContext.User.Identity!.Name;
//         return new DataResponse();
//     }

//     [HttpPost("refresh-token")]
//     public async Task<IActionResult> RefreshToken(AdminRefreshTokenRequest request)
//     {
//         ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.RefreshToken) || string.IsNullOrEmpty(request.AccessToken));
//         var claims = await _jwtService.GetClaimsFromToken(request.AccessToken);
//         ErrorStatuses.ThrowBadRequest("Invalid token", claims == null);
//         var username = claims!.FindFirst(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
//         var user = await _adminService.GetUser(username);
//         ErrorStatuses.ThrowBadRequest("Invalid refresh token", String.Compare(user.RefreshToken, request.RefreshToken) != 0);
//         ErrorStatuses.ThrowBadRequest("Refresh token expired", !user.RefreshTokenExpiryDate.HasValue || DateTime.Compare(user.RefreshTokenExpiryDate.Value, DateTime.UtcNow) <= 0);

//         var refreshToken = _jwtService.GenerateRefreshToken();
//         user.RefreshToken = refreshToken;
//         user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
//         await _adminService.UpdateUser(user);
//         var token = await _jwtService.CreateAdminToken(user!, claims.Claims.ToArray());
//         token.RefreshToken = refreshToken;
//         return Ok(new DataResponse<AuthenticationResponse>
//         {
//             Data = token
//         });
//     }

//     [HttpPost("revoke-token")]
//     [Authorize]
//     public async Task<IActionResult> Revoke(string username)
//     {
//         ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(username));
//         var user = await _adminService.GetUser(username!);
//         ErrorStatuses.ThrowNotFound("User not found", user == null);
//         user!.RefreshToken = null;
//         user.RefreshTokenExpiryDate = null;
//         await _adminService.UpdateUser(user);
//         return Ok();
//     }

//     [HttpGet("user-profile")]
//     [Authorize]
//     public async Task<IActionResult> GetUserProfile()
//     {
//         var username = User.Identity!.Name;
//         var user = await _adminService.GetUser(username!);
//         return Ok(new DataResponse<AdminProfile>
//         {
//             Data = new AdminProfile
//             {
//                 Id = user.Id,
//                 Username = user.Username,
//                 IsSystem = user.IsSystem,
//                 IsCustomer = user.IsCustomer,
//                 FullName = user.FullName
//             }
//         });
//     }

//     [HttpPost("change-password"), AdminAuthorize(true, true)]
//     public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
//     {
//         ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword));
//         var username = User.Identity!.Name!;
//         var pwdResult = await _adminService.VerifyPassword(username, request.CurrentPassword);
//         ErrorStatuses.ThrowBadRequest("Invalid current password", !pwdResult);
//         var newPwdResult = await _adminService.VerifyPassword(username, request.NewPassword);
//         ErrorStatuses.ThrowBadRequest("New password have to different with current password", newPwdResult);
//         ValidatePasswordRequest(request.NewPassword);
//         var user = await _adminService.GetUser(username);
//         await _adminService.UpdateUser(user, request.NewPassword);
//         return Ok();
//     }
// }