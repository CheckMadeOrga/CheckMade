using CheckMade.Common.Model.Core.Actors.RoleSystem.Concrete;
using CheckMade.Common.Model.Core.LiveEvents;
using CheckMade.Common.Model.Core.Trades;
using CheckMade.Common.Model.Utils;

namespace CheckMade.Common.Model.Core.Issues.Concrete.IssueTypes;

public sealed record TechnicalIssue<T>(
        Guid Id,    
        DateTimeOffset CreationDate,
        ISphereOfAction Sphere,
        IFacility Facility,
        IssueEvidence Evidence,
        Role ReportedBy,
        Option<Role> HandledBy,
        IssueStatus Status,
        IDomainGlossary Glossary) 
    : ITradeIssue<T>, IIssueInvolvingFacility, IIssueWithEvidence where T : ITrade
{
    public UiString FormatDetails()
    {
        throw new NotImplementedException();
    }
}