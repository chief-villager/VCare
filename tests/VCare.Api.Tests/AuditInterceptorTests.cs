using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Auditing;
using Xunit;

namespace VCare.Api.Tests;

// Every change to an audited record is written, in the same save, as who made
// it, when, and each value before and after.
public class AuditInterceptorTests
{
    private static readonly Guid Nurse = Guid.NewGuid();
    private static readonly Guid Home = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 22, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Creating_a_record_audits_its_values_and_who_made_it()
    {
        await using var db = Db(Nurse, Home);
        var resident = new Resident { Id = PatientId.New(), CareHomeId = Home, Name = "Ada" };
        db.Add(resident);

        await db.SaveChangesAsync();

        var entry = Assert.Single(db.Set<AuditEntry>());
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal("Resident", entry.EntityType);
        Assert.Equal(resident.Id.Value.ToString(), entry.EntityId);
        Assert.Equal(resident.Id.Value, entry.ResidentId);
        Assert.Equal(Nurse, entry.StaffId);
        Assert.Equal(Home, entry.CareHomeId);
        Assert.Equal(Now, entry.OccurredAt);
        Assert.Equal("Ada", Json(entry)["Name"].GetString());
    }

    [Fact]
    public async Task Changing_a_record_audits_only_what_changed_from_and_to()
    {
        await using var db = Db(Nurse, Home);
        var resident = new Resident { Id = PatientId.New(), CareHomeId = Home, Name = "Ada", Room = "4" };
        db.Add(resident);
        await db.SaveChangesAsync();

        resident.Room = "12";
        await db.SaveChangesAsync();

        var update = Assert.Single(db.Set<AuditEntry>(), e => e.Action == AuditAction.Updated);
        var changes = Json(update);
        Assert.Equal(["Room"], changes.Keys);
        Assert.Equal("4", changes["Room"].GetProperty("from").GetString());
        Assert.Equal("12", changes["Room"].GetProperty("to").GetString());
    }

    [Fact]
    public async Task Deleting_a_record_audits_its_last_values()
    {
        await using var db = Db(Nurse, Home);
        var note = new Note { Id = Guid.NewGuid(), PatientId = PatientId.New(), Text = "Fell in the garden" };
        db.Add(note);
        await db.SaveChangesAsync();

        db.Remove(note);
        await db.SaveChangesAsync();

        var deleted = Assert.Single(db.Set<AuditEntry>(), e => e.Action == AuditAction.Deleted);
        Assert.Equal("Fell in the garden", Json(deleted)["Text"].GetString());
        Assert.Equal(note.PatientId.Value, deleted.ResidentId);
    }

    [Fact]
    public async Task A_record_with_no_resident_of_its_own_is_attributed_through_its_parent()
    {
        await using var db = Db(Nurse, Home);
        var note = new Note { Id = Guid.NewGuid(), PatientId = PatientId.New(), Text = "Wound check" };
        note.Lines.Add(new Line { Id = Guid.NewGuid(), Text = "Dressing changed" });
        db.Add(note);

        await db.SaveChangesAsync();

        var line = Assert.Single(db.Set<AuditEntry>(), e => e.EntityType == "Line");
        Assert.Equal(note.PatientId.Value, line.ResidentId);
    }

    [Fact]
    public async Task Records_not_marked_audited_are_left_out()
    {
        await using var db = Db(Nurse, Home);
        db.Add(new Secret { Id = Guid.NewGuid(), Hash = "not-for-the-audit-trail" });

        await db.SaveChangesAsync();

        Assert.Empty(db.Set<AuditEntry>());
    }

    [Fact]
    public async Task A_change_made_by_the_system_has_no_staff_and_takes_the_care_home_from_the_record()
    {
        await using var db = Db(Guid.Empty, Guid.Empty);
        db.Add(new Resident { Id = PatientId.New(), CareHomeId = Home, Name = "Ada" });

        await db.SaveChangesAsync();

        var entry = Assert.Single(db.Set<AuditEntry>());
        Assert.Null(entry.StaffId);
        Assert.Equal(Home, entry.CareHomeId);
    }

    [Fact]
    public async Task A_save_that_changes_no_value_writes_nothing()
    {
        await using var db = Db(Nurse, Home);
        var resident = new Resident { Id = PatientId.New(), CareHomeId = Home, Name = "Ada" };
        db.Add(resident);
        await db.SaveChangesAsync();

        db.Entry(resident).State = EntityState.Modified;
        await db.SaveChangesAsync();

        Assert.DoesNotContain(db.Set<AuditEntry>(), e => e.Action == AuditAction.Updated);
    }

    private static Dictionary<string, JsonElement> Json(AuditEntry entry) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.Changes!)!;

    private static TestDb Db(Guid staffId, Guid careHomeId) =>
        new(new DbContextOptionsBuilder<TestDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditSaveChangesInterceptor(new Caller(staffId, careHomeId), new FixedClock()))
            .Options);

    private sealed class Caller(Guid userId, Guid careHomeId) : ICurrentUser
    {
        public Guid UserId => userId;
        public Guid CareHomeId => careHomeId;
        public bool IsAuthenticated => userId != Guid.Empty;
        public bool IsInRole(string role) => false;
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    internal sealed class Resident : IAudited
    {
        public PatientId Id { get; set; }
        public Guid CareHomeId { get; set; }
        public string Name { get; set; } = "";
        public string? Room { get; set; }
    }

    internal sealed class Note : IAudited
    {
        public Guid Id { get; set; }
        public PatientId PatientId { get; set; }
        public string Text { get; set; } = "";
        public List<Line> Lines { get; set; } = [];
    }

    internal sealed class Line : IAudited
    {
        public Guid Id { get; set; }
        public Guid NoteId { get; set; }
        public string Text { get; set; } = "";
    }

    internal sealed class Secret
    {
        public Guid Id { get; set; }
        public string Hash { get; set; } = "";
    }

    internal sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resident>().Property(r => r.Id).HasConversion(id => id.Value, value => new PatientId(value));
            modelBuilder.Entity<Note>().Property(n => n.PatientId).HasConversion(id => id.Value, value => new PatientId(value));
            modelBuilder.Entity<Note>().HasMany(n => n.Lines).WithOne().HasForeignKey(l => l.NoteId);
            modelBuilder.Entity<Secret>();
            modelBuilder.MapAuditEntries();
        }
    }
}
