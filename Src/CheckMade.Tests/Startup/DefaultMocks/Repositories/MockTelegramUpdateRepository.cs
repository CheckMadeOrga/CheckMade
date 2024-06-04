using CheckMade.Common.Interfaces.Persistence;
using CheckMade.Common.Model.Telegram.Updates;
using Moq;

namespace CheckMade.Tests.Startup.DefaultMocks.Repositories;

internal class MockTelegramUpdateRepository(IMock<ITelegramUpdateRepository> mockUpdateRepo) : ITelegramUpdateRepository
{
    public async Task AddOrThrowAsync(TelegramUpdate telegramUpdate)
    {
        await mockUpdateRepo.Object.AddOrThrowAsync(telegramUpdate);
    }

    public async Task AddOrThrowAsync(IEnumerable<TelegramUpdate> telegramUpdates)
    {
        await mockUpdateRepo.Object.AddOrThrowAsync(telegramUpdates);
    }

    public async Task<IEnumerable<TelegramUpdate>> GetAllOrThrowAsync()
    {
        return await mockUpdateRepo.Object.GetAllOrThrowAsync();
    }

    public async Task<IEnumerable<TelegramUpdate>> GetAllOrThrowAsync(TelegramUserId userId)
    {
        return await mockUpdateRepo.Object.GetAllOrThrowAsync(userId);
    }

    public async Task HardDeleteAllOrThrowAsync(TelegramUserId userId)
    {
        await mockUpdateRepo.Object.HardDeleteAllOrThrowAsync(userId);
    }
}