using CheckMade.Common.Utils;

namespace CheckMade.DevOps.DataMigration;

internal interface IDataMigrator
{
    Task<Result<int>> MigrateAsync(string env);
}
