namespace MeetingManagementSystem.Core.Constants;

/// <summary>
/// Defines the authorization policy names used in the system
/// </summary>
public static class Policies
{
    public const string AdministratorOnly = "AdministratorOnly";
    public const string GovernmentOfficialOnly = "GovernmentOfficialOnly";
    public const string ParticipantAccess = "ParticipantAccess";
    public const string MeetingOrganizer = "MeetingOrganizer";
    public const string RoomManagement = "RoomManagement";
    public const string UserManagement = "UserManagement";
    public const string ReportAccess = "ReportAccess";
}
