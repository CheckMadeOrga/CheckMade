using CheckMade.ChatBot.Logic;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Utils;
using CheckMade.Tests.Startup;
using CheckMade.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace CheckMade.Tests.Unit.ChatBot.Logic;

public class LogicUtilsTests
{
    private ServiceProvider? _services;

    [Fact]
    public async Task GetAllCurrentInteractiveAsync_ReturnsAllInputs_WhenNoExpiredRoleBinds()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;
        
        var historicInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId)
        };
        
        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();
        
        var currentInput = inputGenerator.GetValidTlgInputTextMessage(
            tlgAgent.UserId, tlgAgent.ChatId);

        var result = 
            await logicUtils.GetAllCurrentInteractiveAsync(tlgAgent, currentInput);

        Assert.Equal(
            historicInputs.Length + 1,
            result.Count);
    }

    [Fact]
    public async Task GetAllCurrentInteractiveAsync_ReturnsInputsAfterCutoffDate_WhenExpiredRoleBindExists()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;

        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        var expiredRoleBind = new TlgAgentRoleBind(
            SaniCleanAdmin_DanielEn_X2024,
            tlgAgent,
            cutoffDate.AddDays(-2),
            cutoffDate,
            DbRecordStatus.Historic);

        var historicInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId, dateTime: cutoffDate.AddHours(-1)),
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId, dateTime: cutoffDate.AddHours(1))
        };
        
        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            roleBindings: new[] { expiredRoleBind },
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();

        var currentInput = inputGenerator.GetValidTlgInputTextMessage(
            tlgAgent.UserId, tlgAgent.ChatId, dateTime: cutoffDate.AddHours(2));

        var result = 
            await logicUtils.GetAllCurrentInteractiveAsync(tlgAgent, currentInput);

        Assert.Equal(
            2,
            result.Count);
        Assert.All(
            result,
            input => Assert.True(
                input.Details.TlgDate > cutoffDate));
    }

    [Fact]
    public async Task GetAllCurrentInteractiveAsync_ReturnsInputsAfterLatestExpiredRoleBind_WhenMultipleExpiredExist()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;

        var oldestCutoffDate = DateTime.UtcNow.AddDays(-3);
        var latestCutoffDate = DateTime.UtcNow.AddDays(-1);

        var expiredRoleBinds = new[]
        {
            new TlgAgentRoleBind(
                SaniCleanAdmin_DanielEn_X2024,
                tlgAgent,
                oldestCutoffDate.AddDays(-1),
                oldestCutoffDate,
                DbRecordStatus.Historic),
            
            new TlgAgentRoleBind(
                SaniCleanInspector_DanielEn_X2024,
                tlgAgent,
                latestCutoffDate.AddDays(-1),
                latestCutoffDate,
                DbRecordStatus.Historic)
        };

        var historicInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId,
                dateTime: oldestCutoffDate.AddHours(-1)),
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId,
                dateTime: oldestCutoffDate.AddHours(1)),
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId,
                dateTime: latestCutoffDate.AddHours(-1))
        };

        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            roleBindings: expiredRoleBinds,
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();

        var currentInput = inputGenerator.GetValidTlgInputTextMessage(
            tlgAgent.UserId, tlgAgent.ChatId,
            dateTime: latestCutoffDate.AddHours(1));
        
        var result = 
            await logicUtils.GetAllCurrentInteractiveAsync(tlgAgent, currentInput);

        Assert.Single(result);
        Assert.True(
            result.Single().Details.TlgDate > latestCutoffDate);
    }

    [Fact]
    public async Task GetAllCurrentInteractiveAsync_ReturnsEmptyCollection_WhenNoInputsAfterLatestExpiredRoleBind()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;

        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        var expiredRoleBind = new TlgAgentRoleBind(
            SaniCleanAdmin_DanielEn_X2024,
            tlgAgent,
            cutoffDate.AddDays(-2),
            cutoffDate,
            DbRecordStatus.Historic);

        var historicInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId,
                dateTime: cutoffDate.AddHours(-2))
        };

        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            roleBindings: new[] { expiredRoleBind },
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();

        var currentInput = inputGenerator.GetValidTlgInputTextMessage(
            tlgAgent.UserId, tlgAgent.ChatId,
            dateTime: cutoffDate.AddHours(-1));
        
        var result = 
            await logicUtils.GetAllCurrentInteractiveAsync(tlgAgent, currentInput);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCurrentInteractiveAsync_HandlesNullDeactivationDate_InExpiredRoleBinds()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;

        var roleBindWithNullDeactivation = new TlgAgentRoleBind(
            SaniCleanAdmin_DanielEn_X2024,
            tlgAgent,
            DateTime.UtcNow.AddDays(-2),
            Option<DateTime>.None(),
            DbRecordStatus.Historic);

        var historicInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId)
        };

        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            roleBindings: new[] { roleBindWithNullDeactivation },
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();

        var currentInput = inputGenerator.GetValidTlgInputTextMessage(
            tlgAgent.UserId, tlgAgent.ChatId);
            
        var result = 
            await logicUtils.GetAllCurrentInteractiveAsync(tlgAgent, currentInput);

        Assert.Equal(
            historicInputs.Length + 1,
            result.Count);
    }

    [Fact]
    public async Task GetAllCurrentInteractiveAsync_FiltersInputsBySpecificTlgAgent()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;
        var tlgAgentDecoy = UserId02_ChatId03_Operations;

        var historicInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgent.UserId, tlgAgent.ChatId),
            inputGenerator.GetValidTlgInputTextMessage(
                tlgAgentDecoy.UserId, tlgAgentDecoy.ChatId)
        };

        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();

        var currentInput = inputGenerator.GetValidTlgInputTextMessage(
            tlgAgent.UserId, tlgAgent.ChatId);
            
        var result = 
            await logicUtils.GetAllCurrentInteractiveAsync(tlgAgent, currentInput);
        
        Assert.Equal(
            2, result.Count);
        Assert.All(
            result,
            input => Assert.Equal(tlgAgent.UserId, input.TlgAgent.UserId));
        Assert.All(
            result,
            input => Assert.Equal(tlgAgent.ChatId, input.TlgAgent.ChatId));
    }

    [Fact]
    public async Task GetRecentLocationHistory_FiltersInputs_ByTlgAgentAndLocationTypeAndTimeFrame()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var tlgAgent = PrivateBotChat_Operations;
        var tlgAgentDecoy = UserId02_ChatId03_Operations;

        var randomDecoyLocation = 
            new Geo(0, 0, Option<double>.None());

        var expectedLocation =
            new Geo(1, 1, Option<double>.None());

        var historicInputs = new[]
        {
            // Decoy: too long ago
            inputGenerator.GetValidTlgInputLocationMessage(
                randomDecoyLocation,
                tlgAgent.UserId, tlgAgent.ChatId,
                DateTime.UtcNow.AddMinutes(-(ILogicUtils.RecentLocationHistoryTimeFrameInMinutes + 2))),
            // Decoy: wrong TlgAgent
            inputGenerator.GetValidTlgInputLocationMessage(
                randomDecoyLocation,
                tlgAgentDecoy.UserId, tlgAgentDecoy.ChatId,
                DateTime.UtcNow.AddMinutes(-(ILogicUtils.RecentLocationHistoryTimeFrameInMinutes -1))),
            // Decoy: not a LocationUpdate
            inputGenerator.GetValidTlgInputTextMessage(),
            
            // Expected to be included
            inputGenerator.GetValidTlgInputLocationMessage(
                expectedLocation,
                tlgAgent.UserId, tlgAgent.ChatId,
                DateTime.UtcNow.AddMinutes(-(ILogicUtils.RecentLocationHistoryTimeFrameInMinutes -1)))
        };
        
        var serviceCollection = new UnitTestStartup().Services;
        var (services, _) = serviceCollection.ConfigureTestRepositories(
            inputs: historicInputs);
        var logicUtils = services.GetRequiredService<ILogicUtils>();
        
        var result = 
            await logicUtils.GetRecentLocationHistory(tlgAgent);
        
        Assert.Single(result);
        Assert.Equivalent(
            expectedLocation, 
            result.First().Details.GeoCoordinates.GetValueOrThrow());
    }
}