namespace CheckMade.Common.Model.Core.Trades.Concrete.TradeModels.SiteClean;

public sealed record SiteCleanTrade : ITrade
{
    public const int SphereNearnessThresholdInMeters = 100;
    
    public UiString GetSphereOfActionLabel => Ui("zone");
}