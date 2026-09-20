using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Staffs.Application.Dto
{
    public record LoginRequest(string UserName, string Password);

    // The service hands back a tuple; a named record keeps the wire shape from
    // serialising as item1/item2.
    public record LoginResponse(string AccessToken, string RefreshToken);

    public record LogoutRequest(string RefreshToken);

    public record RefreshRequest(string RefreshToken);

    public record EmailRequest(string Email);

    public record ResetPasswordRequest(string Email, string Token, string Password);
}
