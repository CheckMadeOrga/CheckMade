using CheckMade.ChatBot.Logic.Utils;
using CheckMade.ChatBot.Logic.Workflows.Concrete.Global.UserAuth.States;
using CheckMade.Common.Model.ChatBot.Input;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete.Global.UserAuth;

internal interface IUserAuthWorkflow : IWorkflow;

internal sealed record UserAuthWorkflow(
        IGeneralWorkflowUtils GeneralWorkflowUtils,
        IStateMediator Mediator)
    : WorkflowBase(GeneralWorkflowUtils, Mediator), IUserAuthWorkflow
{
    protected override async Task<Result<WorkflowResponse>> InitializeAsync(TlgInput currentInput)
    {
        return await WorkflowResponse.CreateFromNextStateAsync(
            currentInput,
            Mediator.Next(typeof(IUserAuthWorkflowTokenEntry)));
    }
}