// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SPVChannels.API.Rest.Database
{
  public interface IDbManager
  {
    public bool DatabaseExists();
    public bool CreateDb(out string errorMessage, out string errorMessageShort);
  }
}
