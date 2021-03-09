using System;

namespace Modular
{
  [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
  public class ModuleAttribute : Attribute
  {
    public string Name { get; }
    public string Description { get; set; }

    public int Priority { get; }

    public string EndpointPrefix { get; set; }

    public ModuleAttribute(string Name)
    {
      this.Name = Name;
    }
  }
}
