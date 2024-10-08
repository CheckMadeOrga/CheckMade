using CheckMade.Common.Model.Core.Actors.RoleSystem;

namespace CheckMade.Common.Model.Utils.Comparers;

internal static class RoleInfoComparer
{
    public static bool AreEqual(IRoleInfo first, IRoleInfo second)
    {
        return first.Token.Equals(second.Token) && 
               first.RoleType.GetType() == second.RoleType.GetType() &&
               first.Status.Equals(second.Status);
    }
}