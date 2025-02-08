using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nellebot.Common.Models;
using Npgsql;

namespace Nellebot.Data.Repositories;

public class MessageRefRepository
{
    private readonly BotDbContext _dbContext;
    private readonly ILogger<MessageRefRepository> _logger;

    public MessageRefRepository(BotDbContext dbContext, ILogger<MessageRefRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CreateMessageRef(ulong messageId, ulong channelId, ulong userId)
    {
        try
        {
            var messageRef = new MessageRef
            {
                MessageId = messageId,
                ChannelId = channelId,
                UserId = userId,
                DateTime = DateTime.UtcNow,
            };

            await _dbContext.AddAsync(messageRef);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException != null
                && ex.InnerException is PostgresException pgEx
                && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogDebug($"{nameof(CreateMessageRef)}: MessageRef for MessageId {messageId} already exists");
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<MessageRef?> GetMessageRef(ulong messageId)
    {
        return await _dbContext.MessageRefs.FindAsync(messageId);
    }

    public async Task<IEnumerable<MessageRef>> GetMessageRefs(ulong[] messageIds)
    {
        return await _dbContext.MessageRefs.Where(mr => messageIds.Contains(mr.MessageId)).ToListAsync();
    }

    public async Task<bool> CreateMessageRefIfNotExists(ulong messageId, ulong channelId, ulong userId)
    {
        MessageRef? messageRef = await GetMessageRef(messageId);

        if (messageRef != null) return false;

        await CreateMessageRef(messageId, channelId, userId);

        return true;
    }
}
