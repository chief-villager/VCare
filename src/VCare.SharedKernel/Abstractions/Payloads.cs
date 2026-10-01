using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VCare.SharedKernel.Abstractions
{
    public interface IOutboxPayload
    {
        
    }
    public sealed class StaffCreatedEventPayload(
        string email,
        string userName,
        string url) : IOutboxPayload
    {
        
        public string Email { get; } = email;
        public string UserName { get; } = userName;
        public string Url { get; } = url;
    }

     public sealed class PassWordResetEventPayload(
        string email,
        string userName,
        string url) : IOutboxPayload
    {
        
        public string Email { get; } = email;
        public string UserName { get; } = userName;
        public string Url { get; } = url;
    }

}