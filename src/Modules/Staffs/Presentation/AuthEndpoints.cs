using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Staffs.Application.Dto;
using Staffs.Application.Services.Interface;
using VCare.SharedKernel.Results;

namespace Staffs.Presentation
{
    public static class AuthEndpoints
    {
        public static void AddAuthEndpoints(this IEndpointRouteBuilder app)
        {
            // A caller with no usable token is the normal case here: this group is
            // how they get one. The paths match the callback links AuthService builds.
            var auth = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();
            auth.MapPost("/login",Login);
            auth.MapPost("/refresh",Refresh);
            auth.MapPost("/logout",Logout);
            auth.MapGet("/verify-email",VerifyEmail);
            auth.MapPost("/verify-email/resend",ResendVerificationLink);
            auth.MapPost("/forgot-password",RequestPasswordReset);
            auth.MapPost("/Reset_password",ResetPassword);
        }

        public static async Task<IResult>Login(LoginRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.LoginStaffAsync(request.UserName,request.Password,cancellationToken);
            return response.IsFailure
                ? response.ToProblem()
                : TypedResults.Ok(new LoginResponse(response.Value.AccessToken, response.Value.RefreshToken));
        }

        // Rotation: the presented token is revoked and a fresh pair issued in the
        // same family. Presenting an already-rotated token kills the whole family,
        // so a failure here is 401 and the caller must log in again.
        public static async Task<IResult>Refresh(RefreshRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.RefreshTokenAsync(request.RefreshToken,cancellationToken);
            return response.IsFailure
                ? response.ToProblem()
                : TypedResults.Ok(new LoginResponse(response.Value.AccessToken, response.Value.RefreshToken));
        }

        public static async Task<IResult>Logout(LogoutRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.LogoutStaffAsync(request.RefreshToken,cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.NoContent();
        }

        // The target of the emailed link: a GET, because the staff member arrives by browser.
        public static async Task<IResult>VerifyEmail(string email, string token, IStaffService staffService)
        {
            var response = await staffService.VerifyEmailAsync(email, token);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok("Email verified. You can now sign in.");
        }

        // For a staff member who never got the link, or whose link expired. 202
        // whether or not a link was sent, so the response gives nothing away about
        // which emails are registered.
        public static async Task<IResult>ResendVerificationLink(EmailRequest request,
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.ResendEmailVerificationLinkAsync(request.Email, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Accepted((string?)null);
        }

        public static async Task<IResult>RequestPasswordReset(EmailRequest request, IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.RequestPasswordResetAsync(request.Email, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok();
        }

        public static async Task<IResult>ResetPassword(ResetPasswordRequest request, IStaffService staffService)
        {
            var response = await staffService.ResetPasswordAsync(request.Email,request.Token,request.Password);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok();
        }
    }
}
