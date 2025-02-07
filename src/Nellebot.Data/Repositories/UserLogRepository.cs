using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models.UserLogs;

namespace Nellebot.Data.Repositories;

public class UserLogRepository
{
    private readonly BotDbContext _dbContext;

    public UserLogRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserLog?> GetLatestFieldForUser(ulong userId, UserLogType logType)
    {
        UserLog? result = await _dbContext.UserLogs
            .Where(u => u.UserId == userId && u.LogType == logType)
            .OrderByDescending(u => u.Timestamp)
            .FirstOrDefaultAsync();
        return result;
    }

    public async Task<List<UserLog>> GetLatestFieldsForUser(ulong userId)
    {
        List<UserLog> result = (await _dbContext.UserLogs
                .Where(u => u.UserId == userId)
                .GroupBy(u => u.LogType)
                .Select(g => g.OrderByDescending(g => g.Timestamp).FirstOrDefault())
                .ToListAsync())
            .Where(u => u != null)
            .Cast<UserLog>()
            .ToList();

        return result;
    }

    public async Task CreateUserLog<T>(ulong userId, T value, UserLogType logType, ulong? responsibleUserId = null)
    {
        Type typeForField = UserLogTypesMap.TypeDictionary[logType];

        UserLog userLog = new UserLog
        {
            LogType = logType,
            ResponsibleUserId = responsibleUserId,
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            ValueType = typeForField,
        }.WithValue(value);

        await _dbContext.AddAsync(userLog);
        await _dbContext.SaveChangesAsync();
    }
}
