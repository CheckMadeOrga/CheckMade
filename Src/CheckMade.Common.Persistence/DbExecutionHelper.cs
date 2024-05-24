using CheckMade.Common.Interfaces;
using CheckMade.Common.LangExt;
using CheckMade.Common.Utils.RetryPolicies;
using Newtonsoft.Json;
using Npgsql;

namespace CheckMade.Common.Persistence;

public interface IDbExecutionHelper
{
    Task ExecuteOrThrowAsync(Func<NpgsqlConnection, NpgsqlTransaction, Task> executeDbOperation);
}

internal class DbExecutionHelper(
        IDbConnectionProvider dbProvider,
        IDbOpenRetryPolicy dbOpenRetryPolicy,
        IDbCommandRetryPolicy dbCommandRetryPolicy) 
    : IDbExecutionHelper
{
    public async Task ExecuteOrThrowAsync(Func<NpgsqlConnection, NpgsqlTransaction, Task> executeDbOperations)
    {
        await using var db = dbProvider.CreateConnection() as NpgsqlConnection;

        if (db == null)
            throw new DataAccessException(Ui("Failed to assign IDbConnection"));
        
        await dbOpenRetryPolicy.ExecuteAsync(async () => await db.OpenAsync());

        try
        {
            await using var transaction = await db.BeginTransactionAsync();
            
            try
            {
                await dbCommandRetryPolicy.ExecuteAsync(async () => await executeDbOperations(db, transaction));
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (JsonSerializationException jsonEx)
        {
            throw new DataAccessException(Ui("JSON (de)serialization exception has occurred during " +
                                          "db command execution."), jsonEx);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(Ui("A database exception has occurred."), ex);
        }
    }
}