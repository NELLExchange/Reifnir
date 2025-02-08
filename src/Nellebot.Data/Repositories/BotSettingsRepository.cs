using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;

namespace Nellebot.Data.Repositories;

public class BotSettingsRepository
{
    private readonly BotDbContext _dbContext;

    public BotSettingsRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetBotSetting(string key)
    {
        BotSettting? setting = await _dbContext.GuildSettings
            .SingleOrDefaultAsync(x => x.Key == key);

        return setting?.Value;
    }

    public async Task<BotSettting> SaveBotSetting(string key, string value)
    {
        BotSettting? setting = await _dbContext.GuildSettings
            .SingleOrDefaultAsync(x => x.Key == key);

        if (setting == null)
        {
            setting = new BotSettting
            {
                Key = key,
                Value = value,
            };

            await _dbContext.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
        }

        await _dbContext.SaveChangesAsync();

        return setting;
    }
}
