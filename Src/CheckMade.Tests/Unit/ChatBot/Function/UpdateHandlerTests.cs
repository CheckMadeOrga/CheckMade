using CheckMade.ChatBot.Function.Services.BotClient;
using CheckMade.ChatBot.Function.Services.Conversion;
using CheckMade.ChatBot.Function.Services.UpdateHandling;
using CheckMade.ChatBot.Logic;
using CheckMade.Common.Interfaces.Persistence.ChatBot;
using CheckMade.Common.Model.ChatBot;
using CheckMade.Common.Model.ChatBot.Input;
using CheckMade.Common.Model.ChatBot.Output;
using CheckMade.Common.Model.ChatBot.UserInteraction;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Utils;
using CheckMade.Common.Utils.UiTranslation;
using CheckMade.Tests.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit.Abstractions;
using static CheckMade.Common.Model.ChatBot.UserInteraction.InteractionMode;
using MessageType = Telegram.Bot.Types.Enums.MessageType;

namespace CheckMade.Tests.Unit.ChatBot.Function;

public class UpdateHandlerTests(ITestOutputHelper outputHelper)
{
    private ServiceProvider? _services;

    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_LogsWarningAndReturns_ForUnhandledMessageType(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        var mockLogger = new Mock<ILogger<UpdateHandler>>();
        serviceCollection.AddScoped<ILogger<UpdateHandler>>(_ => mockLogger.Object);
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        // type 'Unknown' is derived by Telegram for lack of any props!
        var unhandledMessageTypeUpdate = new UpdateWrapper(new Message { Chat = new Chat { Id = 123L } });
        var expectedLoggedMessage = $"Received message of type '{MessageType.Unknown}': " +
                                    $"{BotUpdateSwitch.NoSpecialHandlingWarning.GetFormattedEnglish()}";

        await basics.handler.HandleUpdateAsync(unhandledMessageTypeUpdate, mode);
        
        mockLogger.Verify(l => l.Log(
            LogLevel.Warning, 
            It.IsAny<EventId>(), 
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLoggedMessage)), 
            It.IsAny<Exception>(), 
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!));
    }
    
    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_LogsError_WhenInputProcessorThrowsException(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        var mockInputProcessorFactory = new Mock<IInputProcessorFactory>();
        var mockOperationsInputProcessor = new Mock<IInputProcessor>();
        
        mockOperationsInputProcessor
            .Setup(opr => opr.ProcessInputAsync(It.IsAny<Result<TlgInput>>()))
            .Throws<Exception>();
        mockInputProcessorFactory
            .Setup(x => x.GetInputProcessor(mode))
            .Returns(mockOperationsInputProcessor.Object);
        
        serviceCollection.AddScoped<IInputProcessorFactory>(_ => mockInputProcessorFactory.Object);
        var mockLogger = new Mock<ILogger<UpdateHandler>>();
        serviceCollection.AddScoped<ILogger<UpdateHandler>>(_ => mockLogger.Object);
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var textUpdate = basics.utils.GetValidTelegramTextMessage("random valid text");
        
        await basics.handler.HandleUpdateAsync(textUpdate, mode);
        
        mockLogger.Verify(l => l.Log(
            LogLevel.Error, 
            It.IsAny<EventId>(), 
            It.IsAny<It.IsAnyType>(), 
            It.IsAny<Exception>(), 
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!));
    }

    [Fact]
    public async Task HandleUpdateAsync_ShowsCorrectError_ForInvalidBotCommandToOperations()
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        const string invalidBotCommand = "/invalid";
        var invalidBotCommandUpdate = basics.utils.GetValidTelegramBotCommandMessage(invalidBotCommand);
        const string expectedErrorCode = "W3DL9";
    
        // Writing out to OutputHelper to see the entire error message, as an additional manual verification
        basics.mockBotClient
            .Setup(
                x => x.SendTextMessageAsync(
                    invalidBotCommandUpdate.Message.Chat.Id,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    Option<IReplyMarkup>.None(),
                    It.IsAny<CancellationToken>()))
            .Callback<ChatId, string, string, Option<IReplyMarkup>, CancellationToken>((_, msg, _, _, _) => 
                outputHelper.WriteLine(msg));
        
        await basics.handler.HandleUpdateAsync(invalidBotCommandUpdate, Operations);
    
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                invalidBotCommandUpdate.Message.Chat.Id,
                It.IsAny<string>(),
                It.Is<string>(msg => msg.Contains(expectedErrorCode)),
                Option<IReplyMarkup>.None(),
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    // ToDo: irrelevant or needs total change once switch to manual language control
    #region Old LanguageCode Tests
    
    // [Theory]
    // [InlineData(InteractionMode.Operations)]
    // [InlineData(InteractionMode.Communications)]
    // [InlineData(InteractionMode.Notifications)]
    // public async Task HandleUpdateAsync_ReturnsEnglishTestString_ForEnglishLanguageCode(InteractionMode mode)
    // {
    //     var serviceCollection = new UnitTestStartup().Services;
    //     serviceCollection.AddScoped<IInputProcessorFactory>(_ => 
    //         GetMockInputProcessorFactoryWithSetUpReturnValue(
    //             new List<OutputDto>{ new() { Text = ITestUtils.EnglishUiStringForTests } }));
    //     _services = serviceCollection.BuildServiceProvider();
    //     
    //     var basics = GetBasicTestingServices(_services);
    //     var updateEn = basics.utils.GetValidTelegramTextMessage("random valid text");
    //     updateEn.Message.From!.LanguageCode = LanguageCode.en.ToString();
    //     
    //     await basics.handler.HandleUpdateAsync(updateEn, mode);
    //     
    //     basics.mockBotClient.Verify(
    //         x => x.SendTextMessageAsync(
    //             updateEn.Message.Chat.Id,
    //             It.IsAny<string>(),
    //             ITestUtils.EnglishUiStringForTests.GetFormattedEnglish(),
    //             Option<IReplyMarkup>.None(),
    //             It.IsAny<CancellationToken>()));
    // }
    //
    // [Fact]
    // // Agnostic to InteractionMode, using Operations
    // public async Task HandleUpdateAsync_ReturnsGermanTestString_ForGermanLanguageCode()
    // {
    //     var serviceCollection = new UnitTestStartup().Services;
    //     serviceCollection.AddScoped<IInputProcessorFactory>(_ => 
    //         GetMockInputProcessorFactoryWithSetUpReturnValue(
    //            new List<OutputDto>{ new() { Text = ITestUtils.EnglishUiStringForTests } }));
    //     _services = serviceCollection.BuildServiceProvider();
    //     
    //     var basics = GetBasicTestingServices(_services);
    //     var updateDe = basics.utils.GetValidTelegramTextMessage("random valid text");
    //     updateDe.Message.From!.LanguageCode = LanguageCode.de.ToString();
    //     
    //     await basics.handler.HandleUpdateAsync(updateDe, InteractionMode.Operations);
    //     
    //     basics.mockBotClient.Verify(
    //         x => x.SendTextMessageAsync(
    //             updateDe.Message.Chat.Id,
    //             It.IsAny<string>(),
    //             ITestUtils.GermanStringForTests,
    //             Option<IReplyMarkup>.None(),
    //             It.IsAny<CancellationToken>()));
    // }
    //
    // [Fact]
    // // Agnostic to InteractionMode, using Operations
    // public async Task HandleUpdateAsync_ReturnsEnglishTestString_ForUnsupportedLanguageCode()
    // {
    //     var serviceCollection = new UnitTestStartup().Services;
    //     serviceCollection.AddScoped<IInputProcessorFactory>(_ => 
    //         GetMockInputProcessorFactoryWithSetUpReturnValue(
    //             new List<OutputDto>{ new() { Text = ITestUtils.EnglishUiStringForTests } }));
    //     _services = serviceCollection.BuildServiceProvider();
    //     
    //     var basics = GetBasicTestingServices(_services);
    //     var updateUnsupportedLanguage = 
    //         basics.utils.GetValidTelegramTextMessage("random valid text");
    //     updateUnsupportedLanguage.Message.From!.LanguageCode = "xyz";
    //     
    //     await basics.handler.HandleUpdateAsync(updateUnsupportedLanguage, InteractionMode.Operations);
    //     
    //     basics.mockBotClient.Verify(
    //         x => x.SendTextMessageAsync(
    //             updateUnsupportedLanguage.Message.Chat.Id,
    //             It.IsAny<string>(),
    //             ITestUtils.EnglishUiStringForTests.GetFormattedEnglish(),
    //             Option<IReplyMarkup>.None(),
    //             It.IsAny<CancellationToken>()));
    // }

    #endregion
    
    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    // Just to confirm basic integration. Detailed unit tests for correct Output->ReplyMarkup conversions are elsewhere.
    public async Task HandleUpdateAsync_SendsMessageWithCorrectReplyMarkup_ForOutputWithPrompts(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        var outputWithPrompts = new List<OutputDto>{ 
            new ()
            {
              Text = ITestUtils.EnglishUiStringForTests,
              ControlPromptsSelection = ControlPrompts.Bad | ControlPrompts.Good 
            }
        };
        
        serviceCollection.AddScoped<IInputProcessorFactory>(_ => 
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputWithPrompts, mode));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var textUpdate = basics.utils.GetValidTelegramTextMessage("random valid text");
        var converter = basics.markupConverterFactory.Create(basics.emptyTranslator);
        var expectedReplyMarkup = converter.GetReplyMarkup(outputWithPrompts[0]);
        
        var actualMarkup = Option<IReplyMarkup>.None();
        basics.mockBotClient
            .Setup(
                x => x.SendTextMessageAsync(
                    It.IsAny<ChatId>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Option<IReplyMarkup>>(),
                    It.IsAny<CancellationToken>())
            )
            .Callback<ChatId, string, string, Option<IReplyMarkup>, CancellationToken>(
                (_, _, _, markup, _) => actualMarkup = markup
            );
        
        await basics.handler.HandleUpdateAsync(textUpdate, mode);

        Assert.Equivalent(expectedReplyMarkup, actualMarkup);
    }

    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_SendsMultipleMessages_ForListOfOutputDtos(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputsMultiple = [ 
            new OutputDto { Text = UiNoTranslate("Output1") },
            new OutputDto { Text = UiNoTranslate("Output2") }
        ];
        
        serviceCollection.AddScoped<IInputProcessorFactory>(_ =>
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputsMultiple, mode));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        
        await basics.handler.HandleUpdateAsync(update, mode);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Option<IReplyMarkup>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(outputsMultiple.Count));
    }

    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_SendsMessagesToSpecifiedLogicalPorts_WhenTlgClientPortRolesExist(
        InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputsWithLogicalPort = [
            new OutputDto
            { 
                LogicalPort = new LogicalPort(
                    ITestUtils.SanitaryOpsInspector1, Operations), 
                Text = UiNoTranslate("Output1: Send to Inspector1 on OperationsBot")   
            },
            new OutputDto
            {
                LogicalPort = new LogicalPort(
                    ITestUtils.SanitaryOpsInspector1, Communications),
                Text = UiNoTranslate("Output2: Send to Inspector1 on CommunicationsBot") 
            },
            new OutputDto
            {
                LogicalPort = new LogicalPort(
                    ITestUtils.SanitaryOpsEngineer1, Notifications),
                Text = UiNoTranslate("Output3: Send to Engineer1 on NotificationsBot)") 
            }
        ];
        
        serviceCollection.AddScoped<IInputProcessorFactory>(_ =>
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputsWithLogicalPort, mode));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        var portRoles = await basics.portRoleTask;
        
        var expectedSendParamSets = outputsWithLogicalPort
            .Select(output => new 
            {
                Text = output.Text.GetValueOrThrow().GetFormattedEnglish(),
                
                TelegramPortChatId = portRoles
                    .Where(cpr => 
                        cpr.Role == output.LogicalPort.GetValueOrThrow().Role &&
                        cpr.Status == DbRecordStatus.Active)
                    .MaxBy(cpr => cpr.ActivationDate)!
                    .ClientPort.ChatId.Id
            });

        await basics.handler.HandleUpdateAsync(update, mode);

        foreach (var expectedParamSet in expectedSendParamSets)
        {
            basics.mockBotClient.Verify(
                x => x.SendTextMessageAsync(
                    expectedParamSet.TelegramPortChatId,
                    It.IsAny<string>(),
                    expectedParamSet.Text,
                    It.IsAny<Option<IReplyMarkup>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_SendsToCurrentlyReceivingChatId_WhenOutputDtoHasNoLogicalPort(
        InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        const string fakeOutputMessage = "Output without port";
        
        List<OutputDto> outputWithoutPort = [ new OutputDto{ Text = UiNoTranslate(fakeOutputMessage) } ];
        serviceCollection.AddScoped<IInputProcessorFactory>(_ =>
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputWithoutPort, mode));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        update.Message.Chat.Id = 12345654321L;
        var expectedChatId = update.Message.Chat.Id;
    
        await basics.handler.HandleUpdateAsync(update, mode);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                expectedChatId,
                It.IsAny<string>(),
                fakeOutputMessage,
                It.IsAny<Option<IReplyMarkup>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Cannot test for correct botClient.MyInteractionMode because the mockBotClient is not (yet) InteractionMode-specific!
    }

    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_SendsMultipleAttachmentTypes_WhenOutputContainsThem(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputWithMultipleAttachmentTypes =
        [
            new OutputDto
            {
                Attachments = new List<OutputAttachmentDetails>
                {
                    new(new Uri("https://www.gorin.de/fakeUri1.html"), 
                        TlgAttachmentType.Photo, Option<UiString>.None()),
                    new(new Uri("https://www.gorin.de/fakeUri2.html"), 
                        TlgAttachmentType.Photo, Option<UiString>.None()),
                    new(new Uri("https://www.gorin.de/fakeUri3.html"), 
                        TlgAttachmentType.Voice, Option<UiString>.None()),
                    new(new Uri("https://www.gorin.de/fakeUri4.html"), 
                        TlgAttachmentType.Document, Option<UiString>.None())
                } 
            }
        ];
        
        serviceCollection.AddScoped<IInputProcessorFactory>(_ =>
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputWithMultipleAttachmentTypes, mode));
        _services = serviceCollection.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");

        await basics.handler.HandleUpdateAsync(update, mode);

        basics.mockBotClient.Verify(
            x => x.SendPhotoAsync(
                It.IsAny<AttachmentSendOutParameters>(),
                It.IsAny<CancellationToken>()),
                Times.Exactly(2));

        basics.mockBotClient.Verify(
            x => x.SendVoiceAsync(
                It.IsAny<AttachmentSendOutParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(1));
        
        basics.mockBotClient.Verify(
            x => x.SendDocumentAsync(
                It.IsAny<AttachmentSendOutParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(1));
    }

    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    // This test passing implies that the main Text and each attachment's caption are all seen by the user
    public async Task HandleUpdateAsync_SendsTextAndAttachments_ForOneOutputWithTextAndAttachments(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        const string mainText = "This is the main text describing all attachments";
        
        List<OutputDto> outputWithTextAndCaptions =
        [
            new OutputDto
            {
                Text = UiNoTranslate(mainText),
                Attachments = new List<OutputAttachmentDetails>
                {
                    new(new Uri("http://www.gorin.de/fakeUri1.html"), 
                        TlgAttachmentType.Photo, Ui("Random caption for Attachment 1")),
                    new(new Uri("http://www.gorin.de/fakeUri2.html"), 
                        TlgAttachmentType.Photo, Ui("Random caption for Attachment 2")),
                }
            }
        ];
    
        serviceCollection.AddScoped<IInputProcessorFactory>(_ =>
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputWithTextAndCaptions, mode));
        _services = serviceCollection.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        
        await basics.handler.HandleUpdateAsync(update, mode);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(), 
                It.IsAny<string>(),
                mainText,
                It.IsAny<Option<IReplyMarkup>>(), 
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        basics.mockBotClient.Verify(
            x => x.SendPhotoAsync(
                It.IsAny<AttachmentSendOutParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
    
    [Theory]
    [InlineData(Operations)]
    [InlineData(Communications)]
    [InlineData(Notifications)]
    public async Task HandleUpdateAsync_SendsLocation_WhenOutputContainsOne(InteractionMode mode)
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputWithLocation =
        [
            new OutputDto
            {
                Location = new Geo(35.098, -17.077, Option<float>.None()) 
            }
        ];
        
        serviceCollection.AddScoped<IInputProcessorFactory>(_ =>
            GetMockInputProcessorFactoryWithSetUpReturnValue(outputWithLocation, mode));
        _services = serviceCollection.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");

        await basics.handler.HandleUpdateAsync(update, mode);
        
        basics.mockBotClient.Verify(
            x => x.SendLocationAsync(
                It.IsAny<ChatId>(),
                It.Is<Geo>(geo => geo == outputWithLocation[0].Location),
                It.IsAny<Option<IReplyMarkup>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    private static (ITestUtils utils, 
        Mock<IBotClientWrapper> mockBotClient, 
        IUpdateHandler handler, 
        IOutputToReplyMarkupConverterFactory markupConverterFactory, 
        IUiTranslator emptyTranslator, 
        Task<IEnumerable<TlgClientPortRole>> portRoleTask)
        GetBasicTestingServices(IServiceProvider sp) => 
            (sp.GetRequiredService<ITestUtils>(), 
                sp.GetRequiredService<Mock<IBotClientWrapper>>(),
                sp.GetRequiredService<IUpdateHandler>(),
                sp.GetRequiredService<IOutputToReplyMarkupConverterFactory>(),
                new UiTranslator(Option<IReadOnlyDictionary<string, string>>.None(), 
                    sp.GetRequiredService<ILogger<UiTranslator>>()),
                sp.GetRequiredService<ITlgClientPortRoleRepository>().GetAllAsync());

    // Useful when we need to mock up what ChatBot.Logic returns, e.g. to test ChatBot.Function related mechanics
    private static IInputProcessorFactory 
        GetMockInputProcessorFactoryWithSetUpReturnValue(
            IReadOnlyList<OutputDto> returnValue, InteractionMode interactionMode = Operations)
    {
        var mockInputProcessor = new Mock<IInputProcessor>();
        
        mockInputProcessor
            .Setup<Task<IReadOnlyList<OutputDto>>>(rp => 
                rp.ProcessInputAsync(It.IsAny<Result<TlgInput>>()))
            .Returns(Task.FromResult(returnValue));

        var mockInputProcessorFactory = new Mock<IInputProcessorFactory>();
        
        mockInputProcessorFactory
            .Setup(rps => 
                rps.GetInputProcessor(interactionMode))
            .Returns(mockInputProcessor.Object);

        return mockInputProcessorFactory.Object;
    }
}