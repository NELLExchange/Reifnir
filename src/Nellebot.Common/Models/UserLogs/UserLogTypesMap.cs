using System;
using System.Collections.Generic;

namespace Nellebot.Common.Models.UserLogs;

public static class UserLogTypesMap
{
    public static readonly IDictionary<UserLogType, Type> TypeDictionary = new Dictionary<UserLogType, Type>
    {
        { UserLogType.Unknown, typeof(object) },
        { UserLogType.UsernameChange, typeof(string) },
        { UserLogType.NicknameChange, typeof(string) },
        { UserLogType.AvatarHashChange, typeof(string) },
        { UserLogType.GuildAvatarHashChange, typeof(string) },
        { UserLogType.JoinedServer, typeof(DateTime) },
        { UserLogType.LeftServer, typeof(DateTime) },
    };
}
