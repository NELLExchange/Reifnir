using System;
using System.Threading.Tasks;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;

namespace Nellebot.Services;

public class UserLogService
{
    private readonly SharedCache _cache;
    private readonly UserLogRepository _userLogRepo;

    public UserLogService(UserLogRepository userLogRepo, SharedCache cache)
    {
        _userLogRepo = userLogRepo;
        _cache = cache;
    }

    public Task<UserLog?> GetLatestFieldForUser(ulong userId, UserLogType logType)
    {
        string cacheKey = string.Format(SharedCacheKeys.UserLog, userId, logType);

        Task<UserLog?> userLog = _cache.LoadFromCacheAsync(
            cacheKey,
            () => _userLogRepo.GetLatestFieldForUser(userId, logType),
            TimeSpan.FromMinutes(5));

        return userLog;
    }

    public async Task CreateUserLog<T>(ulong userId, T value, UserLogType logType, ulong? responsibleUserId = null)
    {
        string cacheKey = string.Format(SharedCacheKeys.UserLog, userId, logType);

        await _userLogRepo.CreateUserLog(userId, value, logType, responsibleUserId);

        _cache.FlushCache(cacheKey);
    }
}
