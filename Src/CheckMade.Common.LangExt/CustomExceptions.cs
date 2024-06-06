namespace CheckMade.Common.LangExt;

public class DataAccessException : Exception
{
    public DataAccessException() { }

    public DataAccessException(string? message = null, Exception? innerException = null) 
        : base(message, innerException) { }
}

public class TelegramBotClientCallException(string? message = null, Exception? innerException = null) 
    : Exception(message, innerException);

