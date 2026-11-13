using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VCare.SharedKernel.Domain
{
    public class Hosting
    {
        // The API's public address, used for links that hit the API directly
        // (email verification). Must include the scheme, or email clients won't
        // treat the link as clickable.
        [Required, Url]
        public string BaseUrl { get; set; } = default!;

        // The front-end page that asks for a new password. The reset email links
        // here with ?email=...&token=...; the page posts them, with the password
        // the user types, to POST /api/auth/Reset_password.
        [Required, Url]
        public string ResetPasswordPageUrl { get; set; } = default!;
    }
}
