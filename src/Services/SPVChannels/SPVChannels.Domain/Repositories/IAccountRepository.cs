﻿// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

namespace SPVChannels.Domain.Repositories
{
  public interface IAccountRepository
  {
    long CreateAccount(string accountname, string scheme, string credential);
  }
}
