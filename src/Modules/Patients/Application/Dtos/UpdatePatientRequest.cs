using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Patients.Application.Dtos
{
    public sealed record UpdatePatientRequest
    (  
        string Firstname, string Lastname, string Gender, 
        string Address, DateOnly DateOfBirth, string EmergencyContactRelationship, 
        string EmergencyContactPhoneNumber, string EmergencyContactName, string phoneNumber
        , string Email
    );
   

  
}