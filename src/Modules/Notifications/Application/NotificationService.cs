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
        public async Task<Result> ProcessEmailNotificationAsync(string payload, string payloadName, CancellationToken cancellationToken)
        {   

            var result = payload.ToLower() switch
            {
                "staffcreatedeventpayload" => await SendWelcomeEmailAsync(payload,cancellationToken),
                "passwordreseteventpayload" => await SendPassWordResetEmailAsync(payload, cancellationToken),
                _ => Result.Failure(new Error("404","payload type does not exist",ErrorKind.NotFound))

            };
            return result;
            
        }
        private async Task<Result>SendWelcomeEmailAsync(string payload, CancellationToken cancellationToken )
        {
            var data = JsonSerializer.Deserialize<StaffCreatedEventPayload>(payload);
            if (data == null)
            {
                Result.Failure(new Error("400","Empty payload cannot be seralized", ErrorKind.Validation));
            }
                            
            var body = $"""
                <p>Welcome to Vbook.</p>
                <p>Confirm your email address to activate your account:</p>
                <p><a href="{data!.Url}">Confirm my email</a></p>
                <p>If you didn't create this account, you can ignore this message.</p>
                """; 
            await emailSender.SendAsync(data.Email,"Confirm your Vbook email",body,cancellationToken);
            return Result.Success();
        }

        private async Task<Result>SendPassWordResetEmailAsync(string payload, CancellationToken cancellationToken )
        {
            var data = JsonSerializer.Deserialize<PassWordResetEventPayload>(payload);
            if (data == null)
            {
                Result.Failure(new Error("400","Empty payload cannot be seralized", ErrorKind.Validation));
            }
                            
            var body = $"""
                <p>Welcome to {data!.Url}.</p>
                <p>Reset your password:</p>
                <p><a href="{data!.Url}">Confirm my email</a></p>
                <p>If you didn't request to change your password, you can ignore this message.</p>
                """; 
            await emailSender.SendAsync(data.Email,"Reset Password",body,cancellationToken);
            return Result.Success();
        }
    }
}