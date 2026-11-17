using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Data;

public static class DbInitializer
{
    public static void Initialize(PrisonDbContext context)
    {
        // Creates tables if not already created
        context.Database.EnsureCreated();

        // 1. Roles
        if (!context.StaffRoles.Any())
        {
            var roles = new List<StaffRole>
            {
                new() { RoleID = 1, RoleName = RoleConstants.JailSuperintendent, Description = "Full administrative command and overall jail supervision", IsActive = true },
                new() { RoleID = 2, RoleName = RoleConstants.TimetableOfficer, Description = "Daily movement, routine, and prisoner schedules", IsActive = true },
                new() { RoleID = 3, RoleName = RoleConstants.Gatekeeper, Description = "Main gate security, visitor logging, and departure recording", IsActive = true },
                new() { RoleID = 4, RoleName = RoleConstants.CourtRecordsOfficer, Description = "Judicial hearings, case registration, and trial records", IsActive = true },
                new() { RoleID = 5, RoleName = RoleConstants.PrisonerRecordsOfficer, Description = "Inmate registration, cell assignments, and custody records", IsActive = true },
                new() { RoleID = 6, RoleName = RoleConstants.SystemAdministrator, Description = "User accounts, staff profiles, and system maintenance", IsActive = true }
            };
            context.StaffRoles.AddRange(roles);
            context.SaveChanges();
        }

        // 2. Barracks
        if (!context.Barracks.Any())
        {
            var barracks = new List<Barrack>
            {
                new() { BarrackID = 1, BarrackCode = "BAR-01", BarrackName = "Security Block Alpha", Category = "High Security", IsActive = true },
                new() { BarrackID = 2, BarrackCode = "BAR-02", BarrackName = "General Ward Bravo", Category = "Under Trial", IsActive = true },
                new() { BarrackID = 3, BarrackCode = "BAR-03", BarrackName = "Hospital Ward Charlie", Category = "Medical Care", IsActive = true },
                new() { BarrackID = 4, BarrackCode = "BAR-04", BarrackName = "Quarantine Ward Delta", Category = "Intake Isolation", IsActive = true }
            };
            context.Barracks.AddRange(barracks);
            context.SaveChanges();
        }

        // 3. Cells
        if (!context.Cells.Any())
        {
            var cells = new List<Cell>
            {
                new() { CellID = 1, BarrackID = 1, CellNumber = "101", Capacity = 10, IsActive = true },
                new() { CellID = 2, BarrackID = 1, CellNumber = "102", Capacity = 10, IsActive = true },
                new() { CellID = 3, BarrackID = 2, CellNumber = "201", Capacity = 15, IsActive = true },
                new() { CellID = 4, BarrackID = 2, CellNumber = "202", Capacity = 15, IsActive = true },
                new() { CellID = 5, BarrackID = 3, CellNumber = "301", Capacity = 8, IsActive = true },
                new() { CellID = 6, BarrackID = 4, CellNumber = "401", Capacity = 12, IsActive = true }
            };
            context.Cells.AddRange(cells);
            context.SaveChanges();
        }

        // 4. Staff (6 Assigned Personnel)
        if (!context.Staff.Any())
        {
            var staffMembers = new List<Staff>
            {
                new()
                {
                    StaffID = 1,
                    EmployeeNumber = "SP-001",
                    FullName = "Awais Ali",
                    OfficialRank = "Superintendent of Prisons",
                    RoleID = 1,
                    BarrackID = null,
                    CNIC = "41303-1234567-1",
                    Phone = "0300-1234561",
                    Address = "Officer Colony, Karachi Central Prison",
                    DateOfBirth = new DateOnly(1980, 5, 15),
                    JoiningDate = new DateOnly(2010, 1, 1),
                    IsActive = true
                },
                new()
                {
                    StaffID = 2,
                    EmployeeNumber = "SP-002",
                    FullName = "Ahmed Raza",
                    OfficialRank = "Timetable & Movement Officer",
                    RoleID = 2,
                    BarrackID = 1,
                    CNIC = "41303-1234567-2",
                    Phone = "0300-1234562",
                    Address = "Staff Quarters Block B, Karachi",
                    DateOfBirth = new DateOnly(1985, 8, 20),
                    JoiningDate = new DateOnly(2012, 3, 1),
                    IsActive = true
                },
                new()
                {
                    StaffID = 3,
                    EmployeeNumber = "SP-003",
                    FullName = "Aqib Mubeen",
                    OfficialRank = "Head Gatekeeper & Security Incharge",
                    RoleID = 3,
                    BarrackID = null,
                    CNIC = "41303-1234567-3",
                    Phone = "0300-1234563",
                    Address = "Main Gate Security Barrack, Karachi",
                    DateOfBirth = new DateOnly(1988, 11, 10),
                    JoiningDate = new DateOnly(2015, 6, 15),
                    IsActive = true
                },
                new()
                {
                    StaffID = 4,
                    EmployeeNumber = "SP-004",
                    FullName = "Arshan Ali",
                    OfficialRank = "Court Hearings & Liaison Officer",
                    RoleID = 4,
                    BarrackID = null,
                    CNIC = "41303-1234567-4",
                    Phone = "0300-1234564",
                    Address = "Judicial Branch Quarters, Karachi",
                    DateOfBirth = new DateOnly(1990, 2, 25),
                    JoiningDate = new DateOnly(2016, 9, 1),
                    IsActive = true
                },
                new()
                {
                    StaffID = 5,
                    EmployeeNumber = "SP-005",
                    FullName = "Noor Bux",
                    OfficialRank = "Prisoner Records & Intake Clerk",
                    RoleID = 5,
                    BarrackID = null,
                    CNIC = "41303-1234567-5",
                    Phone = "0300-1234565",
                    Address = "Staff Quarters Block D, Karachi",
                    DateOfBirth = new DateOnly(1992, 7, 14),
                    JoiningDate = new DateOnly(2018, 2, 10),
                    IsActive = true
                },
                new()
                {
                    StaffID = 6,
                    EmployeeNumber = "SP-006",
                    FullName = "Ghulam Mustafa",
                    OfficialRank = "IT Systems & Database Administrator",
                    RoleID = 6,
                    BarrackID = null,
                    CNIC = "41303-1234567-6",
                    Phone = "0300-1234566",
                    Address = "HQ IT Directorate, Karachi",
                    DateOfBirth = new DateOnly(1989, 12, 1),
                    JoiningDate = new DateOnly(2014, 4, 1),
                    IsActive = true
                }
            };
            context.Staff.AddRange(staffMembers);
            context.SaveChanges();
        }

        // 5. ManagementAccounts (The 6 exact accounts from database)
        if (!context.ManagementAccounts.Any())
        {
            var accounts = new List<ManagementAccount>
            {
                new()
                {
                    AccountID = 1,
                    StaffID = 1,
                    Username = "awais.ali@jailpolice.sindh.pk",
                    PasswordHash = "Awais@123",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    AccountID = 2,
                    StaffID = 2,
                    Username = "ahmed.raza@jailpolice.sindh.pk",
                    PasswordHash = "Ahmed@123",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    AccountID = 3,
                    StaffID = 3,
                    Username = "aqib.mubeen@jailpolice.sindh.pk",
                    PasswordHash = "Aqib@123",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    AccountID = 4,
                    StaffID = 4,
                    Username = "arshan.ali@jailpolice.sindh.pk",
                    PasswordHash = "Arshan@123",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    AccountID = 5,
                    StaffID = 5,
                    Username = "noor.bux@jailpolice.sindh.pk",
                    PasswordHash = "Noor@123",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    AccountID = 6,
                    StaffID = 6,
                    Username = "ghulam.mustafa@jailpolice.sindh.pk",
                    PasswordHash = "Mustafa@123",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                }
            };
            context.ManagementAccounts.AddRange(accounts);
            context.SaveChanges();
        }

        // 6. Prisoners
        if (!context.Prisoners.Any())
        {
            var prisoners = new List<Prisoner>
            {
                new()
                {
                    PrisonerID = 1,
                    PrisonerNumber = "PR-2026-001",
                    FullName = "Tariq Jameel",
                    FatherName = "Jameel Ahmed",
                    DateOfBirth = new DateOnly(1994, 6, 12),
                    Gender = "Male",
                    CNIC = "42101-9876543-1",
                    Address = "Sector 4, Landhi, Karachi",
                    AdmissionDate = new DateOnly(2026, 1, 10),
                    CustodyStatus = "In Custody",
                    CellID = 1,
                    RegisteredByStaffID = 5,
                    EmergencyContactName = "Jameel Ahmed (Father)",
                    EmergencyContactPhone = "0321-9876543"
                },
                new()
                {
                    PrisonerID = 2,
                    PrisonerNumber = "PR-2026-002",
                    FullName = "Mansoor Ali",
                    FatherName = "Ali Asghar",
                    DateOfBirth = new DateOnly(1996, 3, 22),
                    Gender = "Male",
                    CNIC = "42201-1234567-2",
                    Address = "Latifabad No. 7, Hyderabad",
                    AdmissionDate = new DateOnly(2026, 2, 14),
                    CustodyStatus = "In Custody",
                    CellID = 2,
                    RegisteredByStaffID = 5,
                    EmergencyContactName = "Ali Asghar (Father)",
                    EmergencyContactPhone = "0333-1234567"
                },
                new()
                {
                    PrisonerID = 3,
                    PrisonerNumber = "PR-2026-003",
                    FullName = "Kamran Shah",
                    FatherName = "Shah Nawaz",
                    DateOfBirth = new DateOnly(1991, 9, 5),
                    Gender = "Male",
                    CNIC = "42301-4567890-3",
                    Address = "Old Sukkur City",
                    AdmissionDate = new DateOnly(2026, 3, 1),
                    CustodyStatus = "In Custody",
                    CellID = 3,
                    RegisteredByStaffID = 5,
                    EmergencyContactName = "Shah Nawaz (Father)",
                    EmergencyContactPhone = "0345-4567890"
                },
                new()
                {
                    PrisonerID = 4,
                    PrisonerNumber = "PR-2026-004",
                    FullName = "Zahid Hussain",
                    FatherName = "Ghulam Hussain",
                    DateOfBirth = new DateOnly(1988, 11, 19),
                    Gender = "Male",
                    CNIC = "42401-7890123-4",
                    Address = "Station Road, Larkana",
                    AdmissionDate = new DateOnly(2026, 4, 18),
                    CustodyStatus = "In Custody",
                    CellID = 4,
                    RegisteredByStaffID = 5,
                    EmergencyContactName = "Ghulam Hussain (Father)",
                    EmergencyContactPhone = "0312-7890123"
                }
            };
            context.Prisoners.AddRange(prisoners);
            context.SaveChanges();
        }

        // 7. CourtCases & CourtTrials
        if (!context.CourtCases.Any())
        {
            var c1 = new CourtCase
            {
                CaseID = 1,
                PrisonerID = 1,
                CaseNumber = "CR-9042/2026",
                CaseYear = 2026,
                CaseType = "Criminal",
                CourtName = "District Sessions Court South, Karachi",
                FIRNumber = "142/2026",
                PoliceStation = "Clifton Police Station",
                AllegedOffence = "Sections 302/34 PPC (Homicide Trial)",
                CaseStatus = "Under Trial",
                LawyerName = "Advocate Muhammad Riaz",
                ResponsibleStaffID = 4,
                Remarks = "Security escort mandated for all appearances"
            };

            var c2 = new CourtCase
            {
                CaseID = 2,
                PrisonerID = 2,
                CaseNumber = "BA-1105/2026",
                CaseYear = 2026,
                CaseType = "Bail Petition",
                CourtName = "High Court of Sindh, Karachi",
                FIRNumber = "88/2026",
                PoliceStation = "Saddar Police Station",
                AllegedOffence = "Section 392 PPC (Armed Robbery)",
                CaseStatus = "Pending",
                LawyerName = "Advocate Sohail Ahmed",
                ResponsibleStaffID = 4,
                Remarks = "Bail confirmation hearing"
            };

            context.CourtCases.AddRange(c1, c2);
            context.SaveChanges();

            var now = DateTime.Now;
            var t1 = new CourtTrial
            {
                TrialID = 1,
                CaseID = 1,
                HearingAt = new DateTime(now.Year, now.Month, now.Day, 10, 30, 0),
                HearingType = "Formal Charges & Plea",
                HearingStatus = "Scheduled",
                AttendanceStatus = "Pending",
                AttendanceMethod = "In Person",
                ResponsibleStaffID = 4,
                RecordedByStaffID = 4,
                Remarks = "Police security escort confirmed"
            };

            var t2 = new CourtTrial
            {
                TrialID = 2,
                CaseID = 1,
                HearingAt = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0),
                HearingType = "Prosecution Witness Recording",
                HearingStatus = "Scheduled",
                AttendanceStatus = "Pending",
                AttendanceMethod = "Video Link",
                ResponsibleStaffID = 4,
                RecordedByStaffID = 4,
                Remarks = "Video link facility reserved in Courtroom 3"
            };

            var tomorrow = now.AddDays(1);
            var t3 = new CourtTrial
            {
                TrialID = 3,
                CaseID = 2,
                HearingAt = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 11, 0, 0),
                HearingType = "Bail Hearing",
                HearingStatus = "Scheduled",
                AttendanceStatus = "Pending",
                AttendanceMethod = "In Person",
                ResponsibleStaffID = 4,
                RecordedByStaffID = 4,
                Remarks = "Defence counsel requested urgent hearing"
            };

