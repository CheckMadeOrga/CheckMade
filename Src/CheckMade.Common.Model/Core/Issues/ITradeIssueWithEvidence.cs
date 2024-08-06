using CheckMade.Common.Model.Core.Issues.Concrete;
using CheckMade.Common.Model.Core.Trades;

namespace CheckMade.Common.Model.Core.Issues;

public interface ITradeIssueWithEvidence<T> : IIssue where T : ITrade
{
    IssueEvidence Evidence { get; }
}