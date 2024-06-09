using CheckMade.Common.Interfaces.Persistence.Tlg;
using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Core.Enums;
using CheckMade.Common.Model.Tlg;
using CheckMade.Common.Model.Tlg.Updates;
using CheckMade.Common.Utils.UiTranslation;
using CheckMade.Telegram.Function.Services.BotClient;
using CheckMade.Telegram.Function.Services.Conversion;
using CheckMade.Telegram.Function.Services.UpdateHandling;
using CheckMade.Telegram.Logic.UpdateProcessors;
using CheckMade.Telegram.Logic.UpdateProcessors.Concrete;
using CheckMade.Telegram.Model.BotCommand;
using CheckMade.Telegram.Model.DTOs;
using CheckMade.Tests.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit.Abstractions;
using MessageType = Telegram.Bot.Types.Enums.MessageType;

namespace CheckMade.Tests.Unit.Telegram.Function;

public class UpdateHandlerTests(ITestOutputHelper outputHelper)
{
    private ServiceProvider? _services;

    [Fact]
    // Agnostic to BotType, using Operations
    public async Task HandleUpdateAsync_LogsWarningAndReturns_ForUnhandledMessageType()
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

        await basics.handler.HandleUpdateAsync(unhandledMessageTypeUpdate, TlgBotType.Operations);
        
