using CheckMade.Common.Model.Core.Interfaces;
using CheckMade.Common.Model.Core.Structs;
using CheckMade.Common.Model.Utils;

namespace CheckMade.Common.Model.Core;

public record User(
    MobileNumber Mobile,
    string FirstName,
    Option<string> MiddleName,
    string LastName,
    Option<EmailAddress> Email,
    LanguageCode Language,
    IReadOnlyCollection<IRoleInfo> HasRoles,
    // Option<Vendor> CurrentlyWorksFor,
    DbRecordStatus Status = DbRecordStatus.Active)
    : IUserInfo
{
    public User(IUserInfo userInfo, IReadOnlyCollection<IRoleInfo> roles) 
        : this(
            userInfo.Mobile,
            userInfo.FirstName,
            userInfo.MiddleName,
            userInfo.LastName,
            userInfo.Email,
            userInfo.Language,
            roles,
            userInfo.Status)
    {
    }
}