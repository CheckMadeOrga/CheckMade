using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Core.Enums;
using CheckMade.Common.Model.Tlg.Input;
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

    // Needs to be 'long' instead of 'TlgUserId' for usage in InlineData() of Tests - but they implicitly convert
    internal const long TestUserDanielGorinTelegramId = 215737196L;
    internal const long TestUserId_01 = 101L;
    internal const long TestUserId_02 = 102L;
    internal const long TestUserId_03 = 103L;
    
    internal const long TestChatId_01 = 100001L;
    internal const long TestChatId_02 = 100002L;
    internal const long TestChatId_03 = 100003L;
    internal const long TestChatId_04 = 100004L;
    internal const long TestChatId_05 = 100005L;
    internal const long TestChatId_06 = 100006L;
    internal const long TestChatId_07 = 100007L;
    internal const long TestChatId_08 = 100008L;
    internal const long TestChatId_09 = 100009L;
    
    Randomizer Randomizer { get; }
    
    TlgUpdate GetValidTlgTextMessage(long userId = TestUserId_01, long chatId = TestChatId_01);
    TlgUpdate GetValidTlgTextMessageWithAttachment(AttachmentType type);
    TlgUpdate GetValidTlgCommandMessage(
        TlgBotType botType, int botCommandEnumCode, long userId = TestUserId_01, long chatId = TestChatId_01);
    
    UpdateWrapper GetValidTelegramTextMessage(string inputText, long chatId = TestChatId_01);
    UpdateWrapper GetValidTelegramBotCommandMessage(string botCommand, long chatId = TestChatId_01);
    UpdateWrapper GetValidTelegramUpdateWithCallbackQuery(string callbackQueryData, long chatId = TestChatId_01);
    UpdateWrapper GetValidTelegramAudioMessage(long chatId = TestChatId_01);
    UpdateWrapper GetValidTelegramDocumentMessage(long chatId = TestChatId_01, string fileId = "fakeOtherDocumentFileId");
    UpdateWrapper GetValidTelegramLocationMessage(Option<float> horizontalAccuracy, long chatId = TestChatId_01);
    UpdateWrapper GetValidTelegramPhotoMessage(long chatId = TestChatId_01);
    UpdateWrapper GetValidTelegramVoiceMessage(long chatId = TestChatId_01);
}

internal class TestUtils(Randomizer randomizer) : ITestUtils
{
    internal static readonly Role SanitaryOpsAdmin1 = new("VB70T", RoleType.SanitaryOps_Admin);
    
    internal static readonly Role SanitaryOpsInspector1 = new("3UDXW", RoleType.SanitaryOps_Inspector);
    internal static readonly Role SanitaryOpsEngineer1 = new("3UED8", RoleType.SanitaryOps_Engineer);
    internal static readonly Role SanitaryOpsCleanLead1 = new("2JXNM", RoleType.SanitaryOps_CleanLead);
    internal static readonly Role SanitaryOpsObserver1 = new("YEATF", RoleType.SanitaryOps_Observer);

    internal static readonly Role SanitaryOpsInspector2 = new("MAM8S", RoleType.SanitaryOps_Inspector);
    internal static readonly Role SanitaryOpsEngineer2 = new("P4XPK", RoleType.SanitaryOps_Engineer);
    internal static readonly Role SanitaryOpsCleanLead2 = new("I8MJ1", RoleType.SanitaryOps_CleanLead);
    internal static readonly Role SanitaryOpsObserver2 = new("67CMC", RoleType.SanitaryOps_Observer);
    
    public Randomizer Randomizer { get; } = randomizer;
    
    public TlgUpdate GetValidTlgTextMessage(long userId, long chatId) =>
        new(userId,
            chatId,
            TlgBotType.Operations,
            TlgUpdateType.TextMessage,
            CreateFromRelevantDetails(
                DateTime.Now,
                1,
                $"Hello World, without attachment: {Randomizer.GenerateRandomLong()}"));
    
