namespace CheckMade.Telegram.Model;

public record TelegramChatId(long Id)
{
    public static implicit operator long(TelegramChatId userId) => userId.Id;
    public static implicit operator TelegramChatId(long id) => new TelegramChatId(id);
}