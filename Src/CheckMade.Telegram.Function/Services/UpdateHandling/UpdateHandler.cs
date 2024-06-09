using CheckMade.Common.Interfaces.ExternalServices.AzureServices;
using CheckMade.Common.Interfaces.Persistence.Tlg;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Core.Enums.UserInteraction;
using CheckMade.Common.Model.Tlg;
using CheckMade.Common.Model.Tlg.Input;
using CheckMade.Common.Model.Tlg.Output;
using CheckMade.Common.Utils.UiTranslation;
using CheckMade.Telegram.Function.Services.BotClient;
using CheckMade.Telegram.Function.Services.Conversion;
using CheckMade.Telegram.Function.Startup;
using CheckMade.Telegram.Logic.InputProcessors;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CheckMade.Telegram.Function.Services.UpdateHandling;

public interface IUpdateHandler
{
    Task<Attempt<Unit>> HandleUpdateAsync(UpdateWrapper update, InteractionMode interactionMode);
}

public class UpdateHandler(
        IBotClientFactory botClientFactory,
        IInputProcessorSelector selector,
        ITlgClientPortToRoleMapRepository tlgClientPortToRoleMapRepository,
        IToModelConverterFactory toModelConverterFactory,
        DefaultUiLanguageCodeProvider defaultUiLanguage,
        IUiTranslatorFactory translatorFactory,
        IOutputToReplyMarkupConverterFactory replyMarkupConverterFactory,
        IBlobLoader blobLoader,
        ILogger<UpdateHandler> logger)
    : IUpdateHandler
{
    public async Task<Attempt<Unit>> HandleUpdateAsync(UpdateWrapper update, InteractionMode currentlyReceivingInteractionMode)
    {
        ChatId currentlyReceivingChatId = update.Message.Chat.Id;
        
        logger.LogTrace("Invoked telegram update function for InteractionMode: {interactionMode} " +
                              "with Message from UserId/ChatId: {userId}/{chatId}", 
            currentlyReceivingInteractionMode, update.Message.From?.Id ?? 0, currentlyReceivingChatId);

        var handledMessageTypes = new[]
        {
            MessageType.Document,
            MessageType.Location,
            MessageType.Photo,
            MessageType.Text,
            MessageType.Voice
        };

        if (!handledMessageTypes.Contains(update.Message.Type))
        {
            logger.LogWarning("Received message of type '{messageType}': {warning}", 
                update.Message.Type, BotUpdateSwitch.NoSpecialHandlingWarning.GetFormattedEnglish());

            return Unit.Value;
        }

        var botClientByMode = new Dictionary<InteractionMode, IBotClientWrapper>
        {
            { InteractionMode.Operations,  botClientFactory.CreateBotClient(InteractionMode.Operations) },
            { InteractionMode.Communications, botClientFactory.CreateBotClient(InteractionMode.Communications) },
            { InteractionMode.Notifications, botClientFactory.CreateBotClient(InteractionMode.Notifications) }
        };
        
        var filePathResolver = new TelegramFilePathResolver(botClientByMode[currentlyReceivingInteractionMode]);
        var toModelConverter = toModelConverterFactory.Create(filePathResolver);
        var uiTranslator = translatorFactory.Create(GetUiLanguage(update.Message));
        var replyMarkupConverter = replyMarkupConverterFactory.Create(uiTranslator);

        var sendOutputsAttempt = await
            (from tlgInput
                    in Attempt<Result<TlgInput>>.RunAsync(() => 
                        toModelConverter.ConvertToModelAsync(update, currentlyReceivingInteractionMode))
                from outputs
                    in Attempt<IReadOnlyList<OutputDto>>.RunAsync(() => 
                        selector.GetInputProcessor(currentlyReceivingInteractionMode).ProcessInputAsync(tlgInput))
                from roleByTlgClientPort
                    in Attempt<IDictionary<TlgClientPort, Role>>.RunAsync(
                        GetRoleByTlgClientPortAsync) 
                from unit
                  in Attempt<Unit>.RunAsync(() => 
                      OutputSender.SendOutputsAsync(
                          outputs, botClientByMode, currentlyReceivingInteractionMode, currentlyReceivingChatId,
                          roleByTlgClientPort, uiTranslator, replyMarkupConverter, blobLoader)) 
                select unit);
        
        return sendOutputsAttempt.Match(
            
            _ => Attempt<Unit>.Succeed(Unit.Value),

            ex =>
            {
                logger.LogError(ex, "Exception with message '{exMessage}' was thrown. " +
                                    "Next, some details to help debug the current exception. " +
                                    "InteractionMode: '{interactionMode}'; Telegram User Id: '{userId}'; " +
                                    "DateTime of received Update: '{telegramDate}'; with text: '{text}'",
                    ex.Message, currentlyReceivingInteractionMode, update.Message.From!.Id,
                    update.Message.Date, update.Message.Text);
                
                return ex;
            });
    }

    private LanguageCode GetUiLanguage(Message telegramInputMessage)
    {
        var userLanguagePreferenceIsRecognized = Enum.TryParse(
            typeof(LanguageCode),
            telegramInputMessage.From?.LanguageCode,
            true,
            out var userLanguagePreference);
        
        return userLanguagePreferenceIsRecognized
            ? (LanguageCode) userLanguagePreference!
            : defaultUiLanguage.Code;
    }
    
    private async Task<IDictionary<TlgClientPort, Role>> GetRoleByTlgClientPortAsync() =>
        (await tlgClientPortToRoleMapRepository.GetAllAsync())
            .ToDictionary(
                keySelector: map => map.ClientPort,
                elementSelector: map => map.Role);
}