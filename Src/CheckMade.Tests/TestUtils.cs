using CheckMade.Common.LangExt;
using CheckMade.Common.Model;
using CheckMade.Common.Model.Enums;
using CheckMade.Common.Model.Telegram.Updates;
using CheckMade.Common.Utils.Generic;
using CheckMade.Telegram.Function.Services.UpdateHandling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CheckMade.Tests;

internal interface ITestUtils
{
    // These string values to be exactly the same as in the corresponding .tsv translation files! 
    internal static readonly UiString EnglishUiStringForTests = Ui("English string for testing");
    internal const string GermanStringForTests = "Deutscher Text für Tests";
    
    Randomizer Randomizer { get; }
    
    TelegramUpdate GetValidModelInputTextMessage();
    TelegramUpdate GetValidModelInputTextMessage(TelegramUserId userId);
    TelegramUpdate GetValidModelInputTextMessageWithAttachment(AttachmentType type);
    TelegramUpdate GetValidModelInputCommandMessage(BotType botType, int botCommandEnumCode);
    
    UpdateWrapper GetValidTelegramTextMessage(string inputText);
    UpdateWrapper GetValidTelegramBotCommandMessage(string botCommand);
    UpdateWrapper GetValidTelegramUpdateWithCallbackQuery(string callbackQueryData);
    UpdateWrapper GetValidTelegramAudioMessage();
    UpdateWrapper GetValidTelegramDocumentMessage();
    UpdateWrapper GetValidTelegramLocationMessage(Option<float> horizontalAccuracy);
    UpdateWrapper GetValidTelegramPhotoMessage();
    UpdateWrapper GetValidTelegramVoiceMessage();
}

internal class TestUtils(Randomizer randomizer) : ITestUtils
{
    // Needs to be 'long' instead of 'TelegramUserId' for usage in InlineData() of Tests - but they implicitly convert
    internal const long TestUserDanielGorinTelegramId = 215737196L;
    internal const long TestChatId1 = 111111L;
    internal const long TestChatId2 = 222222L;
    internal const long TestChatId3 = 333333L;

    internal static readonly Role SanitaryOpsAdmin1 = new("VB70T", RoleType.SanitaryOps_Admin);
    internal static readonly Role SanitaryOpsInspector1 = new("3UDXW", RoleType.SanitaryOps_Inspector);
    internal static readonly Role SanitaryOpsEngineer1 = new("3UED8", RoleType.SanitaryOps_Engineer);
    internal static readonly Role SanitaryOpsCleanLead1 = new("2JXNM", RoleType.SanitaryOps_CleanLead);
    internal static readonly Role SanitaryOpsObserver1 = new("YEATF", RoleType.SanitaryOps_Observer);

    public Randomizer Randomizer { get; } = randomizer;
    
    public TelegramUpdate GetValidModelInputTextMessage() =>
        GetValidModelInputTextMessage(Randomizer.GenerateRandomLong());

    public TelegramUpdate GetValidModelInputTextMessage(TelegramUserId userId) =>
        new(userId,
            Randomizer.GenerateRandomLong(),
            BotType.Operations,
            ModelUpdateType.TextMessage,
            CreateFromRelevantDetails(
                DateTime.Now,
                1,
                $"Hello World, without attachment: {Randomizer.GenerateRandomLong()}"));
    
    public TelegramUpdate GetValidModelInputTextMessageWithAttachment(AttachmentType type) =>
        new(Randomizer.GenerateRandomLong(),
            Randomizer.GenerateRandomLong(),
            BotType.Operations,
            ModelUpdateType.AttachmentMessage,
            CreateFromRelevantDetails(
                DateTime.Now,
                1,
                $"Hello World, with attachment: {Randomizer.GenerateRandomLong()}",
                "fakeAttachmentUrl",
                type));

    public TelegramUpdate GetValidModelInputCommandMessage(BotType botType, int botCommandEnumCode) =>
        new(Randomizer.GenerateRandomLong(),
            Randomizer.GenerateRandomLong(),
            botType,
            ModelUpdateType.CommandMessage,
            CreateFromRelevantDetails(
                DateTime.Now,
                1,
                botCommandEnumCode: botCommandEnumCode));

    internal static TelegramUpdateDetails CreateFromRelevantDetails(
        DateTime telegramDate,
        int telegramMessageId,
        string? text = null,
        string? attachmentExternalUrl = null,
        AttachmentType? attachmentType = null,
        Geo? geoCoordinates = null,
        int? botCommandEnumCode = null,
        int? domainCategoryEnumCode = null,
        long? controlPromptEnumCode = null)
    {
        return new TelegramUpdateDetails(
            telegramDate, 
            telegramMessageId,
            text ?? Option<string>.None(),
            attachmentExternalUrl ?? Option<string>.None(),
            attachmentType ?? Option<AttachmentType>.None(),
            geoCoordinates ?? Option<Geo>.None(),
            botCommandEnumCode ?? Option<int>.None(),
            domainCategoryEnumCode ?? Option<int>.None(),
            controlPromptEnumCode ?? Option<long>.None());
    }
    
    public UpdateWrapper GetValidTelegramTextMessage(string inputText) => 
        new(new Message 
            {
                From = new User { Id = Randomizer.GenerateRandomLong() },
                Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
                Date = DateTime.Now,
                MessageId = 123,
                Text = inputText
            });

    public UpdateWrapper GetValidTelegramBotCommandMessage(string botCommand) =>
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
            Date = DateTime.Now,
            MessageId = 123,
            Text = botCommand,
            Entities = [
                new MessageEntity
                {
                    Length = botCommand.Length,
                    Offset = 0,
                    Type = MessageEntityType.BotCommand
                }
            ]
        });

    public UpdateWrapper GetValidTelegramUpdateWithCallbackQuery(string callbackQueryData) =>
        new(new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = callbackQueryData,
                Message = new Message
                {
                    From = new User { Id = Randomizer.GenerateRandomLong() },
                    Text = "The bot's original prompt",
                    Date = DateTime.Now,
                    Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
                    MessageId = 123,
                }
            }
        });

    public UpdateWrapper GetValidTelegramAudioMessage() => 
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakeAudioCaption",
            Audio = new Audio { FileId = "fakeAudioFileId" }
        });

    public UpdateWrapper GetValidTelegramDocumentMessage() => 
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakeDocumentCaption",
            Document = new Document { FileId = "fakeOtherDocumentFileId" }
        });

    public UpdateWrapper GetValidTelegramLocationMessage(Option<float> horizontalAccuracy) =>
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
            Date = DateTime.Now,
            MessageId = 123,
            Location = new Location
            {
                Latitude = 20.0123,
                Longitude = -17.4509,
                HorizontalAccuracy = horizontalAccuracy.IsSome 
                    ? horizontalAccuracy.GetValueOrDefault() 
                    : null
            }
        });

    public UpdateWrapper GetValidTelegramPhotoMessage() => 
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakePhotoCaption",
            Photo = [new PhotoSize{ Height = 1, Width = 1, FileSize = 100L, FileId = "fakePhotoFileId" }]
        });

    public UpdateWrapper GetValidTelegramVoiceMessage() =>
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = Randomizer.GenerateRandomLong() },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakeVoiceCaption",
            Voice = new Voice { FileId = "fakeVoiceFileId" }
        });
}