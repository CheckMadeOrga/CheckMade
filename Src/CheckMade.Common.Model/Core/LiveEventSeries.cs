using CheckMade.Common.Model.Utils;

namespace CheckMade.Common.Model.Core;

public record LiveEventSeries(
    string Name,
    IReadOnlyCollection<LiveEvent> LiveEvents,
    DbRecordStatus Status = DbRecordStatus.Active);