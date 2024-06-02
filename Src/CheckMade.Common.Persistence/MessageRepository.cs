using System.Collections.Immutable;
using System.Data.Common;
using CheckMade.Common.Interfaces;
using CheckMade.Common.Model.TelegramUpdates;
using CheckMade.Common.Persistence.JsonHelpers;
using Npgsql;
using NpgsqlTypes;

namespace CheckMade.Common.Persistence;

public class MessageRepository(IDbExecutionHelper dbHelper) : IMessageRepository
{
    public async Task AddOrThrowAsync(TelegramUpdate telegramUpdate)
    {
        await AddOrThrowAsync(new List<TelegramUpdate> { telegramUpdate }.ToImmutableArray());
    }

    public async Task AddOrThrowAsync(IEnumerable<TelegramUpdate> inputMessages)
    {
        var commands = inputMessages.Select(inputMessage =>
        {
            var command = new NpgsqlCommand("INSERT INTO tlgr_updates " +
                                            "(user_id, chat_id, details, last_data_migration, bot_type, update_type)" +
                                            " VALUES (@telegramUserId, @telegramChatId, @telegramMessageDetails," +
                                            "@lastDataMig, @botType, @updateType)");

            command.Parameters.AddWithValue("@telegramUserId", (long) inputMessage.UserId);
            command.Parameters.AddWithValue("@telegramChatId", (long) inputMessage.TelegramChatId);
            command.Parameters.AddWithValue("@lastDataMig", 0);
            command.Parameters.AddWithValue("@botType", (int) inputMessage.BotType);
            command.Parameters.AddWithValue("@updateType", (int) inputMessage.ModelUpdateType);

            command.Parameters.Add(new NpgsqlParameter("@telegramMessageDetails", NpgsqlDbType.Jsonb)
            {
                Value = JsonHelper.SerializeToJsonOrThrow(inputMessage.Details)
            });

            return command;
        }).ToImmutableArray();

        await dbHelper.ExecuteOrThrowAsync(async (db, transaction) =>
        {
            foreach (var command in commands)
            {
                command.Connection = db;
                command.Transaction = transaction;        
                await command.ExecuteNonQueryAsync();
            }
        });
    }

    public async Task<IEnumerable<TelegramUpdate>> GetAllOrThrowAsync() =>
        await GetAllOrThrowExecuteAsync(
            "SELECT * FROM tlgr_updates",
            Option<TelegramUserId>.None());

    public async Task<IEnumerable<TelegramUpdate>> GetAllOrThrowAsync(TelegramUserId userId) =>
        await GetAllOrThrowExecuteAsync(
            "SELECT * FROM tlgr_updates WHERE user_id = @userId",
            userId);

    private async Task<IEnumerable<TelegramUpdate>> GetAllOrThrowExecuteAsync(
        string commandText, Option<TelegramUserId> userId)
    {
        var builder = ImmutableArray.CreateBuilder<TelegramUpdate>();
        var command = new NpgsqlCommand(commandText);
            
        if (userId.IsSome)
            command.Parameters.AddWithValue("@userId", (long) userId.GetValueOrDefault());

        await dbHelper.ExecuteOrThrowAsync(async (db, transaction) =>
        {
            command.Connection = db;
            command.Transaction = transaction;
            
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    builder.Add(await CreateInputMessageFromReaderStrictAsync(reader));
                }
            }
        });

        return builder.ToImmutable();
    }
    
    private static async Task<TelegramUpdate> CreateInputMessageFromReaderStrictAsync(DbDataReader reader)
    {
        TelegramUserId telegramUserId = await reader.GetFieldValueAsync<long>(reader.GetOrdinal("user_id"));
        TelegramChatId telegramChatId = await reader.GetFieldValueAsync<long>(reader.GetOrdinal("chat_id"));
        var telegramBotType = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("bot_type"));
        var telegramUpdateType = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("update_type"));
        var details = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("details"));

        var message = new TelegramUpdate(
            telegramUserId,
            telegramChatId,
            (BotType) telegramBotType,
            (ModelUpdateType) telegramUpdateType,
            JsonHelper.DeserializeFromJsonStrictOrThrow<TelegramUpdateDetails>(details) 
            ?? throw new InvalidOperationException("Failed to deserialize"));

        return message;
    }

    public async Task HardDeleteAllOrThrowAsync(TelegramUserId userId)
    {
        var command = new NpgsqlCommand("DELETE FROM tlgr_updates WHERE user_id = @userId");
        command.Parameters.AddWithValue("@userId", (long) userId);

        await dbHelper.ExecuteOrThrowAsync(async (db, transaction) =>
        {
            command.Connection = db;
            command.Transaction = transaction;
            await command.ExecuteNonQueryAsync();
        });
    }
}