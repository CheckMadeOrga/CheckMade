using System.Collections.Immutable;
using CheckMade.Common.ExternalServices.ExternalUtils;
using CheckMade.Common.Interfaces.ExternalServices.AzureServices;
using CheckMade.Common.Interfaces.Persistence.Core;
using CheckMade.Common.Model.Utils;
using CheckMade.ChatBot.Function.Services.BotClient;
using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.UserInteraction;
using CheckMade.Tests.Startup.DefaultMocks;
using CheckMade.Tests.Startup.DefaultMocks.Repositories.ChatBot;
using CheckMade.Tests.Startup.DefaultMocks.Repositories.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using File = Telegram.Bot.Types.File;

namespace CheckMade.Tests.Startup;

[UsedImplicitly]
public class UnitTestStartup : TestStartupBase
{
    public UnitTestStartup()
    {
        RegisterServices();
    }

    protected override void RegisterTestTypeSpecificServices()
    {
        RegisterPersistenceMocks();
        RegisterBotClientMocks();
        RegisterExternalServicesMocks();        
    }

    private void RegisterBotClientMocks()
    {
        Services.AddScoped<IBotClientFactory, MockBotClientFactory>(sp => 
            new MockBotClientFactory(sp.GetRequiredService<Mock<IBotClientWrapper>>()));

        /* Adding Mock<IBotClientWrapper> into the D.I. container is necessary so that I can inject the same instance
         in my tests that is also used by the MockBotClientFactory below. This way I can verify behaviour on the
         mockBotClientWrapper without explicitly setting up the mock in the unit test itself.

         We choose 'AddScoped' because we want our dependencies scoped to the execution of each test method.
         That's why each test method creates its own ServiceProvider. That prevents:

         a) interference between test runs e.g. because of shared state in some dependency (which could e.g.
         falsify Moq's behaviour 'verifications'

         b) having two instanced of e.g. mockBotClientWrapper within a single test-run, when only one is expected
        */ 
        Services.AddScoped<Mock<IBotClientWrapper>>(_ =>
        {
            var mockBotClientWrapper = new Mock<IBotClientWrapper>();
            
            mockBotClientWrapper
                .Setup(x => x.GetFileAsync(It.IsNotNull<string>()))
                .ReturnsAsync(new File { FilePath = "fakeFilePath" });
            mockBotClientWrapper
                .Setup(x => x.MyBotToken)
                .Returns("fakeToken");
            
            return mockBotClientWrapper;
        });
    }

    private void RegisterPersistenceMocks()
    {
        Services.AddScoped<IRoleRepository, MockRoleRepository>();
        
        Services.AddScoped<ITlgInputRepository, MockTlgInputRepository>(_ => 
            new MockTlgInputRepository(new Mock<ITlgInputRepository>()));
        
        var mockTlgClientPortModeRoleRepo = new Mock<ITlgClientPortModeRoleRepository>();

        mockTlgClientPortModeRoleRepo
            .Setup(cpmr => cpmr.GetAllAsync())
            .ReturnsAsync(GetTestingPortModeRoles());

        Services.AddScoped<ITlgClientPortModeRoleRepository>(_ => mockTlgClientPortModeRoleRepo.Object);
        Services.AddScoped<Mock<ITlgClientPortModeRoleRepository>>(_ => mockTlgClientPortModeRoleRepo);
    }

    private void RegisterExternalServicesMocks()
    {
        Services.AddScoped<IBlobLoader, MockBlobLoader>();
        Services.AddScoped<IHttpDownloader, MockHttpDownloader>(); 
    }
    
    private static ImmutableArray<TlgClientPortModeRole> GetTestingPortModeRoles()
    {
        var builder = ImmutableArray.CreateBuilder<TlgClientPortModeRole>();

        // #1
        
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsAdmin1, 
            new TlgClientPort(ITestUtils.TestUserId_01, ITestUtils.TestChatId_01),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        // Group: same Role & ClientPort - all three InteractionModes
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsInspector1, 
            new TlgClientPort(ITestUtils.TestUserId_01, ITestUtils.TestChatId_02),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsInspector1, 
            new TlgClientPort(ITestUtils.TestUserId_01, ITestUtils.TestChatId_02),
            InteractionMode.Communications,
            DateTime.Now, Option<DateTime>.None()));

        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsInspector1, 
            new TlgClientPort(ITestUtils.TestUserId_01, ITestUtils.TestChatId_02),
            InteractionMode.Notifications,
            DateTime.Now, Option<DateTime>.None()));


        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsEngineer1, 
            new TlgClientPort(ITestUtils.TestUserId_02, ITestUtils.TestChatId_03),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        // Expired on purpose - for Unit Tests!
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsEngineer1, 
            new TlgClientPort(ITestUtils.TestUserId_02, ITestUtils.TestChatId_03),
            InteractionMode.Operations,
            new DateTime(1999, 01, 01), new DateTime(1999, 02, 02), 
            DbRecordStatus.Historic));

        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsCleanLead1, 
            new TlgClientPort(ITestUtils.TestUserId_02, ITestUtils.TestChatId_04),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsObserver1, 
            new TlgClientPort(ITestUtils.TestUserId_03, ITestUtils.TestChatId_05),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        // #2
        
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsEngineer2, 
            new TlgClientPort(ITestUtils.TestUserId_03 , ITestUtils.TestChatId_06),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        builder.Add(new TlgClientPortModeRole(
            ITestUtils.SanitaryOpsCleanLead2, 
            new TlgClientPort(ITestUtils.TestUserId_03, ITestUtils.TestChatId_07),
            InteractionMode.Operations,
            DateTime.Now, Option<DateTime>.None()));
        
        // No TlgClientPortModeRole for role 'Inspector2' on purpose for Unit Test, e.g.
        // GetNextOutputAsync_CreatesPortModeRole_WithConfirmation_WhenValidTokenSubmitted_FromChatGroup

        return builder.ToImmutable();
    }
}
