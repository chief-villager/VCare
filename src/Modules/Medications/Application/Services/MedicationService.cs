using Medications.Application.Abstracts;
using Medications.Application.Services.Interfaces;
using Medications.Domain;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using VCare.SharedKernel.Abstractions;
using Medications.Domain.Errors;
using VCare.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Medications.Application.Services
{
    internal class MedicationService( IMedicationOrderRepository _medicationOrder,
    IMedicalAdministrationRepository _medicalAdministrationRepository,
    ScheduleExpander _expander, IOutcomeRepository outcomeRepository,
    ICurrentUser currentUser, IPatientDirectory patients, TimeProvider clock) : IMedicationService
    {
        // Dose times are wall-clock times in the home ("08:00"), so "now" is too.
        private DateTime Now => clock.GetLocalNow().DateTime;

        public async Task<Result<CreateMedicationOrderResponse>>CreateMedicationOrderAsync( Guid patientId, CreateMedicationOrderRequest request, CancellationToken token)
        {
            // The patient comes from the URL and Patients owns it: without this an
            // order could be filed against a mistyped id or another home's resident.
            if (!await patients.ExistsAsync(patientId, token))
            {
                return Result.Failure<CreateMedicationOrderResponse>(MedicationErrors.PatientNotFound);
            }
            var route = RouteEnumChecker.MedicationRouteChecker(request.MedicationRoute);
            if (route.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(route.Error);
            }
            var status = OrderStatusEnumCheker.OrderStatusCheker(request.Status);
            if (status.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(status.Error);
            }

            // Every schedule is checked before any is used: reading Value on a failed
            // result throws, which would turn a bad request into a 500.
            var schedules = new List<(string, FrequencyType, List<TimeOnly>, int, DayOfWeekFlags, DateOnly?, DateOnly, DateOnly?, int)>();
            foreach (var s in request.Schedules ?? [])
            {
                var frequency = FrequencyTypeEnumChecker.FrequencyTypeChecker(s.FType);
                if (frequency.IsFailure)
                {
                    return Result.Failure<CreateMedicationOrderResponse>(frequency.Error);
                }
                var days = DaysOfWeekEnumChecker.DaysOfWeekChecker(s.DaysOfWeek);
                if (days.IsFailure)
                {
                    return Result.Failure<CreateMedicationOrderResponse>(days.Error);
                }
                schedules.Add((s.Dose, frequency.Value, s.Times ?? [], s.IntervalDays, days.Value, s.AnchorDate, s.EffectiveFrom, s.EffectiveTo, s.Sequence));
            }

            var medicationOrder = MedicationOrder.Record(patientId,currentUser.CareHomeId,request.MedicatioName,route.Value,request.Instructions,
            request.StartDate,request.EndDate,request.Prescriber,request.IsPrn,request.PrnIndication,
            request.PrnMinIntervalMinutes, request.PrnMaxDose24h,
            request.IsControlledDrug, status.Value,
            schedules,
            Now, prnMaxDosesPer24h: request.PrnMaxDosesPer24h);
            if (medicationOrder.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(medicationOrder.Error);
            }
            await _medicationOrder.AddAsync(medicationOrder.Value, token);
            await _medicationOrder.SaveChangesAsync(token);
            return Result.Success(new CreateMedicationOrderResponse(medicationOrder.Value.Id.Value, medicationOrder.Value.Medication));
        }

        public async Task<Result>UpdateMedicationOrderStatusAsync(Guid patientId, Guid medicationOrderId, string status, CancellationToken token)
        {
            var medicationOrder = await GetForPatientAsync(patientId, medicationOrderId, token);
            if (medicationOrder is null)
            {
                return Result.Failure(MedicationErrors.OrderNotFound);
            }
            if (medicationOrder.Status != OrderStatus.Active)
            {
                return Result.Failure(Error.Conflict("Medications.OrderNotActive", "Cannot update status of a completed or cancelled order."));
            }
            var statusResult = medicationOrder.UpdateStatus(status, Now);
            if (statusResult.IsFailure)
            {
                return statusResult;
            }
            await _medicationOrder.SaveChangesAsync(token);
            return Result.Success();
        }

        public async Task<Result<IEnumerable<DueSlot>>> GetDueForDay(Guid patientId, DateOnly day, CancellationToken token)
        {
            var orders = await _medicationOrder.ActiveBetween(patientId, day, day);
            var slots = orders.SelectMany(order => _expander.ExpandSchedule(order, day, day));
            return Result.Success(slots);
        }

        // Slots still needing action today: the day's scheduled doses that nobody
        // has recorded yet. Given and Refused sign a slot off. A Missed placeholder
        // does not while it can still be corrected: the carer who gave the dose and
        // forgot to sign still has it on their list.
        public async Task<Result<IEnumerable<DueSlot>>> GetOutstandingForDay(Guid patientId, DateOnly day, CancellationToken token)
        {
            var orders = await _medicationOrder.ActiveBetween(patientId, day, day);
            var slots = orders.SelectMany(order => _expander.ExpandSchedule(order, day, day)).ToList();

            var now = Now;
            var recorded = (await _medicalAdministrationRepository
                .ForResidentBetweenAsync(patientId, day, day, token))
                .Where(a => a.ScheduledFor.HasValue)
                .ToDictionary(a => (a.MedicationOrderId.Value, a.ScheduledFor!.Value));

            var outstanding = slots.Where(slot =>
                !recorded.TryGetValue((slot.OrderId, slot.DueAt), out var record)
                || (record.IsMissed && DoseWindow.CanStillBeRecorded(slot.DueAt, now)));
            return Result.Success(outstanding);
        }



        public async Task<Result<MedicationAdministrationResponse>> RecordAministration(Guid patientId, Guid orderId, Guid scheduleId,
            RecordAdministrationRequest request, Guid staffId, CancellationToken token)
        {
            var scheduledFor = request.ScheduledFor;
            var order = await GetForPatientAsync(patientId, orderId, token);
            if (order is null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.OrderNotFound);
            }
            if (order.IsPrn)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.PrnUsesItsOwnEndpoint);
            }
            if (order.Status != OrderStatus.Active)
            {
                return Result.Failure<MedicationAdministrationResponse>(Error.Conflict("Medications.OrderNotActive", "Order is not active."));
            }
            var day = DateOnly.FromDateTime(scheduledFor);
            var isSlot = _expander.ExpandSchedule(order,day,day).Any( s => s.ScheduleId == scheduleId && s.DueAt == scheduledFor);
            if (!isSlot)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.NotAScheduledSlot);
            }

            // One rule, shared with the MAR chart and the missed-dose recorder.
            var now = Now;
            if (!DoseWindow.HasOpened(scheduledFor, now))
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.DoseNotYetDue(DoseWindow.OpensAt(scheduledFor)));
            }
            if (!DoseWindow.CanStillBeRecorded(scheduledFor, now))
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.RecordingWindowClosed);
            }

            // When it was actually given, for a carer signing a little after the
            // round. Never in the future, never before the dose could be given.
            var administeredAt = request.AdministeredAt ?? now;
            if (administeredAt > now)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.AdministeredInFuture);
            }
            if (administeredAt < DoseWindow.OpensAt(scheduledFor))
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.AdministeredOutsideWindow);
            }

            // Recorded after the on-time window: still the truth, so it is accepted,
            // but it says why and is flagged so a reviewer can find it.
            var isLate = DoseWindow.HasClosed(scheduledFor, now);
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
            if (isLate && reason is null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.LateReasonRequired);
            }

            var outcome = await outcomeRepository.GetOutcomeByName(request.Outcome ?? string.Empty, token);
            if (outcome is null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.OutcomeNotFound);
            }
            if (outcome.Id == OutcomeCode.MissedId)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.MissedIsRecordedBySystem);
            }

            MedicationAdministration administration;
            var existing = await _medicalAdministrationRepository.Find(orderId, scheduledFor);
            if (existing is not null)
            {
                // A Missed placeholder only means nobody signed in time. The carer's
                // account of what happened replaces it; a real entry is never overwritten.
                if (!existing.IsMissed)
                {
                    return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.DoseAlreadyRecorded);
                }
                var resolved = existing.ResolveMissed(outcome.Id, staffId, administeredAt, reason!, request.Notes, now);
                if (resolved.IsFailure)
                {
                    return Result.Failure<MedicationAdministrationResponse>(resolved.Error);
                }
                administration = existing;
            }
            else
            {
                var created = MedicationAdministration.Create(orderId, order.PatientId.Value, order.CareHomeId.Value, scheduledFor,
                    administeredAt, outcome.Id, staffId, request.Notes, now, isLate, reason);
                if (created.IsFailure)
                {
                    return Result.Failure<MedicationAdministrationResponse>(created.Error);
                }
                administration = created.Value;
                await _medicalAdministrationRepository.AddAsync(administration);
            }

            try
            {
                await _medicalAdministrationRepository.SaveChangesAsync(token);
            }
            catch (DbUpdateException)
            {
                // The unique (order, slot) index: the recorder or another carer got
                // there between our read and our write. Their record stands.
                _medicalAdministrationRepository.DiscardPendingChanges();
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.RecordedConcurrently);
            }
            return Result.Success(ToResponse(administration));
        }

        public async Task<Result<MedicationAdministrationResponse>> RecordPrnAdministration(Guid patientId, Guid orderId,
            RecordPrnAdministrationRequest request, Guid staffId, CancellationToken token)
        {
            var order = await GetForPatientAsync(patientId, orderId, token);
            if (order is null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.OrderNotFound);
            }
            if (!order.IsPrn)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.NotAPrnOrder);
            }
            if (order.Status != OrderStatus.Active)
            {
                return Result.Failure<MedicationAdministrationResponse>(Error.Conflict("Medications.OrderNotActive", "Order is not active."));
            }
            if (string.IsNullOrWhiteSpace(request.DoseGiven))
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.PrnDoseRequired);
            }
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.PrnReasonRequired);
            }

            var now = Now;
            var administeredAt = request.AdministeredAt ?? now;
            if (administeredAt > now)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.AdministeredInFuture);
            }
            if (now - administeredAt > DoseWindow.LateRecordingLimit)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.RecordingWindowClosed);
            }
            var givenOn = DateOnly.FromDateTime(administeredAt);
            if (givenOn < order.StartDate || (order.EndDate is { } end && givenOn > end))
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.PrnOutsideOrder);
            }

            var outcome = await outcomeRepository.GetOutcomeByName(request.Outcome ?? string.Empty, token);
            if (outcome is null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.OutcomeNotFound);
            }
            if (outcome.Id == OutcomeCode.MissedId)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.MissedIsRecordedBySystem);
            }

            // Only a dose actually given counts towards the limits; an offer the
            // resident refused does not.
            if (outcome.Id == OutcomeCode.TakenId)
            {
                var limit = await CheckPrnLimitsAsync(order, administeredAt, token);
                if (limit.IsFailure)
                {
                    return Result.Failure<MedicationAdministrationResponse>(limit.Error);
                }
            }

            var created = MedicationAdministration.Create(orderId, order.PatientId.Value, order.CareHomeId.Value, scheduledFor: null,
                administeredAt, outcome.Id, staffId, request.Notes, now,
                isLate: false, reason: request.Reason.Trim(), doseGiven: request.DoseGiven.Trim());
            if (created.IsFailure)
            {
                return Result.Failure<MedicationAdministrationResponse>(created.Error);
            }
            await _medicalAdministrationRepository.AddAsync(created.Value);
            await _medicalAdministrationRepository.SaveChangesAsync(token);
            return Result.Success(ToResponse(created.Value));
        }

        // The minimum interval and the 24-hour maximum, checked against every dose
        // given within a day either side: an entry may be backdated a few hours, so
        // later doses can fall inside its window too. The 24-hour count rolls; it
        // does not reset at midnight.
        private async Task<Result> CheckPrnLimitsAsync(MedicationOrder order, DateTime administeredAt, CancellationToken token)
        {
            var day = TimeSpan.FromHours(24);
            var given = await _medicalAdministrationRepository.PrnGivenTimesAsync(
                order.Id.Value, administeredAt - day, administeredAt + day, token);

            if (order.PrnMinIntervalMinutes is int minutes)
            {
                var interval = TimeSpan.FromMinutes(minutes);
                var clash = given.Where(t => (t - administeredAt).Duration() < interval).ToList();
                if (clash.Count > 0)
                {
                    return Result.Failure(MedicationErrors.PrnTooSoon(clash.Max() + interval));
                }
            }

            if (order.PrnMaxDosesPer24h is int max)
            {
                var times = given.Append(administeredAt).Order().ToList();
                for (var i = 0; i < times.Count; i++)
                {
                    var inWindow = times.Count(t => t >= times[i] && t < times[i] + day);
                    if (inWindow > max)
                    {
                        return Result.Failure(MedicationErrors.PrnDailyLimitReached(max));
                    }
                }
            }
            return Result.Success();
        }

        public async Task<Result<MedicationOrderResponse>> GetMedicationOrder(Guid patientId, Guid orderId, CancellationToken token)
        {
            var order = await GetForPatientAsync(patientId, orderId, token);
            if (order is null)
            {
                return Result.Failure<MedicationOrderResponse>(MedicationErrors.OrderNotFound);
            }
            return Result.Success(new MedicationOrderResponse(
                order.Id.Value,
                order.Medication,
                order.Route,
                order.Instructions,
                order.StartDate,
                order.EndDate,
                order.Prescriber,
                order.IsPrn,
                order.PrnIndication,
                order.PrnMinIntervalMinutes,
                order.PrnMaxDose24h,
                order.IsControlledDrug,
                order.Schedule.Select(s => new ScheduleResponse(
                    s.Id,
                    s.Dose,
                    s.FType,
                    [.. s.Times],
                    s.IntervalDays,
                    s.DaysOfWeek,
                    s.AnchorDate,
                    s.EffectiveFrom,
                    s.EffectiveTo,
                    s.Sequence
                )).ToList(),
                order.Status,
                order.CreatedAt,
                order.PrnMaxDosesPer24h
            ));
        }

        // An order reached through another resident's URL is treated as missing:
        // the patient in the route and the order's own patient must agree.
        private async Task<MedicationOrder?> GetForPatientAsync(Guid patientId, Guid orderId, CancellationToken token)
        {
            var order = await _medicationOrder.GetAsync(orderId, token);
            return order is not null && order.PatientId.Value == patientId ? order : null;
        }

        private static MedicationAdministrationResponse ToResponse(MedicationAdministration a) => new(
            a.Id.Value,
            a.MedicationOrderId.Value,
            a.ScheduledFor,
            a.AdministeredAt,
            a.OutcomeCodeId,
            a.AdministeredByStaffId,
            a.Notes,
            a.IsLate,
            a.Reason,
            a.DoseGiven);
    }
}
