namespace CheckMade.Common.Interfaces.ExternalServices.AzureServices;

public interface IBlobLoader
{
    public Task<Uri> UploadBlobAndReturnUriAsync(MemoryStream stream, string fileName);
    public Task<(MemoryStream, string)> DownloadBlobAsync(Uri blobUri);
}