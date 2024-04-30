using CheckMade.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CheckMade.Chat.Telegram;

public class UpdateService(ITelegramBotClient botClient,
    IRequestProcessor requestProcessor,
    ILogger<UpdateService> logger)
{
    internal async Task EchoAsync(Update update)
    {
        logger.LogDebug("DebugTest Message");
        logger.LogInformation("Invoke telegram update function");

        if (update.Message is not { } inputMessage) return;

        logger.LogInformation("Received Message from {ChatId}", inputMessage.Chat.Id);

        var outputMessage = string.Empty;
        
        if (!string.IsNullOrWhiteSpace(inputMessage.Text))
        {
            outputMessage = requestProcessor.Echo(inputMessage.Chat.Id, inputMessage.Text);
        }
        
        await botClient.SendTextMessageAsync(
            chatId: inputMessage.Chat.Id,
            text: outputMessage);
    }
}

