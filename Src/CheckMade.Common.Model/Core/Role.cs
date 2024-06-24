using CheckMade.Common.Model.Core.Interfaces;
using CheckMade.Common.Model.Utils;

namespace CheckMade.Common.Model.Core;

public record Role(
        string Token,
        RoleType RoleType,
        User User,
        ILiveEventInfo LiveEventInfo,
        DbRecordStatus Status = DbRecordStatus.Active)
    : IRoleInfo;