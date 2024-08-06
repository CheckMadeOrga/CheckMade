using CheckMade.Common.Model.Core.Trades;
using CheckMade.Common.Model.Utils;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete.NewIssue.States.D_Terminators;

internal interface INewIssueCancelled<T> : IWorkflowStateTerminator where T : ITrade;

internal sealed record NewIssueCancelled<T>(IDomainGlossary Glossary) : INewIssueCancelled<T> where T : ITrade;