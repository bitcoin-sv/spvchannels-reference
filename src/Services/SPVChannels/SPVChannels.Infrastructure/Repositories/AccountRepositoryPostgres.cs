// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System.Linq;

namespace SPVChannels.Infrastructure.Repositories
{
  public class AccountRepositoryPostgres : BaseRepositoryPostgres, IAccountRepository
  {
    public AccountRepositoryPostgres(IOptions<AppConfiguration> appConfiguration) : base(appConfiguration) { }

    public static void EmptyRepository(string connectionString)
    {
      using var connection = new NpgsqlConnection(connectionString);
      connection.Open();
      string cmdText =
        "DELETE FROM AccountCredential; DELETE FROM Account; ALTER SEQUENCE Account_id_seq RESTART WITH 1; ALTER SEQUENCE AccountCredential_id_seq RESTART WITH 1";
      connection.Execute(cmdText, null);
    }

    public long CreateAccount(string accountname, string scheme, string credential)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();
      var account = GetAccountAndAccountCredential(accountname, scheme, credential, connection);
      long accountId = 0;

      if (account != null)
      {
        if (!account.AccountCredentialExists)
        {
          InsertAccountCredential(account.Id, scheme, credential, connection);
        }
        accountId = account.Id;
      }
      else
      {
        accountId = InsertAccount(accountname, scheme, credential, connection);
      }

      transaction.Commit();

      return accountId;
    }

    private long InsertAccount(string accountname, string scheme, string credential, NpgsqlConnection connection)
    {
      string insertAccount =
"INSERT INTO Account (name) " +
"VALUES(@name) " +
"RETURNING *;";

      var account = connection.Query<Domain.Models.Account>(insertAccount, new { name = accountname }).Single();

      InsertAccountCredential(account.Id, scheme, credential, connection);

      return account.Id;
    }

    private void InsertAccountCredential(long accountId, string scheme, string credential, NpgsqlConnection connection)
    {
      string insertAccountCredential =
"INSERT INTO AccountCredential (account, scheme, credential) " +
"VALUES(@accountid, @scheme, @credential);";

      connection.Execute(insertAccountCredential, new { accountid = accountId, scheme, credential });
    }

    private Account GetAccountAndAccountCredential(string accountname, string scheme, string credential, NpgsqlConnection connection)
    {
      string insertAccount =
"SELECT * " +
"FROM Account " +
"WHERE name = @name;";

      var account = connection.Query<Account>(insertAccount, new { name = accountname }).SingleOrDefault();
      if (account == null)
        return null;

      string insertAccountCredential =
"SELECT COUNT('x') " +
"FROM AccountCredential " +
"WHERE account = @accountid AND AccountCredential.scheme=@scheme AND AccountCredential.credential = @credential;";

      var accountCredential = connection.ExecuteScalar<long>(insertAccountCredential, new { accountid = account.Id, scheme, credential });
      account.AccountCredentialExists = accountCredential > 0;

      return account;
    }

  }
}
