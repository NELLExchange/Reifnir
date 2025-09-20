namespace Nellebot.Common.Models.UserLogs;

public enum UserLogType
{
    Unknown = 0,
    UsernameChange = 1,
    NicknameChange = 2,
    JoinedServer = 5,
    LeftServer = 6,
    Quarantined = 7,
    Approved = 8,
}
