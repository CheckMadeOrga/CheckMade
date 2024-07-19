using CheckMade.Common.Model.Core.Actors.RoleSystem;
using CheckMade.Common.Model.Core.LiveEvents;
using CheckMade.Common.Model.Core.Trades.Concrete.Types;

namespace CheckMade.Common.Model.Core.Trades.Concrete.SubDomains.SaniClean.Issues;

public record ConsumablesIssue(
        DateTime CreationDate,
        ISphereOfAction Sphere,
        Option<ITradeFacility<SaniCleanTrade>> Facility,
        Geo Location,
        IssueEvidence Evidence,
        IRoleInfo ReportedBy,
        Option<IRoleInfo> HandledBy,
        IssueStatus Status) 
    : ITradeIssue<SaniCleanTrade>;