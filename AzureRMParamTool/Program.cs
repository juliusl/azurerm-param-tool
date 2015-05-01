using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static System.DateTime;
using static System.Environment;
using static System.IO.File;
using static System.IO.Path;

namespace AzureRMParamTool
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { "/Param:adminUserName", "/Val:test_2" };
#endif
            try
            {
                // Grab the command line arguments
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

                // Load the override file if the override file exists
                if (Exists(Combine(CurrentDirectory, "override.param.json")))
                {
                    WriteToConsole("override.param.json file found, switching files.");
                    cmdArgs["File"] = Combine(CurrentDirectory, "override.param.json");
                }

                // Deserialize the params file
                var fileContent = ReadAllText(cmdArgs["File"]);
                var paramObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);

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
                    return;
                }

                // Grab the parameters block
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
                if (cmdArgs.ContainsKey("Verbose")) {
                    WriteToConsole("Parameter set:");
                    WriteResultToConsole(parameters.Children().Select(t => t.ToString()).ToArray());
                }

                // Apply the changes
                paramObject["parameters"] = parameters;

                // Write to the file
                var changedContent = JsonConvert.SerializeObject(paramObject, Formatting.Indented);
                WriteAllText(cmdArgs["File"], changedContent);
            }
            catch (Exception e)
            {
                WriteToConsole($"There was an issue - Exception Details: {e}");
            }            
        }

        static void WriteToConsole(string message)
        {
            Console.Out.WriteLine($"[{Now}] AzureRMParamTool: { message }");
        }

        static void WriteResultToConsole(string[] messages)
        {
            foreach (var message in messages)
            {
                WriteToConsole(message);
            }
        }
    }
}
