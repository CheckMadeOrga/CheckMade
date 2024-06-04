
namespace CheckMade.Common.Model.Telegram.Updates;

public record TelegramUpdateDetails(
    DateTime TelegramDate,
    int TelegramMessageId,
    Option<string> Text,
    Option<string> AttachmentExternalUrl,
    Option<AttachmentType> AttachmentType,
    Option<Geo> GeoCoordinates,
    Option<int> BotCommandEnumCode,
    Option<int> DomainCategoryEnumCode,
    Option<long> ControlPromptEnumCode);