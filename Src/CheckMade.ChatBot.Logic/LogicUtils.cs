using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.Input;

namespace CheckMade.ChatBot.Logic;

internal interface ILogicUtils
{
    public static readonly UiString WorkflowWasCompleted = UiConcatenate(
        Ui("The previous workflow was completed. You can continue with a new one... "),
        IInputProcessor.SeeValidBotCommandsInstruction);

    Task InitAsync();
    
    IReadOnlyCollection<TlgAgentRoleBind> GetAllTlgAgentRoles();
    Task<IReadOnlyCollection<TlgInput>> GetAllInputsOfTlgAgentInCurrentRoleAsync(TlgAgent tlgAgent);
    Task<IReadOnlyCollection<TlgInput>> GetInputsForCurrentWorkflow(TlgAgent tlgAgent);
    
    public static Option<TlgInput> GetLastBotCommand(IReadOnlyCollection<TlgInput> inputs) =>
        inputs.LastOrDefault(i => i.Details.BotCommandEnumCode.IsSome)
        ?? Option<TlgInput>.None();
}

// ToDo: In line with today's decision: pre calculate all historic inputs and then use that in-memory for all subsequent filtering and processing!
// I.e. no need for two calls to inputRepo.GetAll as below.
// Implement GetAllHumanInput(liveEvent) once I've added liveEvent into DB and also as a redundant field in inputs !!!
// And separate GetAllLocationUpdates(???) to not always load thousands of locations when I just need to look at workflow
// Offer GetAllLocationUpdates(tlgAgent) or even restrict to recent time, depending on what I need the location data for. 

internal class LogicUtils(
        ITlgInputsRepository inputsRepo,
        ITlgAgentRoleBindingsRepository tlgAgentRoleBindingsRepo)
    : ILogicUtils
{
    private IReadOnlyCollection<TlgAgentRoleBind> _preExistingTlgAgentRoles = new List<TlgAgentRoleBind>();

    public async Task InitAsync()
    {
        var getTlgAgentRolesTask = tlgAgentRoleBindingsRepo.GetAllAsync();

        // In preparation for other async tasks that can then run in parallel
#pragma warning disable CA1842
        await Task.WhenAll(getTlgAgentRolesTask);
#pragma warning restore CA1842
        
        _preExistingTlgAgentRoles = getTlgAgentRolesTask.Result.ToImmutableReadOnlyCollection();
    }

    public IReadOnlyCollection<TlgAgentRoleBind> GetAllTlgAgentRoles() => _preExistingTlgAgentRoles;

    public async Task<IReadOnlyCollection<TlgInput>> GetAllInputsOfTlgAgentInCurrentRoleAsync(TlgAgent tlgAgent)
    {
        var lastPreviousTlgAgentRole = _preExistingTlgAgentRoles
            .Where(arb =>
                arb.TlgAgent == tlgAgent &&
                arb.DeactivationDate.IsSome)
            .MaxBy(arb => arb.DeactivationDate.GetValueOrThrow());

        var dateOfLastDeactivationForCutOff = lastPreviousTlgAgentRole != null
            ? lastPreviousTlgAgentRole.DeactivationDate.GetValueOrThrow()
            : DateTime.MinValue;
        
        return (await inputsRepo.GetAllAsync(tlgAgent))
            .Where(i => 
                i.Details.TlgDate.ToUniversalTime() > 
                dateOfLastDeactivationForCutOff.ToUniversalTime())
            .ToImmutableReadOnlyCollection();
    }

    public async Task<IReadOnlyCollection<TlgInput>> GetInputsForCurrentWorkflow(TlgAgent tlgAgent)
    {
        var allInputsOfTlgAgent = 
            (await inputsRepo.GetAllAsync(tlgAgent)).ToImmutableReadOnlyCollection();

        return allInputsOfTlgAgent
            .GetLatestRecordsUpTo(input => input.InputType == TlgInputType.CommandMessage)
            .ToImmutableReadOnlyCollection();
    }
}