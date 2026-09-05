namespace VCare.Modules.Patients.Application.Dtos;

public sealed record RegisterPatientRequest(string firstname, string lastname, 
    string gender,string address, DateOnly dateOfBirth, string phoneNumber,
     string email, string emergencyContactName, 
     string emergencyContactPhoneNumber, 
     string emergencyContactRelationship);
