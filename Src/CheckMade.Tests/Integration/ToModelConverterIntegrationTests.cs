using System.Text;
using CheckMade.Common.Interfaces.ExternalServices.AzureServices;
using CheckMade.Common.Model.Telegram.Updates;
using CheckMade.Telegram.Function.Services.BotClient;
using CheckMade.Telegram.Function.Services.Conversions;
using CheckMade.Tests.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace CheckMade.Tests.Integration;

public class ToModelConverterIntegrationTests
{
    private IServiceProvider? _services;

    [Fact]
    public async Task ConvertToModelAsync_HasCorrectInternalAttachmentUri_ForUpdateWithAttachmentInOperationsBot()
    {
        _services = new IntegrationTestStartup().Services.BuildServiceProvider();
        var utils = _services.GetRequiredService<ITestUtils>();
        const string realFileIdOnDevOperationsBot = // uploaded on 04/06/2024
            "BQACAgQAAxkBAAMvZl9iHPHKeRre-ldIyMhLcEvi6a8AAi0gAALxa_lS9Z28FPz-17Q1BA";
        const string realFileUtf8Content = "Text for an integration test.";
        var updateWithAttachment = utils.GetValidTelegramDocumentMessage(
            fileId: realFileIdOnDevOperationsBot);
        
        var blobLoader = _services.GetRequiredService<IBlobLoader>();
        var botClientFactory = _services.GetRequiredService<IBotClientFactory>();
        var converterFactory = _services.GetRequiredService<IToModelConverterFactory>();
        var converter = converterFactory.Create(
            new TelegramFilePathResolver(
                botClientFactory.CreateBotClientOrThrow(BotType.Operations)));
        
        var actualModel = await converter.ConvertToModelAsync(
            updateWithAttachment, BotType.Operations);

        var (downloadedStream, _) = await blobLoader.DownloadBlobOrThrowAsync(
            actualModel.Value!.Details.AttachmentInternalUri.Value!);

        var downloadedContent = Encoding.UTF8.GetString(downloadedStream.ToArray());
        
        Assert.Equal(realFileUtf8Content, downloadedContent);
    }
}