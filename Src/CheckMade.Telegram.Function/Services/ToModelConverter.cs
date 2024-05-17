using CheckMade.Common.FpExt.MonadicWrappers;
using CheckMade.Common.Utils;
using CheckMade.Telegram.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CheckMade.Telegram.Function.Services;

public interface IToModelConverter
{
    Task<InputMessage> ConvertMessageOrThrowAsync(Message telegramInputMessage);
}

internal class ToModelConverter(ITelegramFilePathResolver filePathResolver) : IToModelConverter
{
    public async Task<InputMessage> ConvertMessageOrThrowAsync(Message telegramInputMessage) => 
        (await ConvertMessageAsync(telegramInputMessage)).Match(
            modelInputMessage => modelInputMessage, 
            error => throw new ToModelConversionException(
                $"Failed to convert Telegram Message to Model: {error}"));

    private async Task<Result<InputMessage>> ConvertMessageAsync(Message telegramInputMessage)
    {
        var userId = telegramInputMessage.From?.Id; 
                     
        if (userId == null)
            return Result<InputMessage>.FromError("User Id (From.Id in the input message) must not be null");

        return await GetAttachmentDetails(telegramInputMessage).Match<Task<Result<InputMessage>>>(
            async attachmentDetails =>
            {
                if (string.IsNullOrWhiteSpace(telegramInputMessage.Text) && attachmentDetails.FileId.IsNone)
                {
                    return Result<InputMessage>.FromError(
                        "A valid message must either have a text or an attachment - both must not be null/empty");
                }

                var telegramAttachmentUrl = await attachmentDetails.FileId.Match<Task<Option<string>>>(
                    async value => await filePathResolver.GetTelegramFilePathAsync(value),
                    () => Task.FromResult(Option<string>.None()));

                var messageText = !string.IsNullOrWhiteSpace(telegramInputMessage.Text)
                    ? telegramInputMessage.Text
                    : telegramInputMessage.Caption;

                return Result<InputMessage>.FromSuccess(
                    new InputMessage(userId.Value,
                        telegramInputMessage.Chat.Id,
                        new MessageDetails(
                            telegramInputMessage.Date,
                            !string.IsNullOrWhiteSpace(messageText) ? messageText : Option<string>.None(),
                            telegramAttachmentUrl,
                            attachmentDetails.Type)));
            },
            error => Task.FromResult(Result<InputMessage>.FromError(error))
        );
    }
    
    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
    private static Result<AttachmentDetails> GetAttachmentDetails(Message telegramInputMessage)
    {
        // These stay proper Exceptions b/c they'd represent totally unexpected behaviour from an external library!
        const string errorMessage = "For Telegram message of type {0} we expect the {0} property to not be null";

        return telegramInputMessage.Type switch
        {
            MessageType.Text => Result<AttachmentDetails>.FromSuccess(
                new AttachmentDetails(Option<string>.None(), Option<AttachmentType>.None())),
            
            MessageType.Audio => Result<AttachmentDetails>.FromSuccess(
                new AttachmentDetails(telegramInputMessage.Audio?.FileId 
                                      ?? throw new InvalidOperationException(
                                          string.Format(errorMessage, telegramInputMessage.Type)), 
                    AttachmentType.Audio)),
            
            MessageType.Photo => Result<AttachmentDetails>.FromSuccess(
                new AttachmentDetails(telegramInputMessage.Photo?.OrderBy(p => p.FileSize).Last().FileId
                                       ?? throw new InvalidOperationException(
                                           string.Format(errorMessage, telegramInputMessage.Type)), 
                    AttachmentType.Photo)),
            
            MessageType.Document => Result<AttachmentDetails>.FromSuccess(
                new AttachmentDetails(telegramInputMessage.Document?.FileId
                                       ?? throw new InvalidOperationException(
                                           string.Format(errorMessage, telegramInputMessage.Type)), 
                    AttachmentType.Document)),
            
            MessageType.Video => Result<AttachmentDetails>.FromSuccess(
                new AttachmentDetails(telegramInputMessage.Video?.FileId
                                       ?? throw new InvalidOperationException(
                                           string.Format(errorMessage, telegramInputMessage.Type)),
                AttachmentType.Video)),
            
            _ => Result<AttachmentDetails>.FromError(
                $"Attachment type {telegramInputMessage.Type} is not yet supported!") 
        };
    }

    private record AttachmentDetails(Option<string> FileId, Option<AttachmentType> Type);
}
