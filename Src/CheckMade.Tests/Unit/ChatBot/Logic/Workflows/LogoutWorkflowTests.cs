using CheckMade.ChatBot.Logic;
using CheckMade.ChatBot.Logic.Workflows.Concrete;
using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.UserInteraction;
using CheckMade.Common.Model.ChatBot.UserInteraction.BotCommands.DefinitionsByBot;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Utils;
using CheckMade.Tests.Startup;
using CheckMade.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CheckMade.Tests.Unit.ChatBot.Logic.Workflows;

public class LogoutWorkflowTests
{
    private ServiceProvider? _services;

    [Fact]
    public async Task GetNextOutputAsync_LogsOutAndReturnsConfirmation_AfterUserConfirmsLogoutIntention()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();

        var confirmLogoutCommand = inputGenerator.GetValidTlgInputCallbackQueryForControlPrompts(
            ControlPrompts.Yes);

        var boundRole = TestRepositoryUtils.GetNewRoleBind(
            SOpsEngineer_DanielEn_X2024,
            PrivateBotChat_Operations);
        
        var serviceCollection = new UnitTestStartup().Services;
        var (services, container) = serviceCollection.ConfigureTestRepositories(
            inputs: new[]
            {
                // Decoys
                inputGenerator.GetValidTlgInputCommandMessage(
                    Operations,
                    (int)OperationsBotCommands.Settings),
                inputGenerator.GetValidTlgInputCallbackQueryForDomainTerm(
                    Dt(LanguageCode.de)),
                // Relevant
                inputGenerator.GetValidTlgInputCommandMessage(
                    Operations,
                    (int)OperationsBotCommands.Logout),
                confirmLogoutCommand
            },
            roleBindings: new []{ boundRole } );
        _services = services;

        var mockRoleBindingsRepo =
            (Mock<ITlgAgentRoleBindingsRepository>)container.Mocks[typeof(ITlgAgentRoleBindingsRepository)];
        var workflow = _services.GetRequiredService<ILogoutWorkflow>();
        
        const string expectedMessage = "💨 Logged out.";
        
        var actualOutput = await workflow.GetResponseAsync(confirmLogoutCommand);
        
        Assert.Equal(
            expectedMessage, 
            TestUtils.GetFirstRawEnglish(actualOutput));
        
