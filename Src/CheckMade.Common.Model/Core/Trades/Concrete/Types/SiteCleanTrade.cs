namespace CheckMade.Common.Model.Core.Trades.Concrete.Types;

public class SiteCleanTrade : ITrade
{
    public bool DividesLiveEventIntoSpheresOfAction => true;
    public const int SphereNearnessThresholdInMeters = 100;
}