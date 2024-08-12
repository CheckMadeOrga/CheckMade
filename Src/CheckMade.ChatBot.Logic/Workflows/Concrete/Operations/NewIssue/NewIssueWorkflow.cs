using CheckMade.ChatBot.Logic.Workflows.Concrete.Operations.NewIssue.States.A_Init;
using CheckMade.ChatBot.Logic.Workflows.Utils;
using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Interfaces.Persistence.Core;
using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.Core.LiveEvents;
using CheckMade.Common.Model.Core.Trades.Concrete;
using static CheckMade.ChatBot.Logic.Workflows.Concrete.Operations.NewIssue.NewIssueUtils;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete.Operations.NewIssue;

internal sealed record NewIssueWorkflow(
        ILiveEventsRepository LiveEventsRepo,
        IGeneralWorkflowUtils WorkflowUtils,
        IStateMediator Mediator,
        IDerivedWorkflowBridgesRepository BridgesRepo)
    : WorkflowBase(WorkflowUtils, Mediator, BridgesRepo)
{
    protected override async Task<Result<WorkflowResponse>> InitializeAsync(TlgInput currentInput)
    {
        var currentRole = currentInput.OriginatorRole.GetValueOrThrow();
        
        if (!currentRole.IsCurrentRoleTradeSpecific())
        {
            return await WorkflowResponse.CreateFromNextStateAsync(
                currentInput, Mediator.Next(typeof(INewIssueTradeSelection)));
        }

        var trade = currentRole.RoleType.GetTradeInstance().GetValueOrThrow();
        
        var liveEvent = (await LiveEventsRepo.GetAsync(
            currentInput.LiveEventContext.GetValueOrThrow()))!;
        
        var lastKnownLocation = await LastKnownLocationAsync(currentInput, WorkflowUtils);

        var sphere = lastKnownLocation.IsSome
            ? SphereNearCurrentUser(liveEvent, lastKnownLocation.GetValueOrThrow(), trade)
            : Option<ISphereOfAction>.None();

        return await sphere.Match(
            _ => trade switch
            {
                SaniCleanTrade => WorkflowResponse.CreateFromNextStateAsync(
                    currentInput, Mediator.Next(typeof(INewIssueSphereConfirmation<SaniCleanTrade>))),
                SiteCleanTrade => WorkflowResponse.CreateFromNextStateAsync(
                    currentInput, Mediator.Next(typeof(INewIssueSphereConfirmation<SiteCleanTrade>))),
                _ => throw new InvalidOperationException($"Unhandled {nameof(trade)}: '{trade}'")
            },
            () => trade switch
            {
                SaniCleanTrade => WorkflowResponse.CreateFromNextStateAsync(
                    currentInput, Mediator.Next(typeof(INewIssueSphereSelection<SaniCleanTrade>))),
                SiteCleanTrade => WorkflowResponse.CreateFromNextStateAsync(
                    currentInput, Mediator.Next(typeof(INewIssueSphereSelection<SiteCleanTrade>))),
                _ => throw new InvalidOperationException($"Unhandled {nameof(trade)}: '{trade}'")
            });
    }
}