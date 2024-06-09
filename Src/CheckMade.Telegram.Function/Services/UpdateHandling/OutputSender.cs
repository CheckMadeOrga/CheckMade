using CheckMade.Common.Interfaces.ExternalServices.AzureServices;
using CheckMade.Common.Model;
using CheckMade.Common.Model.Telegram;
using CheckMade.Common.Model.Telegram.Updates;
using CheckMade.Common.Utils.UiTranslation;
using CheckMade.Telegram.Function.Services.BotClient;
using CheckMade.Telegram.Function.Services.Conversion;
using CheckMade.Telegram.Model.DTOs;
using Telegram.Bot.Types;

namespace CheckMade.Telegram.Function.Services.UpdateHandling;

internal static class OutputSender
{
        internal static async Task<Unit> SendOutputsAsync(
            IReadOnlyList<OutputDto> outputs,
            IDictionary<BotType, IBotClientWrapper> botClientByBotType,
            BotType currentlyReceivingBotType,
            ChatId currentlyReceivingChatId,
            IDictionary<TelegramPort, Role> roleByTelegramPort,
            IUiTranslator uiTranslator,
            IOutputToReplyMarkupConverter converter,
            IBlobLoader blobLoader)
    {
        Func<IReadOnlyCollection<OutputDto>, Task> sendOutputsInSeriesAndOriginalOrder 
            = async outputsPerPort =>
        {
            foreach (var output in outputsPerPort)
            {
                var portBotClient = output.LogicalPort.Match(
                    logicalPort => botClientByBotType[logicalPort.BotType],
                    () => botClientByBotType[currentlyReceivingBotType]); // e.g. for a virgin, pre-auth update

                var portChatId = output.LogicalPort.Match(
                    logicalPort => roleByTelegramPort
                        .First(kvp => kvp.Value == logicalPort.Role).Key.ChatId.Id,
                    () => currentlyReceivingChatId); // e.g. for a virgin, pre-auth update
                    
                switch (output)
                {
                    case { Text.IsSome: true, Attachments.IsSome: false, Location.IsSome: false }:
                        await InvokeSendTextMessageAsync(output.Text.GetValueOrThrow());
                        break;

                    case { Attachments.IsSome: true }:
                        if (output.Text.IsSome)
                            await InvokeSendTextMessageAsync(output.Text.GetValueOrThrow());
                        foreach (var attachment in output.Attachments.GetValueOrThrow())
                            await InvokeSendAttachmentAsync(attachment);
                        break;

                    case { Location.IsSome: true }:
                        await InvokeSendLocationAsync(output.Location.GetValueOrThrow());
                        break;
                }

                continue;

                async Task InvokeSendTextMessageAsync(UiString outputText)
                {
                    await portBotClient
                        .SendTextMessageAsync(
                            portChatId,
                            uiTranslator.Translate(Ui("Please choose:")),
                            uiTranslator.Translate(outputText),
                            converter.GetReplyMarkup(output));
                }

                async Task InvokeSendAttachmentAsync(OutputAttachmentDetails details)
                {
                    var (blobData, fileName) =
                        await blobLoader.DownloadBlobAsync(details.AttachmentUri);
                    var fileStream = new InputFileStream(blobData, fileName);
                    
                    var caption = details.Caption.Match(
                        value => Option<string>.Some(uiTranslator.Translate(value)),
                        Option<string>.None);

                    var attachmentSendOutParams = new AttachmentSendOutParameters(
                        portChatId,
                        fileStream,
                        caption,
                        converter.GetReplyMarkup(output)
                    );

                    switch (details.AttachmentType)
                    {
                        case AttachmentType.Document:
                            await portBotClient.SendDocumentAsync(attachmentSendOutParams);
                            break;

                        case AttachmentType.Photo:
                            await portBotClient.SendPhotoAsync(attachmentSendOutParams);
                            break;

                        case AttachmentType.Voice:
                            await portBotClient.SendVoiceAsync(attachmentSendOutParams);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(details.AttachmentType));
                    }
                }

                async Task InvokeSendLocationAsync(Geo location)
                {
                    await portBotClient
                        .SendLocationAsync(
                            portChatId,
                            location,
                            converter.GetReplyMarkup(output));
                }
            }
        };

        var outputGroups = outputs.GroupBy(o => o.LogicalPort);

        var parallelTasks = outputGroups
            .Select(outputsPerLogicalPortGroup => 
                sendOutputsInSeriesAndOriginalOrder.Invoke(outputsPerLogicalPortGroup.ToList().AsReadOnly()));
        
        /* 1) Waits for all parallel executing tasks (generated by .Select()), to complete
         * 2) The 'await' unwraps the resulting aggregate Task object and rethrows any Exceptions */
        await Task.WhenAll(parallelTasks);
        
        return Unit.Value;
    }
}