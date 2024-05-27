using CheckMade.Common.LangExt;
using CheckMade.Telegram.Model.BotOperations;

namespace CheckMade.Telegram.Model.DTOs;

public record OutputMessageDto(
    UiString Text,
    Option<IEnumerable<BotOperation>> BotOperations,
    Option<IEnumerable<string>> PredefinedChoices);
    