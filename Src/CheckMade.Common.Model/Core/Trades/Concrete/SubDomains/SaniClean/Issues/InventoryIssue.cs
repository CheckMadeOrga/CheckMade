using CheckMade.Common.Model.Core.Actors.RoleSystem;
using CheckMade.Common.Model.Core.LiveEvents;

namespace CheckMade.Common.Model.Core.Trades.Concrete.SubDomains.SaniClean.Issues;

public record InventoryIssue(
        DateTime CreationDate,
        ISphereOfAction Sphere,
        Option<ITradeFacility> Facility,
        Geo Location,
        IssueEvidence Evidence,
        IRoleInfo ReportedBy,
        Option<IRoleInfo> HandledBy,
        IssueStatus Status) 
    : ITradeIssue;