        mockRoleBindingsRepo.Verify(x => x.UpdateStatusAsync(
                new [] { boundRole }
                    .ToImmutableReadOnlyCollection(),
                DbRecordStatus.Historic),
            Times.Once());
    }

    [Fact]
    public async Task GetNextOutputAsync_LogsOutFromAllModes_WhenLoggingOutInPrivateChat()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        
        var tlgAgentOperations = PrivateBotChat_Operations;
        var tlgAgentComms = PrivateBotChat_Communications;
        var tlgAgentNotif = PrivateBotChat_Notifications;
        var boundRole = SOpsEngineer_DanielEn_X2024; 
        
        var confirmLogoutCommand = inputGenerator.GetValidTlgInputCallbackQueryForControlPrompts(
            ControlPrompts.Yes);

        var serviceCollection = new UnitTestStartup().Services;
        var (services, container) = serviceCollection.ConfigureTestRepositories(
            inputs: new[]
            {
                inputGenerator.GetValidTlgInputCommandMessage(
                    Operations,
                    (int)OperationsBotCommands.Logout),
                confirmLogoutCommand
            },
            roleBindings: new List<TlgAgentRoleBind>
            {
                // Relevant
                new(boundRole, tlgAgentOperations,
                    DateTime.UtcNow, Option<DateTime>.None()),
                new(boundRole, tlgAgentComms,
                    DateTime.UtcNow, Option<DateTime>.None()),
                new(boundRole, tlgAgentNotif,
                    DateTime.UtcNow, Option<DateTime>.None()),
                // Decoys
                new(boundRole, tlgAgentOperations,
                    DateTime.UtcNow, Option<DateTime>.None(), DbRecordStatus.SoftDeleted),
                new(SOpsCleanLead_DanielDe_X2024, tlgAgentOperations,
                    DateTime.UtcNow, Option<DateTime>.None()),
                new(boundRole, new TlgAgent(UserId02, ChatId04, Communications),
                    DateTime.UtcNow, Option<DateTime>.None())
            });
        _services = services;

        var mockTlgAgentRoleBindingsForAllModes =
            (Mock<ITlgAgentRoleBindingsRepository>)container.Mocks[typeof(ITlgAgentRoleBindingsRepository)];
        var workflow = _services.GetRequiredService<ILogoutWorkflow>();
        
        var expectedBindingsUpdated = 
            (await mockTlgAgentRoleBindingsForAllModes.Object.GetAllActiveAsync())
            .Where(tarb => 
                tarb.TlgAgent.UserId.Equals(tlgAgentOperations.UserId) &&
                tarb.TlgAgent.ChatId.Equals(tlgAgentOperations.ChatId) &&
                tarb.Role.Equals(boundRole))
            .ToImmutableReadOnlyList();
        
        var actualTlgAgentRoleBindingsUpdated = new List<TlgAgentRoleBind>();
        mockTlgAgentRoleBindingsForAllModes
            .Setup(x => x.UpdateStatusAsync(
                It.IsAny<IReadOnlyCollection<TlgAgentRoleBind>>(), DbRecordStatus.Historic))
            .Callback<IReadOnlyCollection<TlgAgentRoleBind>, DbRecordStatus>(
                (tlgAgentRoleBinds, newStatus) => 
                {
                    actualTlgAgentRoleBindingsUpdated = tlgAgentRoleBinds
                        .Select(tarb => tarb with
                        {
                            DeactivationDate = DateTime.UtcNow,
                            Status = newStatus
                        })
                        .ToList();
                });
        
        await workflow.GetResponseAsync(confirmLogoutCommand);

        for (var i = 0; i < expectedBindingsUpdated.Count; i++)
        {
            Assert.Equivalent(
                expectedBindingsUpdated[i].TlgAgent,
                actualTlgAgentRoleBindingsUpdated[i].TlgAgent);
            Assert.Equivalent(
                expectedBindingsUpdated[i].Role,
                actualTlgAgentRoleBindingsUpdated[i].Role);
            Assert.True(
                actualTlgAgentRoleBindingsUpdated[i].Status == DbRecordStatus.Historic);
        }
    }

    [Fact]
    public async Task GetNextOutputAsync_ConfirmsAbortion_AfterUserAbortsLogout()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var abortLogoutCommand = inputGenerator.GetValidTlgInputCallbackQueryForControlPrompts(
            ControlPrompts.No);

        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            inputs: new[]
            {
                // Decoys
                inputGenerator.GetValidTlgInputCommandMessage(
                    Operations,
                    (int)OperationsBotCommands.Settings),
                inputGenerator.GetValidTlgInputCallbackQueryForDomainTerm(
                    Dt(LanguageCode.de)),
                // Relevant
                inputGenerator.GetValidTlgInputCommandMessage(
                    Operations,
                    (int)OperationsBotCommands.Logout),
                abortLogoutCommand
            });
        _services = services;
        
        var workflow = _services.GetRequiredService<ILogoutWorkflow>();
        const string expectedMessage1 = "Logout aborted.\n"; 
        
        var actualOutput = 
            await workflow.GetResponseAsync(abortLogoutCommand);
        
        Assert.Equal(
            expectedMessage1,
            TestUtils.GetFirstRawEnglish(actualOutput));
        Assert.Equivalent(
            IInputProcessor.SeeValidBotCommandsInstruction.RawEnglishText, 
            actualOutput.GetValueOrThrow().First().Text.GetValueOrThrow().Concatenations.Last()!.RawEnglishText);
    }
}