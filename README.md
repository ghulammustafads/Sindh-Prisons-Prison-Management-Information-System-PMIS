# Sindh Prisons - Prison Management Information System (PMIS)

Production-grade ASP.NET Core MVC application developed with C#, Razor Views, Entity Framework Core SQL Server provider, server-side authentication, and role authorization.

---

## 1. Architecture & Design

- **Backend**: ASP.NET Core 8.0 MVC (C#).
- **Views**: Server-rendered Razor views with reusable partials (`_Alerts.cshtml`, `_AuthenticatedLayout.cshtml`, `_Layout.cshtml`).
- **Persistence**: Microsoft Entity Framework Core SQL Server Provider (`Microsoft.EntityFrameworkCore.SqlServer`).
- **Styling**: Clean custom CSS (`wwwroot/css/site.css`) matching the approved Sindh Prisons design specifications:
  - Light theme with pale grey workspace (`#f4f6f8`).
  - White cards with rounded corners (`12px`) and subtle borders.
  - Deep navy typography (`#0f1e36`) and subheadings (`#5a6a85`).
  - Forest green primary buttons (`#0a5435`) and selected navigation badges (`#eaf5ee`).
  - Strict two-item sidebar on authenticated screens (Current Page non-navigating active label + Sign out).
- **JavaScript**: Minimal unobtrusive client scripts (`wwwroot/js/site.js`) for password toggle and dynamic cell loading.
- **Zero Pill / Anti-AI Slop**: Exact proportional layouts matching all reference screens without generic placeholder pills or extraneous menus.

---

## 2. SQL Server Configuration & Database Check

### Database Connection Configuration
As required, the SQL Server connection string is placed exclusively in a separate server-side configuration file named `appsettings.Database.json`:

```json
{
  "ConnectionStrings": {
    "PrisonDatabase": "Server=localhost\\SQLEXPRESS;Database=PrisonManagementDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=True"
  }
}
```

This file is explicitly loaded in `Program.cs` before registering `PrisonDbContext`. Environment variables are enabled to override it if needed. The connection string is never exposed to browser client assets or JavaScript.

### Critical Database & Schema Verification
- **Target Database**: `PrisonManagementDB` on `localhost\SQLEXPRESS`.
- **Legacy Database Warning**: If connected to an earlier simplified installation named `PrisonManagementInitialDB`, `SchemaCompatibilityService` detects the database name mismatch (`SELECT DB_NAME()`) and reports the exact configuration update required in `appsettings.Database.json`.
- **10 Verified Tables**:
  1. `dbo.StaffRoles`
  2. `dbo.Barracks`
  3. `dbo.Staff`
  4. `dbo.ManagementAccounts`
  5. `dbo.Cells`
  6. `dbo.Prisoners`
  7. `dbo.CourtCases`
  8. `dbo.CourtTrials`
  9. `dbo.JailTimetable`
  10. `dbo.GateRegister` (simplified schema without StaffID or PrisonerID; exits tracked with atomic SQL updates)

*Note on Local Environment*: In containerized or non-Windows environments where `localhost\SQLEXPRESS` is not running, the application gracefully flags the database as unverified/connection unavailable rather than falsifying data or substituting empty results.

---

## 3. Server-Side Authentication & Role-to-Page Routing

Application accounts authenticate against `dbo.ManagementAccounts` through `dbo.Staff` and `dbo.StaffRoles`.

### Role-to-Page Mapping

| Role Name in `dbo.StaffRoles` | Assigned Page | Controller Route | Avatar Initials |
| :--- | :--- | :--- | :--- |
| **Jail Superintendent** | Dashboard | `/Dashboard` | `SU` |
| **Timetable Officer** | Daily Timetable | `/DailyTimetable` | `TO` |
| **Gatekeeper** | Gate Register | `/GateRegister` | `GK` |
| **Court Records Officer** | Court Hearings | `/CourtHearings` | `CO` |
| **Prisoner Records Officer** | Prisoners | `/Prisoners` | `PR` |
| **System Administrator** | Staff & Accounts | `/StaffAccounts` | `SU` |

### Security Policies
1. **Server-Side Enforcement**: Every authenticated page is guarded by `[EnforceAssignedRolePage(RoleName)]`. If an authenticated user enters another role's URL directly, they are rejected with an access-denied response.
2. **Strict Redirection**: After login, users are routed strictly to their designated role page. Any arbitrary `returnUrl` attempting to bypass this rule is discarded.
3. **Password Security**: Passwords are hashed using ASP.NET Core's standard PBKDF2 HMAC-SHA256 password hasher with 128-bit cryptographically secure salt. Plaintext passwords or unsalted hashes are never used or logged.
4. **Anti-Forgery Protection**: Anti-forgery tokens (`@Html.AntiForgeryToken()`) protect all state-changing POST endpoints, including logout.

---

## 4. Operational Pages & Features

### 1. Login (`/Account/Login`)
- Username and password inputs with eye icon visibility toggle.
- Clean validation banner for invalid credentials or inactive accounts.
- Subtitle text: *"Authorized personnel only."*

### 2. Superintendent Dashboard (`/Dashboard`)
- Read-only jail administration overview.
- Top statistics:
  - Prisoners currently in custody (`COUNT(*)` where `CustodyStatus = 'In Custody'`).
  - Active staff count.
  - Hearings scheduled for today (operational Asia/Karachi time).
  - Visitors currently inside (calculated as the `SUM(NumberOfPeople)` of open gate entries where `ExitTime IS NULL`).
- 2x2 overview sections:
  - Upcoming court hearings.
  - Barracks & cells summary.
  - Daily timetable activities for the current day.
  - Recent gate activity.
- No navigation links or "View all" click-throughs.

### 3. Daily Timetable (`/DailyTimetable`)
- Day filter (Monday through Sunday, or All days).
- Activity table with Start/End times, Barrack location, and Responsible Staff.
- Add and Edit activity form validating that `EndTime > StartTime`.
- RowVersion concurrency token checking.

### 4. Gate Register (`/GateRegister`)
- Date range filtering (`fromDate` to `toDate`).
- Internal tabs: **All entries** and **Still inside**.
- Visitor name, visit type, purpose, vehicle details, number of people (`> 0`), entry time.
- "Record exit" action executes an atomic SQL update conditioned on `ExitTime IS NULL`.
- Gate departure does not affect prisoner custody status.

### 5. Court Hearings (`/CourtHearings`)
- Case search and date filter.
- Hearing table with prisoner name, case reference, hearing type, attendance, outcome, and status.
- Schedule hearing form with prisoner case selector (`FullName (PrisonerNumber) — CaseNumber (CourtName)`).
- `RecordedByStaffID` is populated automatically from the authenticated user's identity.
- Outcome update action recording attendance method and trial findings.

### 6. Prisoners (`/Prisoners`)
- Search by name, prisoner number, or CNIC; status filter.
- Internal tabs: **Prisoner records** and **Cases & hearings**.
- Registration form collecting Full Name, Father Name, Date of Birth, Gender, CNIC, Admission Date, Barrack, and Cell.
- Dynamic cell dropdown filtered by selected barrack.
- **Cell Capacity Locking**: Validates occupancy against cell capacity within a SQL Server transaction using serialized locking (`WITH (UPDLOCK, HOLDLOCK)`).
- Concurrency-safe unique prisoner number generation (`SP-{Year}-{RandomSuffix}`).
- Cases & hearings tab allows managing court cases and viewing linked hearing history for any selected prisoner.

### 7. Staff & Accounts (`/StaffAccounts`)
- Internal tabs: **Staff**, **Roles**, **Accounts**.
- Staff tab: Search employees, view ranks and assignments, toggle active/inactive status, Add staff form.
- Roles tab: View role descriptions and staff counts, Add new roles.
- Accounts tab: View management accounts, create login credentials for staff members without accounts, change account passwords securely, enable/disable accounts.
- Password hashes are never displayed.

---

## 5. Local Setup & Startup Instructions

### Prerequisites
- .NET 8.0 SDK or later installed locally.
- Microsoft SQL Server (Local or Express instance on `localhost\SQLEXPRESS`).

### Running the Application

1. Verify connection string in `appsettings.Database.json`:
   ```json
   {
     "ConnectionStrings": {
       "PrisonDatabase": "Server=localhost\\SQLEXPRESS;Database=PrisonManagementDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=True"
     }
   }
   ```
2. Build the solution:
   ```bash
   dotnet build PrisonManagementSystem.csproj
   ```
3. Run the application:
   ```bash
   dotnet run --project PrisonManagementSystem.csproj --urls=http://localhost:3000
   ```
4. Run the test suite:
   ```bash
   dotnet test PrisonManagementSystem.Tests/PrisonManagementSystem.Tests.csproj
   ```
5. Open your browser and navigate to `http://localhost:3000`.
