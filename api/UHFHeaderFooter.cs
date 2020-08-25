using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Text;
namespace Company.Function
{
  public static class UHFHeaderFooter
  {
    /*
    [FunctionName("UHFHeaderFooter")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      string name = req.Query["name"];

      string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      dynamic data = JsonConvert.DeserializeObject(requestBody);
      name = name ?? data?.name;

      string responseMessage = string.IsNullOrEmpty(name)
          ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
          : $"Hello, {name}. This HTTP triggered function executed successfully.";

      return new OkObjectResult(responseMessage);
    }*/

    [FunctionName("HeaderFooterTemplate")]
    public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "getUHFInfo")] HttpRequest req, ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      var shellObj = await new ShellServiceProvider().LoadAsync("en-US");
      var response = JsonConvert.SerializeObject(shellObj);

      return shellObj != null
          ? (ActionResult)new OkObjectResult(response)
          : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
    }
  }

  #region Helper Classes

  public class ShellModel
  {
    public String CssIncludes { get; set; }
    public String JavaScriptIncludes { get; set; }
    public String HeaderHtml { get; set; }
    public String FooterHtml { get; set; }
  }

  public class ShellServiceProvider
  {
    static string serviceEndpoint = "http://uhf.microsoft.com";
    static string partnerId = "MSOpenjdk";
    static string headerId = "MSOpenjdkHeader";
    static string footerId = "MSOpenjdkFooter";
    static string userAgent = "Microsoft";

    public async Task<ShellModel> LoadAsync(string locale)
    {
      if (string.IsNullOrWhiteSpace(locale))
      {
        throw new ArgumentNullException(nameof(locale));
      }

      using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(60) })
      {
        var url = serviceEndpoint +
            "/" + locale + "/shell/xml/" + partnerId +
            "?headerId=" + headerId +
            "&footerId=" + footerId;

        client.DefaultRequestHeaders.Add("user-agent", userAgent);

        var stringResponse = await client.GetStringAsync(url).ConfigureAwait(false);

        var feed = ConvertXmlToModel(stringResponse);

        return feed;
      }
    }

    public static ShellModel Load(string locale)
    {
      string xml = string.Empty;
      using (var client = new WebClient())
      {
        var serviceUrl = serviceEndpoint +
            "/" + locale + "/shell/xml/" + partnerId +
            "?headerId=" + headerId +
            "&footerId=" + footerId;
        client.Headers.Add("user-agent", userAgent);
        client.Encoding = Encoding.UTF8;
        xml = client.DownloadString(serviceUrl);
      }
      var response = ConvertXmlToModel(xml);
      return response;
    }

    private static ShellModel ConvertXmlToModel(string xml)
    {
      var document = XDocument.Parse(xml);
      var root = document.Element("shell");
      var response = new ShellModel
      {
        CssIncludes = EnsureStringValue(root, "cssIncludes"),
        JavaScriptIncludes = EnsureStringValue(root, "javascriptIncludes"),
        FooterHtml = EnsureStringValue(root, "footerHtml"),
        HeaderHtml = EnsureStringValue(root, "headerHtml"),
      };
      return response;
    }

    private static String EnsureStringValue(XElement root, string elementName)
    {
      var element = root.Element(elementName);
      return element != null ? (string)element : string.Empty;
    }
  }
  #endregion
}
