using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace avallama.Services;

public interface IDatabaseInitService
{
    Task<SqliteConnection> GetOpenConnectionAsync();
}