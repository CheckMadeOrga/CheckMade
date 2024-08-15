using CheckMade.Common.Model.Core.Actors.RoleSystem.Concrete;
using CheckMade.Common.Model.Core.LiveEvents;
using CheckMade.Common.Model.Core.Trades;
using CheckMade.Common.Model.Utils;
using static CheckMade.Common.Model.Core.Submissions.Issues.Concrete.IssueSummaryCategories;

namespace CheckMade.Common.Model.Core.Submissions.Issues.Concrete.IssueTypes;

public sealed record StaffIssue<T>(
        Guid Id,
        DateTimeOffset CreationDate, 
        ISphereOfAction Sphere, 
        SubmissionEvidence Evidence, 
        Role ReportedBy, 
        Option<Role> HandledBy, 
        IssueStatus Status,
        IDomainGlossary Glossary) 
    : ITradeIssue<T>, ITradeIssueWithEvidence where T : ITrade, new()
{
    public IReadOnlyDictionary<IssueSummaryCategories, UiString> GetSummary()
    {
        return new Dictionary<IssueSummaryCategories, UiString>
        {
            [CommonBasics] = IssueFormatters.FormatCommonBasics(this, Glossary),
            [OperationalInfo] = IssueFormatters.FormatOperationalInfo(this, Glossary),
            [EvidenceInfo] = IssueFormatters.FormatEvidenceInfo(this)
        };
    }
}