        mockLogger.Verify(l => l.Log(
            LogLevel.Warning, 
            It.IsAny<EventId>(), 
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLoggedMessage)), 
            It.IsAny<Exception>(), 
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!));
    }
    
    [Fact]
    // Agnostic to BotType, using Operations
    public async Task HandleUpdateAsync_LogsError_WhenUpdateProcessorThrowsException()
    {
        var serviceCollection = new UnitTestStartup().Services;
        var mockIUpdateProcessorSelector = new Mock<IUpdateProcessorSelector>();
        var mockOperationsUpdateProcessor = new Mock<IOperationsUpdateProcessor>();
        
        mockOperationsUpdateProcessor
            .Setup(opr => opr.ProcessUpdateAsync(It.IsAny<Result<TlgUpdate>>()))
            .Throws<Exception>();
        mockIUpdateProcessorSelector
            .Setup(x => x.GetUpdateProcessor(TlgBotType.Operations))
            .Returns(mockOperationsUpdateProcessor.Object);
        
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ => mockIUpdateProcessorSelector.Object);
        var mockLogger = new Mock<ILogger<UpdateHandler>>();
        serviceCollection.AddScoped<ILogger<UpdateHandler>>(_ => mockLogger.Object);
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var textUpdate = basics.utils.GetValidTelegramTextMessage("random valid text");
        
        await basics.handler.HandleUpdateAsync(textUpdate, TlgBotType.Operations);
        
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
        
        await basics.handler.HandleUpdateAsync(invalidBotCommandUpdate, TlgBotType.Operations);
    
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                invalidBotCommandUpdate.Message.Chat.Id,
                It.IsAny<string>(),
                It.Is<string>(msg => msg.Contains(expectedErrorCode)),
                Option<IReplyMarkup>.None(),
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData(TlgBotType.Operations)]
    [InlineData(TlgBotType.Communications)]
    [InlineData(TlgBotType.Notifications)]
    public async Task HandleUpdateAsync_ShowsCorrectWelcomeMessage_UponStartCommand(TlgBotType botType)
    {
        _services = new UnitTestStartup().Services.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var startCommandUpdate = basics.utils.GetValidTelegramBotCommandMessage(TlgStart.Command);
        var expectedWelcomeMessageSegment = IUpdateProcessor.SeeValidBotCommandsInstruction.RawEnglishText;
        
        await basics.handler.HandleUpdateAsync(startCommandUpdate, botType);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                startCommandUpdate.Message.Chat.Id,
                It.IsAny<string>(),
                It.Is<string>(output => output.Contains(expectedWelcomeMessageSegment) && 
                                        output.Contains(botType.ToString())),
                Option<IReplyMarkup>.None(),
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    // Agnostic to BotType, using Operations
    public async Task HandleUpdateAsync_ReturnsEnglishTestString_ForEnglishLanguageCode()
    {
        var serviceCollection = new UnitTestStartup().Services;
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ => 
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(
                new List<OutputDto>{ new() { Text = ITestUtils.EnglishUiStringForTests } }));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var updateEn = basics.utils.GetValidTelegramTextMessage("random valid text");
        updateEn.Message.From!.LanguageCode = LanguageCode.en.ToString();
        
        await basics.handler.HandleUpdateAsync(updateEn, TlgBotType.Operations);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                updateEn.Message.Chat.Id,
                It.IsAny<string>(),
                ITestUtils.EnglishUiStringForTests.GetFormattedEnglish(),
                Option<IReplyMarkup>.None(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    // Agnostic to BotType, using Operations
    public async Task HandleUpdateAsync_ReturnsGermanTestString_ForGermanLanguageCode()
    {
        var serviceCollection = new UnitTestStartup().Services;
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ => 
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(
               new List<OutputDto>{ new() { Text = ITestUtils.EnglishUiStringForTests } }));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var updateDe = basics.utils.GetValidTelegramTextMessage("random valid text");
        updateDe.Message.From!.LanguageCode = LanguageCode.de.ToString();
        
        await basics.handler.HandleUpdateAsync(updateDe, TlgBotType.Operations);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                updateDe.Message.Chat.Id,
                It.IsAny<string>(),
                ITestUtils.GermanStringForTests,
                Option<IReplyMarkup>.None(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    // Agnostic to BotType, using Operations
    public async Task HandleUpdateAsync_ReturnsEnglishTestString_ForUnsupportedLanguageCode()
    {
        var serviceCollection = new UnitTestStartup().Services;
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ => 
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(
                new List<OutputDto>{ new() { Text = ITestUtils.EnglishUiStringForTests } }));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var updateUnsupportedLanguage = 
            basics.utils.GetValidTelegramTextMessage("random valid text");
        updateUnsupportedLanguage.Message.From!.LanguageCode = "xyz";
        
        await basics.handler.HandleUpdateAsync(updateUnsupportedLanguage, TlgBotType.Operations);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                updateUnsupportedLanguage.Message.Chat.Id,
                It.IsAny<string>(),
                ITestUtils.EnglishUiStringForTests.GetFormattedEnglish(),
                Option<IReplyMarkup>.None(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    // Agnostic to BotType, using Operations
    // Just to confirm basic integration. Detailed unit tests for correct Output->ReplyMarkup conversions are elsewhere.
    public async Task HandleUpdateAsync_SendsMessageWithCorrectReplyMarkup_ForOutputWithPrompts()
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        var outputWithPrompts = new List<OutputDto>{ 
            new ()
            {
              Text = ITestUtils.EnglishUiStringForTests,
              ControlPromptsSelection = new[] { ControlPrompts.Bad, ControlPrompts.Good } 
            }
        };
        
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ => 
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(outputWithPrompts));
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
        
        await basics.handler.HandleUpdateAsync(textUpdate, TlgBotType.Operations);

        Assert.Equivalent(expectedReplyMarkup, actualMarkup);
    }

    [Fact]
    public async Task HandleUpdateAsync_SendsMultipleMessages_ForListOfOutputDtos()
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputsMultiple = [ 
            new OutputDto { Text = UiNoTranslate("Output1") },
            new OutputDto { Text = UiNoTranslate("Output2") }
        ];
        
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ =>
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(outputsMultiple));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        
        await basics.handler.HandleUpdateAsync(update, TlgBotType.Operations);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Option<IReplyMarkup>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(outputsMultiple.Count));
    }

    [Fact]
    public async Task HandleUpdateAsync_SendsMessagesToSpecifiedLogicalPorts_WhenMappingsExist()
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputsWithLogicalPort = [
            new OutputDto
            { 
                LogicalPort = new TlgLogicPort(
                    TestUtils.SanitaryOpsInspector1, TlgBotType.Operations), 
                Text = UiNoTranslate("Output1: Send to Inspector1 on OperationsBot - mapping exists")   
            },
            new OutputDto
            {
                LogicalPort = new TlgLogicPort(
                    TestUtils.SanitaryOpsInspector1, TlgBotType.Communications),
                Text = UiNoTranslate("Output2: Send to Inspector1 on CommunicationsBot - mapping exists") 
            },
            new OutputDto
            {
                LogicalPort = new TlgLogicPort(
                    TestUtils.SanitaryOpsEngineer1, TlgBotType.Notifications),
                Text = UiNoTranslate("Output3: Send to Engineer1 on NotificationsBot - mapping exists)") 
            }
        ];
        
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ =>
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(outputsWithLogicalPort));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");

        var expectedSendParamSets = outputsWithLogicalPort
            .Select(output => new 
            {
                Text = output.Text.GetValueOrThrow().GetFormattedEnglish(),
                TelegramPortChatId = basics.roleByTelegramPort
                    .First(kvp => 
                        kvp.Value == output.LogicalPort.GetValueOrThrow().Role)
                    .Key.ChatId.Id
            });

        await basics.handler.HandleUpdateAsync(update, TlgBotType.Operations);

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

    [Fact]
    public async Task HandleUpdateAsync_SendsToCurrentlyReceivingChatId_WhenOutputDtoHasNoLogicalPort()
    {
        var serviceCollection = new UnitTestStartup().Services;
        const string fakeOutputMessage = "Output without port";
        const TlgBotType actualBotType = TlgBotType.Communications;
        
        List<OutputDto> outputWithoutPort = [ new OutputDto{ Text = UiNoTranslate(fakeOutputMessage) } ];
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ =>
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(
                outputWithoutPort, actualBotType));
        _services = serviceCollection.BuildServiceProvider();
        
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        update.Message.Chat.Id = 12345654321L;
        var expectedChatId = update.Message.Chat.Id;
    
        await basics.handler.HandleUpdateAsync(update, actualBotType);
        
        basics.mockBotClient.Verify(
            x => x.SendTextMessageAsync(
                expectedChatId,
                It.IsAny<string>(),
                fakeOutputMessage,
                It.IsAny<Option<IReplyMarkup>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Cannot test for correct botClient.MyBotType because the mockBotClient is not (yet) BotType-specific!
    }

    [Fact]
    public async Task HandleUpdateAsync_SendsMultipleAttachmentTypes_WhenOutputContainsThem()
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputWithMultipleAttachmentTypes =
        [
            new OutputDto
            {
                Attachments = new List<OutputAttachmentDetails>
                {
                    new(new Uri("https://www.gorin.de/fakeUri1.html"), 
                        AttachmentType.Photo, Option<UiString>.None()),
                    new(new Uri("https://www.gorin.de/fakeUri2.html"), 
                        AttachmentType.Photo, Option<UiString>.None()),
                    new(new Uri("https://www.gorin.de/fakeUri3.html"), 
                        AttachmentType.Voice, Option<UiString>.None()),
                    new(new Uri("https://www.gorin.de/fakeUri4.html"), 
                        AttachmentType.Document, Option<UiString>.None())
                } 
            }
        ];
        
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ =>
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(outputWithMultipleAttachmentTypes));
        _services = serviceCollection.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");

        await basics.handler.HandleUpdateAsync(update, TlgBotType.Operations);

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

    [Fact]
    // This test passing implies that the main Text and each attachment's caption are all seen by the user
    public async Task HandleUpdateAsync_SendsTextAndAttachments_ForOneOutputWithTextAndAttachments()
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
                        AttachmentType.Photo, Ui("Random caption for Attachment 1")),
                    new(new Uri("http://www.gorin.de/fakeUri2.html"), 
                        AttachmentType.Photo, Ui("Random caption for Attachment 2")),
                }
            }
        ];
    
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ =>
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(outputWithTextAndCaptions));
        _services = serviceCollection.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");
        
        await basics.handler.HandleUpdateAsync(update, TlgBotType.Operations);
        
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
    
    [Fact]
    public async Task HandleUpdateAsync_SendsLocation_WhenOutputContainsOne()
    {
        var serviceCollection = new UnitTestStartup().Services;
        
        List<OutputDto> outputWithLocation =
        [
            new OutputDto
            {
                Location = new Geo(35.098, -17.077, Option<float>.None()) 
            }
        ];
        
        serviceCollection.AddScoped<IUpdateProcessorSelector>(_ =>
            GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(outputWithLocation));
        _services = serviceCollection.BuildServiceProvider();
        var basics = GetBasicTestingServices(_services);
        var update = basics.utils.GetValidTelegramTextMessage("random valid text");

        await basics.handler.HandleUpdateAsync(update, TlgBotType.Operations);
        
        basics.mockBotClient.Verify(
            x => x.SendLocationAsync(
                It.IsAny<ChatId>(),
                It.Is<Geo>(geo => geo == outputWithLocation[0].Location),
                It.IsAny<Option<IReplyMarkup>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    private static (ITestUtils utils, Mock<IBotClientWrapper> mockBotClient, IUpdateHandler handler, IOutputToReplyMarkupConverterFactory markupConverterFactory, IUiTranslator emptyTranslator, IDictionary<TlgClientPort, Role> roleByTelegramPort)
        GetBasicTestingServices(IServiceProvider sp) => 
            (sp.GetRequiredService<ITestUtils>(), 
                sp.GetRequiredService<Mock<IBotClientWrapper>>(),
                sp.GetRequiredService<IUpdateHandler>(),
                sp.GetRequiredService<IOutputToReplyMarkupConverterFactory>(),
                new UiTranslator(Option<IReadOnlyDictionary<string, string>>.None(), 
                    sp.GetRequiredService<ILogger<UiTranslator>>()),
                sp.GetRequiredService<ITlgClientPortToRoleMapRepository>().GetAllAsync()
                    .Result
                    .ToDictionary(
                        keySelector: map => map.ClientPort,
                        elementSelector: map => map.Role)
                );

    // Useful when we need to mock up what Telegram.Logic returns, e.g. to test Telegram.Function related mechanics
    private static IUpdateProcessorSelector 
        GetMockSelectorForOperationsUpdateProcessorWithSetUpReturnValue(
            IReadOnlyList<OutputDto> returnValue, TlgBotType botType = TlgBotType.Operations)
    {
        var mockOperationsUpdateProcessor = new Mock<IOperationsUpdateProcessor>();
        
        mockOperationsUpdateProcessor
            .Setup<Task<IReadOnlyList<OutputDto>>>(rp => 
                rp.ProcessUpdateAsync(It.IsAny<Result<TlgUpdate>>()))
            .Returns(Task.FromResult(returnValue));

        var mockUpdateProcessorSelector = new Mock<IUpdateProcessorSelector>();
        
        mockUpdateProcessorSelector
            .Setup(rps => 
                rps.GetUpdateProcessor(botType))
            .Returns(mockOperationsUpdateProcessor.Object);

        return mockUpdateProcessorSelector.Object;
    }
}
