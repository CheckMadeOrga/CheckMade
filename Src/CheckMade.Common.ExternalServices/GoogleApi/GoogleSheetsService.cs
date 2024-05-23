using CheckMade.Common.Interfaces.ExternalServices.GoogleApi;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace CheckMade.Common.ExternalServices.GoogleApi;

public class GoogleSheetsService(GoogleAuth googleAuth) : ISheetsService
{
    private const string SheetRangeForAllData = "A:ZZ";
    
    private readonly SheetsService _sheetsService = CreateSheetsService(googleAuth.CreateCredential());

    private static SheetsService CreateSheetsService(IConfigurableHttpClientInitializer credential) => 
        new(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CheckMadeGoogleSheetsAccess",
        });

    public async Task<SheetData> GetSpreadsheetDataAsync(string sheetId, string sheetRange, string? sheetName = null)
    {
        var fullSheetRange = sheetName != null 
            ? string.Concat(sheetName, "!", sheetRange) 
            : sheetRange;
        
        var request = _sheetsService.Spreadsheets.Values.Get(sheetId, fullSheetRange);
        var response = (await request.ExecuteAsync()).Values;
        
        return new SheetData(
            Cells: response
                .Select(row => row.Select(cell => cell.ToString() 
                                                  ?? string.Empty).ToArray()).ToArray());
    }

    public async Task<SheetData> GetAllSpreadsheetDataAsync(string sheetId, string? sheetName = null) =>
        await GetSpreadsheetDataAsync(sheetId, SheetRangeForAllData, sheetName);
}