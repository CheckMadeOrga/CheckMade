using CheckMade.ChatBot.Logic.Utils;
using CheckMade.Common.Interfaces.Persistence.Core;
using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.ChatBot.Output;
using CheckMade.Common.Model.ChatBot.UserInteraction;
using CheckMade.Common.Model.Core.LiveEvents;
using CheckMade.Common.Model.Core.Trades;
using CheckMade.Common.Model.Utils;
using static CheckMade.ChatBot.Logic.Workflows.Concrete.NewIssue.NewIssueUtils;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete.NewIssue.States.B_Details;

internal interface INewIssueFacilitySelection<T> : IWorkflowStateNormal where T : ITrade; 

internal sealed record NewIssueFacilitySelection<T>(
        IDomainGlossary Glossary,
        IStateMediator Mediator,
        IGeneralWorkflowUtils GeneralWorkflowUtils,
        ILiveEventsRepository LiveEventsRepo) 
    : INewIssueFacilitySelection<T> where T : ITrade, new()
{
    public async Task<IReadOnlyCollection<OutputDto>> GetPromptAsync(
        TlgInput currentInput, 
        Option<int> inPlaceUpdateMessageId,
        Option<OutputDto> previousPromptFinalizer)
    {
        var currentSphere = 
            GetLastSelectedSphere(
                await GeneralWorkflowUtils.GetInteractiveSinceLastBotCommandAsync(currentInput), 
                GetAllTradeSpecificSpheres(
                    (await LiveEventsRepo.GetAsync(currentInput.LiveEventContext.GetValueOrThrow()))!,
                    new T()));
        
        List<OutputDto> outputs =
        [
            new()
            {
                Text = Ui("Choose affected facility:"),
                DomainTermSelection = Option<IReadOnlyCollection<DomainTerm>>.Some(
                    Glossary
                        .GetAll(typeof(IFacility))
                        .Where(dt => currentSphere.Details.AvailableFacilities.Contains(dt))
                        .ToImmutableReadOnlyCollection()),
                ControlPromptsSelection = ControlPrompts.Back,
                UpdateExistingOutputMessageId = inPlaceUpdateMessageId
            }
        ];
        
        return previousPromptFinalizer.Match(
            ppf =>
            {
                outputs.Add(ppf);
                return outputs;
            },
            () => outputs);
    }

    public async Task<Result<WorkflowResponse>> GetWorkflowResponseAsync(TlgInput currentInput)
    {
        if (currentInput.InputType is not TlgInputType.CallbackQuery)
            return WorkflowResponse.CreateWarningUseInlineKeyboardButtons(this);

        if (currentInput.Details.DomainTerm.IsSome)
        {
            return await WorkflowResponse.CreateFromNextStateAsync(
                currentInput, 
                Mediator.Next(typeof(INewIssueEvidenceEntry<T>)),
                new PromptTransition(
                    new OutputDto
                    {
                        Text = UiConcatenate(
                            UiIndirect(currentInput.Details.Text.GetValueOrThrow()),
                            UiNoTranslate(" "),
                            Glossary.GetUi(currentInput.Details.DomainTerm.GetValueOrThrow())),
                        UpdateExistingOutputMessageId = currentInput.TlgMessageId
                    }));
        }

        return // on ControlPrompts.Back
            await WorkflowResponse.CreateFromNextStateAsync(
                currentInput,
                Mediator.Next(typeof(INewIssueTypeSelection<T>)),
                new PromptTransition(true));
    }
}