namespace CheckMade.Common.Model.Core.Trades.Concrete.TradeModels.SaniClean;

public sealed record SaniCleanTrade : ITrade
{
    public const int SphereNearnessThresholdInMeters = 30;
    
    public UiString GetSphereOfActionLabel => Ui("sanitary camp");
}