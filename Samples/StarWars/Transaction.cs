using System;
using System.Collections.Generic;
using System.Text;

namespace StarWars {
  // this is a mock trans object, just to demonstrate how to commit/rollback transactions 
  //  when using mutations
  public class Transaction {
    public void Commit() { }
    public void Abort() { }
  }
}
