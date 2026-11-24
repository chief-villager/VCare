using VCare.SharedKernel.Auditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHomes.Domain.Entity
{
    internal class CareHome : AggregateRoot<CareHomeId>, IAudited
    {
        public string Name {get; private set;} = null!;
        public string Address {get; private set;} = null!;
        public string Email {get; private set;} = null!;

        // An IANA zone such as "Europe/London". Dose times ("08:00") are times in
        // the home, so every "is this dose due" question is asked on this clock.
        public string TimeZoneId { get; private set; } = null!;
        public const int TimeZoneIdMaxLength = 64;


        private CareHome(){}


        public static Result<CareHome> CreateCareHome(string name, string address, string email, string timeZoneId)
        {
            var zone = CheckTimeZone(timeZoneId);
            if (zone.IsFailure)
            {
                return Result.Failure<CareHome>(zone.Error);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<CareHome>("Name is required");
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                return Result.Failure<CareHome>("Address is required");
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<CareHome>("Email is required");
            }
            CareHome careHome = new CareHome
            {
                Id = CareHomeId.New(),
                Name = name,
                Address = address,
                Email = email,
                TimeZoneId = zone.Value
            };
            return Result.Success(careHome);

        }

        // Only zones this server can convert with: a zone it does not know would
        // fail on the first dose-time question instead of here.
        private static Result<string> CheckTimeZone(string? timeZoneId)
        {
            var id = timeZoneId?.Trim();
            if (string.IsNullOrEmpty(id) || id.Length > TimeZoneIdMaxLength
                || !TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
            {
                return Result.Failure<string>(Error.Validation("CareHomes.InvalidTimeZone",
                    "A time zone such as Europe/London is required."));
            }
            return Result.Success(id);
        }

        public Result UpdateCareHome(string? name, string? address, string? email, string? timeZoneId = null)
        {
            // Checked before anything changes, so a bad zone leaves the home as it was.
            string? zone = null;
            if (timeZoneId is not null)
            {
                var checkedZone = CheckTimeZone(timeZoneId);
                if (checkedZone.IsFailure)
                {
                    return checkedZone;
                }
                zone = checkedZone.Value;
            }
            if (zone is not null)
            {
                TimeZoneId = zone;
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                Name=name;
            }
            if (!string.IsNullOrWhiteSpace(address))
            {
                Address=address;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                Email=email;
            }
            return Result.Success();
        }
       
    }
}