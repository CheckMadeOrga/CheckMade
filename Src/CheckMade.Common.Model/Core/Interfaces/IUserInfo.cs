using CheckMade.Common.Model.Core.Structs;
using CheckMade.Common.Model.Utils;

namespace CheckMade.Common.Model.Core.Interfaces;

public interface IUserInfo
{
    MobileNumber Mobile { get; }
    string FirstName { get; }
    Option<string> MiddleName { get; }
    string LastName { get; }
    Option<EmailAddress> Email { get; }
    LanguageCode Language { get; }
    DbRecordStatus Status { get; }
}