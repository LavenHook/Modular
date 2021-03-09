using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular
{
  public abstract class BasicEncryption
  {
    public abstract string Encrypt(string value);
    public abstract string Decrypt(string value);
  }
}
