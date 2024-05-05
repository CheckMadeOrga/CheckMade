using System.ComponentModel;
using CheckMade.Chat.Logic;
using CheckMade.Common.Utils;
using Telegram.Bot;

namespace CheckMade.Chat.Telegram.Startup;

public interface ITelegramBotClientFactory
{
    ITelegramBotClient CreateBotClient(BotType botType);
}

public class TelegramBotClientFactory(IHttpClientFactory httpFactory, BotTokens botTokens) 
    : ITelegramBotClientFactory
{
    public ITelegramBotClient CreateBotClient(BotType botType) => botType switch
    {
        BotType.Submissions => new TelegramBotClient(botTokens.TelegramSubmissionsBotToken,
            httpFactory.CreateClient($"CheckMade{botType}Bot")),
        
        BotType.Communications => new TelegramBotClient(botTokens.TelegramCommunicationsBotToken,
            httpFactory.CreateClient($"CheckMade{botType}Bot")),
        
        BotType.Notifications => new TelegramBotClient(botTokens.TelegramNotificationsBotToken,
            httpFactory.CreateClient($"CheckMade{botType}Bot")),
        
        _ => throw new InvalidEnumArgumentException()
    };
}
