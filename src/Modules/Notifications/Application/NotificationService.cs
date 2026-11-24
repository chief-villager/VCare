using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Notifications.Application.Contract;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Notifications.Application
{
    internal class NotificationService(ISmtpEmailSender emailSender) : INotification
    {
        public async Task<Result> SendEmailVerificationLinkAsync(string email, string userName, string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(url))
            {
                return Result.Failure(new Error("400","Email and link are required", ErrorKind.Validation));
            }

            var body = $"""
                <p>Welcome to VCare.</p>
                <p>Verify your email address to activate your account:</p>
                <p><a href="{url}">Verify my email</a></p>
                <p>If you didn't create this account, you can ignore this message.</p>
                """;
            await emailSender.SendAsync(email,"Verify your VCare email",body,cancellationToken);
            return Result.Success();
        }

        public async Task<Result> SendPasswordResetLinkAsync(string email, string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(url))
            {
                return Result.Failure(new Error("400","Email and link are required", ErrorKind.Validation));
            }

            var body = $"""
                <p>We received a request to reset your VCare password.</p>
                <p><a href="{url}">Reset my password</a></p>
                <p>If you didn't request to change your password, you can ignore this message.</p>
                """;
            await emailSender.SendAsync(email,"Reset your VCare password",body,cancellationToken);
            return Result.Success();
        }
    }
}