using CheckMade.Common.Model.Utils;

namespace CheckMade.Common.Model.Core.LiveEvents.Concrete;

public record LiveEventVenue(
    string Name,
    DbRecordStatus Status = DbRecordStatus.Active);