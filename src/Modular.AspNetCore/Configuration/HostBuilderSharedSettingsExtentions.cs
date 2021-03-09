//@SEE: https://andrewlock.net/sharing-appsettings-json-configuration-files-between-projects-in-asp-net-core/
//@SEE: https://jumpforjoysoftware.com/2018/09/aspnet-core-shared-settings/
//although this file does not resemble anything in the above links now, there was plenty of inspiration taken
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting
{
  public static class HostBuilderSharedSettingsExtentions
  {
    private static Action<HostBuilderContext, IConfigurationBuilder> confureHostSharedResources(string rootPath)
    {
      return (hostingContext, config) =>
      {
        var env = hostingContext.HostingEnvironment;
        ////WTF: the hostingContext keeps throwing an exception that it can't find the get_HostingEnvironment method
        //Microsoft.Extensions.Hosting.IHostingEnvironment env = null;
        //var hcType = hostingContext.GetType();
        //var pi = hcType.GetProperty(nameof(hostingContext.HostingEnvironment));
        //var tmp = pi.GetMethod.Invoke(hostingContext, null);
        //if (tmp is Microsoft.Extensions.Hosting.IHostingEnvironment)
        //{
        //  env = tmp as Microsoft.Extensions.Hosting.IHostingEnvironment;
        //}

        var SharedSettingsPath = rootPath;
        var SharedSettings_pub = System.IO.Path.Combine(env.ContentRootPath, SharedSettingsPath); //this is the location next to wwwroot when the project run from a published location
        var SharedSettings_run = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), SharedSettingsPath); //this is the location when the project is run from cli or VS
        List<IFileProvider> fileProviders = new List<IFileProvider>();
        var fp = config.GetFileProvider();
        if (Directory.Exists(SharedSettings_pub))
        {
          fileProviders.Add(new PhysicalFileProvider(SharedSettings_pub)); //prefer this location
        }
        if (Directory.Exists(SharedSettings_run))
        {
          fileProviders.Add(new PhysicalFileProvider(SharedSettings_run));
        }
        if (fp is IFileProvider)
        {
          fileProviders.Add(fp);
        }
        config.SetFileProvider(new CompositeFileProvider(fileProviders));
      };
    }

    public static IHostBuilder ConfigureSharedResources(this IHostBuilder builder, string rootPath)
    {
      return builder.ConfigureAppConfiguration(confureHostSharedResources(rootPath));
    }
  }
}
