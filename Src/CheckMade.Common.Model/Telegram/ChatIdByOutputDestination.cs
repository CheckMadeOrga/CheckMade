using CheckMade.Common.Model.Telegram.Updates;

namespace CheckMade.Common.Model.Telegram;

public record ChatIdByOutputDestination(TelegramOutputDestination OutputDestination, TelegramChatId ChatId);