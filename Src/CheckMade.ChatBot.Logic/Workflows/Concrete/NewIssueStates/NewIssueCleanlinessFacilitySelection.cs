using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.ChatBot.Output;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete.NewIssueStates;

internal interface INewIssueCleanlinessFacilitySelection : IWorkflowState; 

internal record NewIssueCleanlinessFacilitySelection : INewIssueCleanlinessFacilitySelection
{
    public IReadOnlyCollection<OutputDto> MyPrompt()
    {
        throw new NotImplementedException();
    }

    public Task<Result<WorkflowResponse>> 
        ProcessAnswerToMyPromptToGetNextStateWithItsPromptAsync(TlgInput currentInput)
    {
        throw new NotImplementedException();
    }
}