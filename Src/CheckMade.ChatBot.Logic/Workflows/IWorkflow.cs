using CheckMade.ChatBot.Logic.Utils;
using CheckMade.Common.Model.ChatBot.Input;

namespace CheckMade.ChatBot.Logic.Workflows;

internal interface IWorkflow
{
    Task<Result<WorkflowResponse>> GetResponseAsync(TlgInput currentInput);
    IStateMediator Mediator { get; }
    IGeneralWorkflowUtils GeneralWorkflowUtils { get; }
}