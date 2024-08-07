using CheckMade.Common.Model.Utils;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete.Global.Logout.States;

internal interface ILogoutWorkflowAborted : IWorkflowStateTerminator;

internal sealed record LogoutWorkflowAborted(IDomainGlossary Glossary) : ILogoutWorkflowAborted;