using CheckMade.ChatBot.Logic.Workflows.Concrete;
using CheckMade.Common.Interfaces.ChatBot.Logic;
using CheckMade.Common.Interfaces.Persistence;
using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Persistence;
using CheckMade.Tests.Startup;
using CheckMade.Tests.Startup.ConfigProviders;
using CheckMade.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static CheckMade.Tests.Utils.TestOriginatorRoleSetting;

namespace CheckMade.Tests.Integration.Persistence;

// ProblematicTestsOutsideOfIDE
public class TlgInputsRepositoryTests(ITestOutputHelper testOutputHelper)
{
    private ServiceProvider? _services;
    
    [Fact]
    public async Task SavesAndRetrieves_IndividualInputs_WhenAllInputsValid()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        var glossary = _services.GetRequiredService<IDomainGlossary>();
        
        var tlgInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                roleSetting: None),
            inputGenerator.GetValidTlgInputTextMessage(
                roleSetting: Default),
            inputGenerator.GetValidTlgInputTextMessage(
                roleSetting: Default,
                resultantWorkflowInfo: new ResultantWorkflowInfo(
                    glossary.IdAndUiByTerm[Dt(typeof(UserAuthWorkflow))].callbackId,
                    glossary.IdAndUiByTerm[Dt(UserAuthWorkflow.States.Initial)].callbackId))
        };
        
        foreach (var input in tlgInputs)
        {
            List<TlgInput> expectedRetrieval = [ 
                new (input.TlgAgent, 
                    input.InputType, 
                    input.OriginatorRole, 
                    input.LiveEventContext, 
                    input.ResultantWorkflow,
                    input.Details)];
        
            await inputRepo.AddAsync(input);
            
            var retrievedInputs = 
                (await inputRepo.GetAllInteractiveAsync(input.TlgAgent))
                .OrderByDescending(x => x.Details.TlgDate)
                .ToImmutableReadOnlyCollection();
            
            await inputRepo.HardDeleteAllAsync(input.TlgAgent);
        
            Assert.Equivalent(
                expectedRetrieval[0],
                retrievedInputs.First());
        }
    }

    [Fact]
    public async Task SavesAndRetrieves_DomainTerm_ViaCustomJsonSerialization_WhenInputHasValidDomainTerm()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        
        var expectedDomainTerm = Dt(LanguageCode.de);
        var tlgInput = inputGenerator.GetValidTlgInputCallbackQueryForDomainTerm(
            expectedDomainTerm,
            roleSetting: Default);
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        
        await inputRepo.AddAsync(tlgInput);
        
        var retrievedInput = 
            (await inputRepo.GetAllInteractiveAsync(PrivateBotChat_Operations))
            .First();
        
        await inputRepo.HardDeleteAllAsync(PrivateBotChat_Operations);
        
        Assert.Equivalent(
            expectedDomainTerm,
            retrievedInput.Details.DomainTerm.GetValueOrThrow());
    }
    
    [Fact]
    public async Task SavesAndRetrieves_GeoLocation_ViaCustomJsonSerialization_WhenInputHasValidGeo()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        
        var expectedGeo = new Geo(
            17.456,
            -23.00987,
            15.7f);
        
        var tlgInput = inputGenerator.GetValidTlgInputLocationMessage(
            expectedGeo,
            roleSetting: Default);
        
        await inputRepo.AddAsync(tlgInput);
        
        var retrievedInput = 
            (await inputRepo.GetAllLocationAsync(PrivateBotChat_Operations, DateTime.MinValue))
            .First();
        
        await inputRepo.HardDeleteAllAsync(PrivateBotChat_Operations);
        
        Assert.Equivalent(
            expectedGeo,
            retrievedInput.Details.GeoCoordinates.GetValueOrThrow());
    }

    [Fact]
    public async Task AddAsync_And_GetAllAsync_CorrectlyAddAndReturnsInBulk_MultipleValidInputs()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        
        var tlgInputs = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                roleSetting: None),
            inputGenerator.GetValidTlgInputTextMessage(
                roleSetting: Default),
            inputGenerator.GetValidTlgInputTextMessage(
                roleSetting: Default)
        };
        
        await inputRepo.AddAsync(tlgInputs);
        var retrievedInputs = 
            await inputRepo.GetAllInteractiveAsync(PrivateBotChat_Operations);
        await inputRepo.HardDeleteAllAsync(PrivateBotChat_Operations);

        Assert.Equivalent(
            tlgInputs,
            retrievedInputs);
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenUserIdNotExist()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITelegramUpdateGenerator>();
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        
        var tlgAgent = new TlgAgent(
            inputGenerator.Randomizer.GenerateRandomLong(),
            Default_UserAndChatId_PrivateBotChat,
            Operations);
    
        var retrievedInputs = 
            await inputRepo.GetAllInteractiveAsync(tlgAgent);
    
        Assert.Empty(
            retrievedInputs);
    }

    /* Main purpose is to verify that the Details column doesn't have values with outdated schema e.g. because
    its migration has been forgotten after the details schema evolved in the model/code. */ 
    // [Theory(Skip = "Waiting to migrate the old DB data")]
    // [Theory(Skip = "Running tests from unknown IP / internet")]
    [Theory]
    [InlineData(RealTestUser_DanielGorin_TelegramId, false)]
    [InlineData(RealTestUser_DanielGorin_TelegramId, true)]
    public async Task Verifies_Db_DoesNotHaveInvalidTestData_ForGivenTestUser(
        TlgUserId devDbUserId, bool overwriteDefaultDbConnProviderWithPrdDbConn)
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        
        if (overwriteDefaultDbConnProviderWithPrdDbConn)
        {
            var prdDbConnString = _services.GetRequiredService<PrdDbConnStringProvider>().Get;
            testOutputHelper.WriteLine(prdDbConnString);
            var serviceCollection = new IntegrationTestStartup().Services;
            serviceCollection.AddScoped<IDbConnectionProvider>(_ => 
                new DbConnectionProvider(prdDbConnString));
            _services = serviceCollection.BuildServiceProvider();
        }
        
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        
        // No assert needed: test fails when exception thrown!
        await inputRepo.GetAllInteractiveAsync(new TlgAgent(devDbUserId, devDbUserId.Id, Operations));
        await inputRepo.GetAllInteractiveAsync(new TlgAgent(devDbUserId, devDbUserId.Id, Communications));
        await inputRepo.GetAllInteractiveAsync(new TlgAgent(devDbUserId, devDbUserId.Id, Notifications));
    }
    
    [Fact]
    public async Task GetAllAsync_ReturnsCorrectInputs_ForGivenLiveEvent()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();

        var inputsX2024 = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                text: "Input for X 2024 1",
                roleSpecified: SaniCleanInspector_DanielEn_X2024),
            inputGenerator.GetValidTlgInputTextMessage(
                text: "Input for X 2024 2", 
                roleSpecified: SaniCleanInspector_DanielEn_X2024)
        };
        
        var inputsX2025 = new[]
        {
            inputGenerator.GetValidTlgInputTextMessage(
                text: "Input for X 2025 1",
                roleSpecified: SaniCleanInspector_DanielEn_X2025),
            inputGenerator.GetValidTlgInputTextMessage(
                text: "Input for X 2025 2",
                roleSpecified: SaniCleanInspector_DanielEn_X2025)
        };

        await inputRepo.AddAsync(
            inputsX2024
                .Concat(inputsX2025)
                .ToArray());
        
        var retrievedInputsX2024 = 
            (await inputRepo.GetAllInteractiveAsync(X2024))
            .ToImmutableReadOnlyCollection();
        var retrievedInputsX2025 = 
            (await inputRepo.GetAllInteractiveAsync(X2025))
            .ToImmutableReadOnlyCollection();
        
        await inputRepo.HardDeleteAllAsync(inputsX2024[0].TlgAgent);
        await inputRepo.HardDeleteAllAsync(inputsX2025[0].TlgAgent);
        
        Assert.Equal(
            2,
            retrievedInputsX2024.Count);
        Assert.Equal(
            2,
            retrievedInputsX2025.Count);
        Assert.All(
            retrievedInputsX2024,
            input => Assert.Equal("LiveEvent X 2024", input.LiveEventContext.GetValueOrThrow().Name));
        Assert.All(
            retrievedInputsX2025,
            input => Assert.Equal("LiveEvent X 2025", input.LiveEventContext.GetValueOrThrow().Name));
    }
    
    [Fact]
    public async Task GetAllLocationAsync_GetsOnlyLocations_FromGivenDate()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var inputGenerator = _services.GetRequiredService<ITlgInputGenerator>();
        var inputRepo = _services.GetRequiredService<ITlgInputsRepository>();
        var sinceParam = new DateTime(2024, 07, 01, 12, 15, 00);
        
        var tlgInputLongBefore = inputGenerator.GetValidTlgInputLocationMessage(
            new Geo( 13.4, 51.2, Option<double>.None()),
            dateTime: sinceParam.AddHours(-2));
        
        var tlgInputRightBefore = inputGenerator.GetValidTlgInputLocationMessage(
            new Geo(13.6, 51.7, Option<double>.None()),
            dateTime: sinceParam.AddMilliseconds(-1));

        var tlgInputExactlyAt = inputGenerator.GetValidTlgInputLocationMessage(
            new Geo(11.4, 47.2, Option<double>.None()),
            dateTime: sinceParam);
        
        var tlgInputAfter = inputGenerator.GetValidTlgInputLocationMessage(
            new Geo(11.5, 47.6, Option<double>.None()),
            dateTime: sinceParam.AddSeconds(1));

        await inputRepo.AddAsync(new List<TlgInput>
        {
            tlgInputLongBefore,
            tlgInputRightBefore,
            tlgInputExactlyAt,
            tlgInputAfter
        });
        
        var retrievedInputs = 
            await inputRepo.GetAllLocationAsync(
                PrivateBotChat_Operations, 
                sinceParam);
        var retrievedDates =
            retrievedInputs
                .Select(i => i.Details.TlgDate)
                .ToList();
        
        await inputRepo.HardDeleteAllAsync(PrivateBotChat_Operations);
        
        Assert.Contains(tlgInputExactlyAt.Details.TlgDate, retrievedDates);
        Assert.Contains(tlgInputAfter.Details.TlgDate, retrievedDates);
        Assert.DoesNotContain(tlgInputRightBefore.Details.TlgDate, retrievedDates);
        Assert.DoesNotContain(tlgInputLongBefore.Details.TlgDate, retrievedDates);
    }
}