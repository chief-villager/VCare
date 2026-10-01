using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Notifications.Domain.Entity
{
    internal class SmtpOptions
    {
        public const string SectionName = "Smtp";

        [Required] public string Host { get; init; } = string.Empty;
        [Range(1, 65535)] public int Port { get; init; } = 25;

        // Optional: leave empty for an anonymous relay (local fake inboxes).
        public string? User { get; init; }
        public string? Password { get; init; }

        [Required, EmailAddress] public string FromAddress { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;

        public bool UseSsl { get; init; }
    }
}