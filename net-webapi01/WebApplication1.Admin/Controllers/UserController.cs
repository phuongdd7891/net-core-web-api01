// using CoreLibrary.Utils;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using WebApi.Models;
// using WebApi.Models.Requests;
// using WebApi.Authentication;

// namespace WebApi.Controllers;

// public class UserController
// {
//     [HttpPost("create-user")]
//     public async Task<IActionResult> CreateUser(User user)
//     {
//         if (!ModelState.IsValid)
//         {
//             return BadRequest(new DataResponse<string>
//             {
//                 Code = DataResponseCode.InvalidRequest.ToString(),
//                 Data = ModelState.Values.First().Errors.First()!.ErrorMessage
//             });
//         }
//         ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(user.Email));
//         ErrorStatuses.ThrowBadRequest("Invalid phone", !string.IsNullOrEmpty(user.PhoneNumber) && !Utils.ValidPhoneNumber(user.PhoneNumber));
//         var result = await _userManager.CreateAsync(
//             new ApplicationUser()
//             {
//                 UserName = user.Username,
//                 Email = user.Email,
//                 CustomerId = user.CustomerId
//             },
//             user.Password!
//         );
//         if (!result.Succeeded)
//         {
//             return BadRequest(new DataResponse<string>
//             {
//                 Code = DataResponseCode.InvalidRequest.ToString(),
//                 Data = result.Errors.First()!.Description
//             });
//         }
//         var appUser = await _userManager.FindByNameAsync(user.Username);
//         try
//         {
//             var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser!);
//             //await _emailSender.SendEmailAsync(user.Email, "Email Confirmation Token", $"<p>You need to confirm your email account by using below token</p><p><b>{emailToken}</b></p>").ConfigureAwait(false);
//         }
//         catch (Exception ex)
//         {
//             await _userManager.DeleteAsync(appUser!);
//             return BadRequest(new DataResponse<string>
//             {
//                 Code = DataResponseCode.IternalError.ToString(),
//                 Data = ex.Message
//             });
//         }
//         return Ok(new DataResponse());
//     }

//     [HttpPost("lock-user")]
//     [AdminAuthorize(true, true)]
//     public async Task<IActionResult> LockUser([FromBody] LockUserRequest request)
//     {
//         ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.Username));
//         var user = await _userManager.FindByNameAsync(request.Username!);
//         ErrorStatuses.ThrowNotFound("User not found", user == null);
//         if (request.IsLock)
//         {
//             var result = await _userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.MaxValue);
//             ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Lock failed", !result.Succeeded);
//         }
//         else
//         {
//             var result = await _userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(-1));
//             ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Unlock failed", !result.Succeeded);
//         }
//         return Ok();
//     }

//     [HttpPost("update-user")]
//     [Authorize]
//     public async Task<IActionResult> UpdateUser(User user)
//     {
//         var appUser = await _userManager.FindByNameAsync(user.Username);
//         ErrorStatuses.ThrowNotFound("User not found", appUser == null);
//         ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(user.Email));
//         ErrorStatuses.ThrowBadRequest("Invalid phone", !string.IsNullOrEmpty(user.PhoneNumber) && !Utils.ValidPhoneNumber(user.PhoneNumber));
//         if (!string.IsNullOrEmpty(user.Password))
//         {
//             appUser!.PasswordHash = _userManager.PasswordHasher.HashPassword(appUser, user.Password);
//         }
//         appUser!.Email = user.Email;
//         appUser.PhoneNumber = user.PhoneNumber;
//         appUser.CustomerId = user.CustomerId;
//         var result = await _userManager.UpdateAsync(appUser);
//         if (!result.Succeeded)
//         {
//             return BadRequest(new DataResponse<string>
//             {
//                 Code = DataResponseCode.IternalError.ToString(),
//                 Data = result.Errors.First()!.Description
//             });
//         }
//         return Ok(new DataResponse());
//     }

//     [HttpGet("user"), AdminAuthorize(true, true)]
//     public async Task<IActionResult> GetUser(string username)
//     {
//         ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(username));
//         var user = await _userManager.FindByNameAsync(username);
//         ErrorStatuses.ThrowNotFound("User not found", user == null);
//         var roles = await _userManager.GetRolesAsync(user!);
//         AdminUser? customer = null;
//         if (!string.IsNullOrEmpty(user!.CustomerId))
//         {
//             customer = await _adminService.GetUserById(user.CustomerId);
//         }
//         return Ok(new DataResponse<UserViewModel>
//         {
//             Data = new UserViewModel(user)
//             {
//                 Roles = roles.ToArray(),
//                 CustomerName = customer?.FullName ?? string.Empty
//             }
//         });
//     }
// }