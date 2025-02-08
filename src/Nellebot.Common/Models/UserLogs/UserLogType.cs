namespace Nellebot.Common.Models.UserLogs;

public enum UserLogType
{
    Unknown = 0,
    UsernameChange = 1,
    NicknameChange = 2,
    AvatarHashChange = 3,
    GuildAvatarHashChange = 4,
    JoinedServer = 5,
    LeftServer = 6,
}
