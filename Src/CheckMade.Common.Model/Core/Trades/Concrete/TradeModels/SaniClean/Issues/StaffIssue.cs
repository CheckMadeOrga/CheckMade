using CheckMade.Common.Model.Core.Actors.RoleSystem;
using CheckMade.Common.Model.Core.LiveEvents;

namespace CheckMade.Common.Model.Core.Trades.Concrete.TradeModels.SaniClean.Issues;

public record StaffIssue(
        Guid Id,
        DateTime CreationDate, 
        ISphereOfAction Sphere, 
        IssueEvidence Evidence, 
        IRoleInfo ReportedBy, 
        Option<IRoleInfo> HandledBy, 
        IssueStatus Status = IssueStatus.Reported) 
    : ITradeIssue
{
    public UiString GetSummary()
    {
        throw new NotImplementedException();
    }
}