    public TlgUpdate GetValidTlgTextMessageWithAttachment(AttachmentType type) =>
        new(Randomizer.GenerateRandomLong(),
            Randomizer.GenerateRandomLong(),
            TlgBotType.Operations,
            TlgUpdateType.AttachmentMessage,
            CreateFromRelevantDetails(
                DateTime.Now,
                1,
                $"Hello World, with attachment: {Randomizer.GenerateRandomLong()}",
                new Uri("fakeTelegramUri"),
                new Uri("fakeInternalUri"),
                type));

    public TlgUpdate GetValidTlgCommandMessage(
        TlgBotType botType, int botCommandEnumCode, long userId, long chatId) =>
        new(userId,
            chatId,
            botType,
            TlgUpdateType.CommandMessage,
            CreateFromRelevantDetails(
                DateTime.Now,
                1,
                botCommandEnumCode: botCommandEnumCode));

    internal static TlgUpdateDetails CreateFromRelevantDetails(
        DateTime telegramDate,
        int telegramMessageId,
        string? text = null,
        Uri? attachmentTlgUri = null,
        Uri? attachmentInternalUri = null,
        AttachmentType? attachmentType = null,
        Geo? geoCoordinates = null,
        int? botCommandEnumCode = null,
        int? domainCategoryEnumCode = null,
        long? controlPromptEnumCode = null)
    {
        return new TlgUpdateDetails(
            telegramDate, 
            telegramMessageId,
            text ?? Option<string>.None(),
            attachmentTlgUri ?? Option<Uri>.None(),
            attachmentInternalUri ?? Option<Uri>.None(), 
            attachmentType ?? Option<AttachmentType>.None(),
            geoCoordinates ?? Option<Geo>.None(),
            botCommandEnumCode ?? Option<int>.None(),
            domainCategoryEnumCode ?? Option<int>.None(),
            controlPromptEnumCode ?? Option<long>.None());
    }
    
    public UpdateWrapper GetValidTelegramTextMessage(string inputText, long chatId) => 
        new(new Message 
            {
                From = new User { Id = Randomizer.GenerateRandomLong() },
                Chat = new Chat { Id = chatId },
                Date = DateTime.Now,
                MessageId = 123,
                Text = inputText
            });

    public UpdateWrapper GetValidTelegramBotCommandMessage(string botCommand, long chatId) =>
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = chatId },
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

    public UpdateWrapper GetValidTelegramUpdateWithCallbackQuery(
        string callbackQueryData, long chatId) =>
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
                    Chat = new Chat { Id = chatId },
                    MessageId = 123,
                }
            }
        });

    public UpdateWrapper GetValidTelegramAudioMessage(long chatId) => 
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = chatId },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakeAudioCaption",
            Audio = new Audio { FileId = "fakeAudioFileId" }
        });

    public UpdateWrapper GetValidTelegramDocumentMessage(long chatId, string fileId) => 
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = chatId },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakeDocumentCaption",
            Document = new Document { FileId = fileId }
        });

    public UpdateWrapper GetValidTelegramLocationMessage(
        Option<float> horizontalAccuracy, long chatId) =>
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = chatId },
            Date = DateTime.Now,
            MessageId = 123,
            Location = new Location
            {
                Latitude = 20.0123,
                Longitude = -17.4509,
                HorizontalAccuracy = horizontalAccuracy.IsSome 
                    ? horizontalAccuracy.GetValueOrThrow() 
                    : null
            }
        });

    public UpdateWrapper GetValidTelegramPhotoMessage(long chatId) => 
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = chatId },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakePhotoCaption",
            Photo = [new PhotoSize{ Height = 1, Width = 1, FileSize = 100L, FileId = "fakePhotoFileId" }]
        });

    public UpdateWrapper GetValidTelegramVoiceMessage(long chatId) =>
        new(new Message
        {
            From = new User { Id = Randomizer.GenerateRandomLong() },
            Chat = new Chat { Id = chatId },
            Date = DateTime.Now,
            MessageId = 123,
            Caption = "fakeVoiceCaption",
            Voice = new Voice { FileId = "fakeVoiceFileId" }
        });
}