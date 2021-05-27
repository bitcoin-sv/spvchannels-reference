// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Utilities
{
  public class HelperTools
  {    
    public async static Task ExecuteWithRetries(int noOfRetries, string errorMessage, Func<Task> methodToExecute, int sleepTimeBetweenRetries = 1000)
    {
      try
      {
        do
        {
          noOfRetries--;
          try
          {
            await methodToExecute();
            return;
          }
          catch (Exception)
          {
            Thread.Sleep(sleepTimeBetweenRetries);
            if (noOfRetries == 0)
            {
              throw;
            }
          }
        }
        while (noOfRetries > 0);
      }
      catch (Exception ex)
      {
        if (!string.IsNullOrEmpty(errorMessage))
          throw new Exception(errorMessage, ex);
        throw;
      }
    }
    public static string SerializeDateTimeToJSON(DateTime value)
    {
      return value.ToString("yyyy-MM-ddThh:mm:ss.fffffffZ");
    }

  }
}
