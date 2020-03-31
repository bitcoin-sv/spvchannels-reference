namespace SPVChannels.Domain.Repositories
{
  public interface IAccountRepository
  {
    long CreateAccount(string accountname, string scheme, string credential);
  }
}
