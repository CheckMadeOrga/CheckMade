using CheckMade.ChatBot.Logic.Utils;
using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Interfaces.Persistence.Core;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.ChatBot.Output;
using CheckMade.Common.Model.ChatBot.UserInteraction;
using CheckMade.Common.Model.Utils;
using static CheckMade.Common.LangExt.InputValidator;

namespace CheckMade.ChatBot.Logic.Workflows.Concrete;

using static UserAuthWorkflow.States;

internal interface IUserAuthWorkflow : IWorkflow
{
    UserAuthWorkflow.States DetermineCurrentState(
        IReadOnlyCollection<TlgInput> tlgAgentInputHistory);
}

internal sealed record UserAuthWorkflow(
        IRolesRepository RolesRepo,
        ITlgAgentRoleBindingsRepository RoleBindingsRepo,
        IGeneralWorkflowUtils GeneralWorkflowUtils,
        IDomainGlossary Glossary)
    : IUserAuthWorkflow
{
    internal static readonly OutputDto EnterTokenPrompt = new()
    {
        Text = Ui("🌀 Please enter your role token (format '{0}'): ", GetTokenFormatExample())
    };

    public bool IsCompleted(IReadOnlyCollection<TlgInput> inputHistory)
    {
        return 
            DetermineCurrentState(inputHistory) 
            == ReceivedTokenSubmissionAttempt;
    }

    public async Task<Result<WorkflowResponse>> 
        GetResponseAsync(TlgInput currentInput)
    {
        var inputText = currentInput.Details.Text.GetValueOrDefault();
        
        var tlgAgentInputHistory = 
            await GeneralWorkflowUtils.GetAllCurrentInteractiveAsync(currentInput.TlgAgent, currentInput);
        
        return DetermineCurrentState(tlgAgentInputHistory) switch
        {
            Initial => 
                new WorkflowResponse(
                    EnterTokenPrompt,
                    Glossary.GetId(Initial)),
            
            ReceivedTokenSubmissionAttempt => 
                new WorkflowResponse(
                    IsValidToken(inputText) switch 
                    {
                        true => await TokenExists(currentInput.Details.Text.GetValueOrDefault()) switch
                        {
                            true => await AuthenticateUserAsync(currentInput),
                            
                            false => [new OutputDto 
                                {
                                    Text = Ui("This is an unknown token. Try again...")
                                },
                                EnterTokenPrompt]
                        },
                        false => [new OutputDto
                            {
                                Text = Ui("Bad token format! Try again...")
                            },
                            EnterTokenPrompt] 
                    }, 
                    Glossary.GetId(ReceivedTokenSubmissionAttempt)),
            
            _ => Result<WorkflowResponse>.FromError(
                UiNoTranslate($"Can't determine State in {nameof(UserAuthWorkflow)}"))
        };
    }
    
    public States DetermineCurrentState(
        IReadOnlyCollection<TlgInput> tlgAgentInputHistory)
    {
        var lastTextSubmitted = tlgAgentInputHistory
            .LastOrDefault(i => i.InputType.Equals(TlgInputType.TextMessage));

        return lastTextSubmitted switch
        {
            null => Initial,
            _ => ReceivedTokenSubmissionAttempt
        };
    }

    private async Task<bool> TokenExists(string tokenAttempt) =>
        (await RolesRepo.GetAllAsync())
        .Any(role => role.Token.Equals(tokenAttempt));

    private async Task<List<OutputDto>> AuthenticateUserAsync(TlgInput tokenInputAttempt)
    {
        var inputText = tokenInputAttempt.Details.Text.GetValueOrThrow();
        var currentMode = tokenInputAttempt.TlgAgent.Mode;
        
        var outputs = new List<OutputDto>();
        
        var newTlgAgentRoleBindForCurrentMode = new TlgAgentRoleBind(
            (await RolesRepo.GetAllAsync()).First(r => r.Token.Equals(inputText)),
            tokenInputAttempt.TlgAgent with { Mode = currentMode },
            DateTimeOffset.UtcNow,
            Option<DateTimeOffset>.None());
        
        var preExistingRoleBindings = 
            (await RoleBindingsRepo.GetAllActiveAsync())
            .ToImmutableReadOnlyCollection();
        
        var preExistingActiveRoleBind = 
            FirstOrDefaultPreExistingActiveRoleBind(currentMode);

        var roleTypeUiString = 
            Glossary.GetUi(newTlgAgentRoleBindForCurrentMode.Role.RoleType.GetType()); 
        
        if (preExistingActiveRoleBind != null)
        {
            await RoleBindingsRepo.UpdateStatusAsync(preExistingActiveRoleBind, DbRecordStatus.Historic);
            
            outputs.Add(new OutputDto
            {
                Text = UiConcatenate(
                    Ui("""
                       Warning: you were already authenticated with this token in another {0} chat.
                       This will be the new {0} chat where you receive messages at {1}, in your role as: 
                       """, 
                        currentMode,
                        newTlgAgentRoleBindForCurrentMode.Role.AtLiveEvent.Name),
                    roleTypeUiString)
            });
        }
        
        outputs.Add(new OutputDto
        {
            Text = UiConcatenate(
                Ui("{0}, you have successfully authenticated at live-event {1} in your role as: ", 
                    newTlgAgentRoleBindForCurrentMode.Role.ByUser.FirstName,
                    newTlgAgentRoleBindForCurrentMode.Role.AtLiveEvent.Name),
                roleTypeUiString)
        });

        outputs.Add(new OutputDto
        {
            Text = IInputProcessor.SeeValidBotCommandsInstruction
        });

        var tlgAgentRoleBindingsToAdd = new List<TlgAgentRoleBind> { newTlgAgentRoleBindForCurrentMode };
        
        var isInputInPrivateBotChat = 
            tokenInputAttempt.TlgAgent.ChatId.Id.Equals(
                tokenInputAttempt.TlgAgent.UserId.Id);

        if (isInputInPrivateBotChat)
        {
            AddTlgAgentRoleBindingsForOtherModes();
        }
        
        await RoleBindingsRepo.AddAsync(tlgAgentRoleBindingsToAdd);
        
        return outputs;
        
        TlgAgentRoleBind? FirstOrDefaultPreExistingActiveRoleBind(InteractionMode mode) =>
            preExistingRoleBindings.FirstOrDefault(tarb => 
                tarb.Role.Token.Equals(inputText) &&
                tarb.TlgAgent.Mode.Equals(mode));

        void AddTlgAgentRoleBindingsForOtherModes()
        {
            var allModes = Enum.GetValues(typeof(InteractionMode)).Cast<InteractionMode>();
            var otherModes = allModes.Except(new[] { currentMode });

            tlgAgentRoleBindingsToAdd.AddRange(
                from mode in otherModes 
                where FirstOrDefaultPreExistingActiveRoleBind(mode) == null
                select newTlgAgentRoleBindForCurrentMode with
                {
                    TlgAgent = newTlgAgentRoleBindForCurrentMode.TlgAgent with { Mode = mode },
                    ActivationDate = DateTimeOffset.UtcNow
                });
        }
    }
    
    [Flags]
    internal enum States
    {
        Initial = 1,
        ReceivedTokenSubmissionAttempt = 1 << 1
    }
}