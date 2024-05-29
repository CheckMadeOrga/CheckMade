namespace CheckMade.Telegram.Model.ControlPrompt;

public record ControlPromptCallbackId
{
    public string Id { get; }

    public ControlPromptCallbackId(int id)
    {
        if(id < 1)
        {
            throw new ArgumentException("ID must be a positive integer.");
        }
        
        Id = id.ToString();
    }
}