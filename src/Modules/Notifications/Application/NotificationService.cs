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
        // Nothing is queued in the outbox any more: verification and password reset
        // emails are sent directly. Any message the outbox still hands over is unknown.
        public Task<Result> ProcessEmailNotificationAsync(string payload, string payloadName, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure(new Error("404","payload type does not exist",ErrorKind.NotFound)));

        public async Task<Result> SendEmailVerificationLinkAsync(string email, string userName, string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(url))
            {
                return Result.Failure(new Error("400","Email and link are required", ErrorKind.Validation));
            }

            var body = $"""
                <p>Welcome to Vbook.</p>
                <p>Verify your email address to activate your account:</p>
                <p><a href="{url}">Verify my email</a></p>
                <p>If you didn't create this account, you can ignore this message.</p>
                """;
            await emailSender.SendAsync(email,"Verify your Vbook email",body,cancellationToken);
            return Result.Success();
        }

        public async Task<Result> SendPasswordResetLinkAsync(string email, string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(url))
            {
                return Result.Failure(new Error("400","Email and link are required", ErrorKind.Validation));
            }

            var body = $"""
                <p>We received a request to reset your Vbook password.</p>
                <p><a href="{url}">Reset my password</a></p>
                <p>If you didn't request to change your password, you can ignore this message.</p>
                """;
            await emailSender.SendAsync(email,"Reset your Vbook password",body,cancellationToken);
            return Result.Success();
        }
    }
}