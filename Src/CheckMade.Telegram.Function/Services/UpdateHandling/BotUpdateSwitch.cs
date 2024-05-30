using CheckMade.Common.LangExt;
using CheckMade.Telegram.Model;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CheckMade.Telegram.Function.Services.UpdateHandling;

public interface IBotUpdateSwitch
{
    Task<Attempt<Unit>> HandleUpdateAsync(Update update, BotType botType);
}

public class BotUpdateSwitch(IMessageHandler messageHandler, ILogger<BotUpdateSwitch> logger) : IBotUpdateSwitch
{
    internal static readonly UiString
        NoSpecialHandlingWarning = Ui("Telegram Message/Update of this type not yet supported. " +
                                          "No special handling is taking place for it, but that doesn't mean that a " +
                                          "Telegram-System-related update didn't work. You may assume it did.");
    
    public async Task<Attempt<Unit>> HandleUpdateAsync(Update update, BotType botType)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (update.Type)
        {
            case UpdateType.Message:
            case UpdateType.EditedMessage:
            case UpdateType.CallbackQuery:
                return await messageHandler.HandleMessageAsync(new UpdateWrapper(update), botType);

            case UpdateType.MyChatMember:
                logger.LogInformation("MyChatMember Update from '{From}', with previous status '{OldStatus}' " +
                                      "and new status '{NewStatus}'",
                    update.MyChatMember!.From.Username, update.MyChatMember.OldChatMember.Status, 
                    update.MyChatMember.NewChatMember.Status);
                return Unit.Value;
            
            default:
                logger.LogWarning("Received update of type '{updateType}': {warningMessage}", 
                    update.Type, NoSpecialHandlingWarning.GetFormattedEnglish());
                return Unit.Value;
        }
    }
}
