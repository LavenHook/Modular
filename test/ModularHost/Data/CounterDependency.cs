using System;
using System.Collections.Generic;
using System.Text;

namespace ModularHost.Data
{
  public class CounterDependency
  {
    public string message { get; private set; } = "This is a modularly injected dependency.";
  }
}
