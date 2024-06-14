using CheckMade.Common.Model.ChatBot.Input;

namespace CheckMade.DevOps.TlgInputDetailsMigration.Helpers;

internal record DetailsUpdate(
    TlgUserId UserId, 
    DateTime TelegramDate, 
    string NewDetails);