using CheckMade.Common.Model.Core;
using CheckMade.Common.Model.Core.Trades.SubDomain.SanitaryOps.Issues;

namespace CheckMade.Tests.Unit.Common;

public class DomainTermTests
{
    [Fact]
    public void Equals_ReturnsFalse_ForTwoEnumsOfSameValueButDifferentType()
    {
        var enum1 = Dt(LanguageCode.en);
        var enum2 = Dt(ConsumablesIssue.Item.ToiletPaper);

        Assert.Equal((int)LanguageCode.en, (int)ConsumablesIssue.Item.ToiletPaper);
        Assert.False(enum1.Equals(enum2));
    }
}