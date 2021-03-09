//@SEE: https://andrewlock.net/sharing-appsettings-json-configuration-files-between-projects-in-asp-net-core/
//@SEE: https://jumpforjoysoftware.com/2018/09/aspnet-core-shared-settings/
//although this file does not resemble anything in the above links now, there was plenty of inspiration taken
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
  public static class WebHostBuilderSharedSettingsExtentions
  {
    private static Action<WebHostBuilderContext, IConfigurationBuilder> confureWebHostSharedResources(params string[] Paths)
    {
      return (hostingContext, config) =>
      {
        var env = hostingContext.HostingEnvironment;
        List<IFileProvider> fileProviders = new List<IFileProvider>();
        var fp = config.GetFileProvider();
        if (fp is IFileProvider)
        {
          fileProviders.Add(fp);
        }

        foreach (var path in Paths)
        {
          var SharedSettingsPath = path;
          var SharedSettings_pub = System.IO.Path.Combine(env.ContentRootPath, SharedSettingsPath); //this is the location next to wwwroot when the project run from a published location
          var SharedSettings_run = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), SharedSettingsPath); //this is the location when the project is run from cli or VS
          if (Directory.Exists(SharedSettings_pub))
          {
            fileProviders.Add(new PhysicalFileProvider(SharedSettings_pub)); //prefer this location
          }
          if (Directory.Exists(SharedSettings_run))
          {
            fileProviders.Add(new PhysicalFileProvider(SharedSettings_run));
          }
        }
        config.SetFileProvider(new CompositeFileProvider(fileProviders));
      };
    }

    public static IWebHostBuilder ConfigureSharedResources(this IWebHostBuilder builder, params string[] Paths)
    {
      return builder.ConfigureAppConfiguration(confureWebHostSharedResources(Paths));
    }
  }
}
