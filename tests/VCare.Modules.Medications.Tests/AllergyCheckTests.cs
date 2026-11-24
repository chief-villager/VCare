using Medications.Application.Services;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// An order is checked against the resident's recorded allergies before it is saved.
public class AllergyCheckTests
{
    private static readonly DateOnly Day = new(2026, 11, 19);

    private static Task<Result<CreateMedicationOrderResponse>> Prescribe(MedicationWorld world, string medication,
        string? overrideReason = null) =>
        world.Medications.CreateMedicationOrderAsync(MedicationWorld.Resident, new CreateMedicationOrderRequest(
            medication, "ORAL", null, Day, null, "Dr Okafor", false, null, null, null, false,
            [new ScheduleRequest("1 capsule", "DailyAtTimes", [new TimeOnly(8, 0)], 1, null, null, Day, null, 1)],
            "Active", AllergyOverrideReason: overrideReason), CancellationToken.None);

    [Fact]
    public async Task An_order_for_a_medicine_the_resident_is_allergic_to_is_refused()
    {
        var world = new MedicationWorld(Day.ToDateTime(new TimeOnly(10, 0)));
        world.Allergies.Add(MedicationWorld.Resident, "Penicillin");

        var result = await Prescribe(world, "Phenoxymethylpenicillin 250mg tablets");

        Assert.Equal("Medications.AllergyConflict", result.Error.Code);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Contains("Penicillin", result.Error.Description);
        Assert.Empty(world.Orders.Orders);
    }

    [Fact]
    public async Task The_allergy_check_ignores_letter_case()
    {
        var world = new MedicationWorld(Day.ToDateTime(new TimeOnly(10, 0)));
        world.Allergies.Add(MedicationWorld.Resident, "aspirin");

        var result = await Prescribe(world, "ASPIRIN 75mg dispersible");

        Assert.Equal("Medications.AllergyConflict", result.Error.Code);
    }

    [Fact]
    public async Task An_order_confirmed_by_the_prescriber_despite_the_allergy_is_saved_with_why()
    {
        var world = new MedicationWorld(Day.ToDateTime(new TimeOnly(10, 0)));
        world.Allergies.Add(MedicationWorld.Resident, "Penicillin");

        var result = await Prescribe(world, "Phenoxymethylpenicillin 250mg tablets",
            "Dr Okafor confirmed by phone: the 2019 rash was not an allergy");

        Assert.True(result.IsSuccess);
        Assert.Equal("Dr Okafor confirmed by phone: the 2019 rash was not an allergy",
            Assert.Single(world.Orders.Orders).AllergyOverrideReason);
    }

    [Fact]
    public async Task An_order_with_no_allergy_match_is_saved_with_who_entered_it()
    {
        var world = new MedicationWorld(Day.ToDateTime(new TimeOnly(10, 0)));
        world.Allergies.Add(MedicationWorld.Resident, "Penicillin");

        var result = await Prescribe(world, "Paracetamol 500mg tablets");

        Assert.True(result.IsSuccess);
        Assert.Equal(MedicationWorld.Carer, Assert.Single(world.Orders.Orders).CreatedByStaffId);
    }
}
