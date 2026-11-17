using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Data;

public class PrisonDbContext : DbContext
{
    public PrisonDbContext(DbContextOptions<PrisonDbContext> options)
        : base(options)
    {
    }

    public DbSet<StaffRole> StaffRoles => Set<StaffRole>();
    public DbSet<Barrack> Barracks => Set<Barrack>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<ManagementAccount> ManagementAccounts => Set<ManagementAccount>();
    public DbSet<Cell> Cells => Set<Cell>();
    public DbSet<Prisoner> Prisoners => Set<Prisoner>();
    public DbSet<CourtCase> CourtCases => Set<CourtCase>();
    public DbSet<CourtTrial> CourtTrials => Set<CourtTrial>();
    public DbSet<JailTimetable> JailTimetable => Set<JailTimetable>();
    public DbSet<GateRegister> GateRegister => Set<GateRegister>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isSqlServer = Database.IsSqlServer();

        // 1. StaffRoles
        modelBuilder.Entity<StaffRole>(entity =>
        {
            if (isSqlServer) entity.ToTable("StaffRoles", "dbo"); else entity.ToTable("StaffRoles");
            entity.HasKey(e => e.RoleID);
            entity.Property(e => e.RoleID).ValueGeneratedOnAdd();
            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // 2. Barracks
        modelBuilder.Entity<Barrack>(entity =>
        {
            if (isSqlServer) entity.ToTable("Barracks", "dbo"); else entity.ToTable("Barracks");
            entity.HasKey(e => e.BarrackID);
            entity.Property(e => e.BarrackID).ValueGeneratedOnAdd();
            entity.Property(e => e.BarrackCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.BarrackName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(30).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.InChargeStaff)
                  .WithMany()
                  .HasForeignKey(e => e.InChargeStaffID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. Cells
        modelBuilder.Entity<Cell>(entity =>
        {
            if (isSqlServer) entity.ToTable("Cells", "dbo"); else entity.ToTable("Cells");
            entity.HasKey(e => e.CellID);
            entity.Property(e => e.CellID).ValueGeneratedOnAdd();
            entity.Property(e => e.CellNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Capacity).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Barrack)
                  .WithMany(b => b.Cells)
                  .HasForeignKey(e => e.BarrackID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.BarrackID, e.CellNumber }).IsUnique();
        });

        // 4. Staff
        modelBuilder.Entity<Staff>(entity =>
        {
            if (isSqlServer) entity.ToTable("Staff", "dbo"); else entity.ToTable("Staff");
            entity.HasKey(e => e.StaffID);
            entity.Property(e => e.StaffID).ValueGeneratedOnAdd();
            entity.Property(e => e.EmployeeNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.OfficialRank).HasMaxLength(100);
            entity.Property(e => e.CNIC).HasColumnType("varchar(15)").HasMaxLength(15);
            entity.Property(e => e.Phone).HasColumnType("varchar(25)").HasMaxLength(25);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            if (isSqlServer)
            {
                entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            }
            else
            {
                entity.Property(e => e.RowVersion).IsConcurrencyToken();
            }

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.StaffMembers)
                  .HasForeignKey(e => e.RoleID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Barrack)
                  .WithMany(b => b.AssignedStaff)
                  .HasForeignKey(e => e.BarrackID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. ManagementAccounts
        modelBuilder.Entity<ManagementAccount>(entity =>
        {
            if (isSqlServer) entity.ToTable("ManagementAccounts", "dbo"); else entity.ToTable("ManagementAccounts");
            entity.HasKey(e => e.AccountID);
            entity.Property(e => e.AccountID).ValueGeneratedOnAdd();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            if (isSqlServer)
            {
                entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            }
            else
            {
                entity.Property(e => e.RowVersion).IsConcurrencyToken();
            }

            entity.HasOne(e => e.Staff)
                  .WithOne(s => s.ManagementAccount)
                  .HasForeignKey<ManagementAccount>(e => e.StaffID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StaffID).IsUnique();
        });

        // 6. Prisoners
        modelBuilder.Entity<Prisoner>(entity =>
        {
            if (isSqlServer) entity.ToTable("Prisoners", "dbo"); else entity.ToTable("Prisoners");
            entity.HasKey(e => e.PrisonerID);
            entity.Property(e => e.PrisonerID).ValueGeneratedOnAdd();
            entity.Property(e => e.PrisonerNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.PrisonerNumber).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.FatherName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CNIC).HasColumnType("varchar(15)").HasMaxLength(15);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(150);
            entity.Property(e => e.EmergencyContactPhone).HasColumnType("varchar(25)").HasMaxLength(25);
            entity.Property(e => e.PhotoReference).HasMaxLength(400);
            entity.Property(e => e.CustodyStatus).HasMaxLength(20).IsRequired().HasDefaultValue("In Custody");
            entity.Property(e => e.ReleaseOrderReference).HasMaxLength(100);
            entity.Property(e => e.TransferDestination).HasMaxLength(200);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            if (isSqlServer)
            {
                entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            }
            else
            {
                entity.Property(e => e.RowVersion).IsConcurrencyToken();
            }

            entity.HasOne(e => e.Cell)
                  .WithMany(c => c.Prisoners)
                  .HasForeignKey(e => e.CellID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RegisteredByStaff)
                  .WithMany()
                  .HasForeignKey(e => e.RegisteredByStaffID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReleaseRecordedByStaff)
                  .WithMany()
                  .HasForeignKey(e => e.ReleaseRecordedByStaffID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. CourtCases
        modelBuilder.Entity<CourtCase>(entity =>
        {
            if (isSqlServer) entity.ToTable("CourtCases", "dbo"); else entity.ToTable("CourtCases");
            entity.HasKey(e => e.CaseID);
            entity.Property(e => e.CaseID).ValueGeneratedOnAdd();
            entity.Property(e => e.CaseNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CaseYear).HasColumnType("smallint").IsRequired();
            entity.Property(e => e.CaseType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CourtName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FIRNumber).HasMaxLength(50);
            entity.Property(e => e.PoliceStation).HasMaxLength(150);
            entity.Property(e => e.AllegedOffence).HasMaxLength(500);
            entity.Property(e => e.CaseStatus).HasMaxLength(30).IsRequired().HasDefaultValue("Pending");
            entity.Property(e => e.LawyerName).HasMaxLength(150);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            if (isSqlServer)
            {
                entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            }
            else
            {
                entity.Property(e => e.RowVersion).IsConcurrencyToken();
            }

            entity.HasOne(e => e.Prisoner)
                  .WithMany(p => p.CourtCases)
                  .HasForeignKey(e => e.PrisonerID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResponsibleStaff)
                  .WithMany()
                  .HasForeignKey(e => e.ResponsibleStaffID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. CourtTrials
        modelBuilder.Entity<CourtTrial>(entity =>
        {
            if (isSqlServer) entity.ToTable("CourtTrials", "dbo"); else entity.ToTable("CourtTrials");
            entity.HasKey(e => e.TrialID);
            entity.Property(e => e.TrialID).ValueGeneratedOnAdd();
            entity.Property(e => e.HearingAt).IsRequired();
            entity.Property(e => e.HearingType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.HearingStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Scheduled");
            entity.Property(e => e.AttendanceStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            entity.Property(e => e.AttendanceMethod).HasMaxLength(20);
            entity.Property(e => e.Outcome).HasMaxLength(500);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            if (isSqlServer)
            {
                entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            }
            else
            {
                entity.Property(e => e.RowVersion).IsConcurrencyToken();
            }

            entity.HasOne(e => e.Case)
                  .WithMany(c => c.CourtTrials)
                  .HasForeignKey(e => e.CaseID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResponsibleStaff)
                  .WithMany()
                  .HasForeignKey(e => e.ResponsibleStaffID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RecordedByStaff)
                  .WithMany()
                  .HasForeignKey(e => e.RecordedByStaffID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. JailTimetable
        modelBuilder.Entity<JailTimetable>(entity =>
        {
            if (isSqlServer) entity.ToTable("JailTimetable", "dbo"); else entity.ToTable("JailTimetable");
            entity.HasKey(e => e.TimetableID);
            entity.Property(e => e.TimetableID).ValueGeneratedOnAdd();
            entity.Property(e => e.ActivityName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DayOfWeek).HasColumnType("tinyint");
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
            entity.Property(e => e.Instructions).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            if (isSqlServer)
            {
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            }
            else
            {
                entity.Property(e => e.RowVersion).IsConcurrencyToken();
            }

            entity.HasOne(e => e.Barrack)
                  .WithMany(b => b.TimetableEntries)
                  .HasForeignKey(e => e.BarrackID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResponsibleStaff)
                  .WithMany()
                  .HasForeignKey(e => e.ResponsibleStaffID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. GateRegister
        modelBuilder.Entity<GateRegister>(entity =>
        {
            if (isSqlServer) entity.ToTable("GateRegister", "dbo"); else entity.ToTable("GateRegister");
            entity.HasKey(e => e.EntryID);
            entity.Property(e => e.EntryID).ValueGeneratedOnAdd();
            entity.Property(e => e.VisitorName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.VisitType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.VisitPurpose).HasMaxLength(250);
            entity.Property(e => e.VehicleNumber).HasMaxLength(200);
            entity.Property(e => e.NumberOfPeople).IsRequired();
            entity.Property(e => e.EntryTime).IsRequired();
            entity.Property(e => e.ExitTime);
        });
    }
}
