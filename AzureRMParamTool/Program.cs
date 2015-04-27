using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureRMParamTool
{
    class Program
    {
        const string MessageFormat = "AzureRMParamTool: {0}";

        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { "/Param:adminUserName", "/Val:test_2" };
#endif
            // Found from http://stackoverflow.com/a/5955585 user- https://stackoverflow.com/users/261653/daniel
            Regex cmdRegEx = new Regex(@"/(?<name>.+?):(?<val>.+)");
            Regex cmdSwitchRegEx = new Regex(@"/(?<name>.+)");

            Dictionary<string, string> cmdArgs = new Dictionary<string, string>();
            foreach (string s in args)
            {
                Match m = cmdRegEx.Match(s);
                Match mSwitch = cmdSwitchRegEx.Match(s);
                if (m.Success)
                {
                    cmdArgs.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
                else if (mSwitch.Success)
                {
                    cmdArgs.Add(mSwitch.Groups[1].Value, "");
                }
            }

            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "override.param.json")))
            {
                cmdArgs["File"] = Path.Combine(Environment.CurrentDirectory, "override.param.json");
            }

            var fileContent = File.ReadAllText(cmdArgs["File"]);

            Dictionary<string, object> paramObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);

            // Validate the given file is a Azure RM parameters file
            if (!paramObject.ContainsKey("$schema"))
            {
                WriteToConsole("Missing $schema property. File is an invalid AzureRM paramters file");
                return;
            }

            var schema = (string)paramObject["$schema"];
            if (!schema.Contains("deploymentParameters"))
            {
                WriteToConsole("Schema doesn't contain 'deploymentParameters'. This file must not be an Azure RM deployment parameters file");
            }

            JObject parameters = (JObject)paramObject["parameters"];

            // Evaluate if the user just want's to list properties and return
            if (cmdArgs.ContainsKey("List"))
            {
                WriteResultToConsole(parameters.Children().Select(t => t.ToString()).ToArray());
                return;
            }

            // Validate that the user has the Param and Val keys
            if (!cmdArgs.ContainsKey("Param") && 
                !cmdArgs.ContainsKey("Val"))
            {
                WriteToConsole("/Param:<parameter name> and /Val:<value> required");
                return;
            }
 
            // Set the parameter           
            parameters[cmdArgs["Param"]]["value"] = cmdArgs["Val"];
            WriteResultToConsole(parameters.Children().Select(t => t.ToString()).ToArray());
            paramObject["parameters"] = parameters;
            
            // Write to the file
            var changedContent = JsonConvert.SerializeObject(paramObject);
            File.WriteAllText(cmdArgs["File"], changedContent);
        }

        static void WriteToConsole(string message)
        {
            Console.Out.WriteLine(string.Format(MessageFormat, message));
        }

        static void WriteResultToConsole(string[] messages)
        {
            foreach (var message in messages)
            {
                Console.Out.WriteLine(message);
            }
        }
    }
}
