namespace PrisonManagementSystem.Authorization;

public static class RoleConstants
{
    public const string JailSuperintendent = "Jail Superintendent";
    public const string TimetableOfficer = "Timetable Officer";
    public const string Gatekeeper = "Gatekeeper";
    public const string CourtRecordsOfficer = "Court Records Officer";
    public const string PrisonerRecordsOfficer = "Prisoner Records Officer";
    public const string SystemAdministrator = "System Administrator";

    public static readonly Dictionary<string, (string Controller, string Action, string PageName, string Initials)> RolePageMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [JailSuperintendent] = ("Dashboard", "Index", "Dashboard", "SU"),
            [TimetableOfficer] = ("DailyTimetable", "Index", "Daily Timetable", "TO"),
            [Gatekeeper] = ("GateRegister", "Index", "Gate Register", "GK"),
            [CourtRecordsOfficer] = ("CourtHearings", "Index", "Court Hearings", "CO"),
            [PrisonerRecordsOfficer] = ("Prisoners", "Index", "Prisoners", "PR"),
            [SystemAdministrator] = ("StaffAccounts", "Index", "Staff & Accounts", "SU")
        };

    public static string? GetAssignedController(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return null;
        if (RolePageMap.TryGetValue(roleName, out var mapping))
        {
            return mapping.Controller;
        }
        return null;
    }

    public static string GetInitials(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return "U";
        if (RolePageMap.TryGetValue(roleName, out var mapping))
        {
            return mapping.Initials;
        }
        var parts = roleName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
    }
}