            context.CourtTrials.AddRange(t1, t2, t3);
            context.SaveChanges();
        }

        // 8. JailTimetable
        if (!context.JailTimetable.Any())
        {
            var activities = new List<JailTimetable>
            {
                new()
                {
                    TimetableID = 1,
                    ActivityName = "Morning Roll Call & Barrack Inspection",
                    DayOfWeek = null,
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(7, 0, 0),
                    BarrackID = 1,
                    ResponsibleStaffID = 2,
                    Instructions = "Full physical headcount and lock verification across all barracks.",
                    IsActive = true
                },
                new()
                {
                    TimetableID = 2,
                    ActivityName = "Breakfast & Medical Dispensary Queue",
                    DayOfWeek = null,
                    StartTime = new TimeSpan(7, 30, 0),
                    EndTime = new TimeSpan(8, 30, 0),
                    BarrackID = 1,
                    ResponsibleStaffID = 2,
                    Instructions = "Supervise orderly breakfast line and dispense scheduled daily medication.",
                    IsActive = true
                },
                new()
                {
                    TimetableID = 3,
                    ActivityName = "Court Escort Van Departure",
                    DayOfWeek = null,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(9, 45, 0),
                    BarrackID = null,
                    ResponsibleStaffID = 2,
                    Instructions = "Cross-check judicial production warrants before inmates exit main gate.",
                    IsActive = true
                },
                new()
                {
                    TimetableID = 4,
                    ActivityName = "Vocational Carpentry & Tailoring Workshop",
                    DayOfWeek = null,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(12, 30, 0),
                    BarrackID = 2,
                    ResponsibleStaffID = 2,
                    Instructions = "Ensure all sharp tools and materials are logged and accounted for.",
                    IsActive = true
                },
                new()
                {
                    TimetableID = 5,
                    ActivityName = "Afternoon Yard Exercise & Sports",
                    DayOfWeek = null,
                    StartTime = new TimeSpan(16, 0, 0),
                    EndTime = new TimeSpan(17, 30, 0),
                    BarrackID = 1,
                    ResponsibleStaffID = 2,
                    Instructions = "Guards stationed at high watchtowers during courtyard exercise.",
                    IsActive = true
                },
                new()
                {
                    TimetableID = 6,
                    ActivityName = "Evening Barrack Lockup & Night Count",
                    DayOfWeek = null,
                    StartTime = new TimeSpan(18, 30, 0),
                    EndTime = new TimeSpan(19, 30, 0),
                    BarrackID = null,
                    ResponsibleStaffID = 2,
                    Instructions = "Master headcount verified and certified to Superintendent.",
                    IsActive = true
                }
            };
            context.JailTimetable.AddRange(activities);
            context.SaveChanges();
        }

        // 9. GateRegister
        if (!context.GateRegister.Any())
        {
            var now = DateTime.Now;
            var entries = new List<GateRegister>
            {
                new()
                {
                    EntryID = 1,
                    VisitorName = "Advocate Muhammad Riaz",
                    VisitType = "Official Legal Counsel",
                    VisitPurpose = "Case consultation with Inmate Tariq Jameel (CR-9042)",
                    VehicleNumber = "ABC-8921",
                    NumberOfPeople = 1,
                    EntryTime = new DateTime(now.Year, now.Month, now.Day, 9, 15, 0),
                    ExitTime = null // Currently inside!
                },
                new()
                {
                    EntryID = 2,
                    VisitorName = "Sindh Police Escort Squad (ASI Farhan)",
                    VisitType = "Court Escort Van",
                    VisitPurpose = "Judicial remand transfer of prisoners to City Courts",
                    VehicleNumber = "SP-POL-4412",
                    NumberOfPeople = 4,
                    EntryTime = new DateTime(now.Year, now.Month, now.Day, 8, 45, 0),
                    ExitTime = null // Currently inside!
                },
                new()
                {
                    EntryID = 3,
                    VisitorName = "Civil Hospital Medical Team",
                    VisitType = "Official Health Inspection",
                    VisitPurpose = "Bi-weekly general health checkup and hygiene audit",
                    VehicleNumber = "KHI-3310",
                    NumberOfPeople = 2,
                    EntryTime = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0),
                    ExitTime = null // Currently inside!
                },
                new()
                {
                    EntryID = 4,
                    VisitorName = "Nasreen Bibi & Family",
                    VisitType = "Family Visit",
                    VisitPurpose = "Authorized weekly meeting with Inmate Mansoor Ali",
                    VehicleNumber = null,
                    NumberOfPeople = 3,
                    EntryTime = new DateTime(now.Year, now.Month, now.Day, 7, 30, 0),
                    ExitTime = new DateTime(now.Year, now.Month, now.Day, 8, 30, 0) // Departed
                }
            };
            context.GateRegister.AddRange(entries);
            context.SaveChanges();
        }
    }
}
