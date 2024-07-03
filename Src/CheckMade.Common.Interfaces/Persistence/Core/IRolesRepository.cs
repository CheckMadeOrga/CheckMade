using CheckMade.Common.Model.Core.Actors;
using CheckMade.Common.Model.Core.Interfaces;

namespace CheckMade.Common.Interfaces.Persistence.Core;

public interface IRolesRepository
{
    Task<Role?> GetAsync(IRoleInfo role);
    Task<IReadOnlyCollection<Role>> GetAllAsync();
}