
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

namespace Company.Function
{
    public static class HttpTriggerCSharp1
    {
        [FunctionName("HttpTriggerCSharp1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            var allParams = req.GetQueryParameterDictionary();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;


            /*using (PowerShell ps = PowerShell.Create())
            {
                // specify the script code to run.
                ps.AddScript("Set-Item WSMan:localhost\\client\\trustedhosts -value *");

                // specify the parameters to pass into the script.
                //ps.AddParameters();

                // execute the script and await the result.
                var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(false);

                // print the resulting pipeline objects to the console.
                foreach (var item in pipelineObjects)
                {
                    Console.WriteLine(item.BaseObject.ToString());
                }
            }*/

            WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
            connectionInfo.Credential = new PSCredential(Environment.GetEnvironmentVariable("User"), ConvertToSecureString(Environment.GetEnvironmentVariable("Password")));
            connectionInfo.ComputerName = Environment.GetEnvironmentVariable("ScriptHost");
            Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo);
            runspace.Open();
            string outputString = "";
            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = runspace;
                
                string scriptPath = Environment.GetEnvironmentVariable("ScriptPath");
                // specify the script code to run.
                ps.AddCommand(scriptPath);

                var scriptParams = new Dictionary<string, string>();
                foreach (var item in allParams)
                {
                    scriptParams.Add(item.Key, item.Value);

                }

                // specify the parameters to pass into the script.
                ps.AddParameters(scriptParams);

                // execute the script and await the result.                
                var output = ps.Invoke();

                // print the resulting pipeline objects to the console.
                
                
                foreach (var item in output)
                {
                    Console.WriteLine(item.ToString());
                    outputString += item.ToString() + "\n";
                }
            }
            runspace.Close();

            string responseMessage = string.IsNullOrEmpty(outputString)
                ? "NO OUTPUT"
                : $"Connect to {Environment.GetEnvironmentVariable("ScriptHost")} as {Environment.GetEnvironmentVariable("User")} with OUTPUT: \n{outputString}";

            return new OkObjectResult(responseMessage);

            SecureString ConvertToSecureString(string password)
            {
                if (password == null)
                    throw new ArgumentNullException("password");

                var securePassword = new SecureString();

                foreach (char c in password)
                    securePassword.AppendChar(c);

                securePassword.MakeReadOnly();
                return securePassword;
            }
        }
    }
}
