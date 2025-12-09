/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace Quickstarts.ConsoleReferenceClient
{
    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
        private const string SimpleVariablesNamespace = "http://opcfoundation.org/SimpleVariables";

        /// <summary>
        /// Reads and displays the three simple variables from the server.
        /// </summary>
        private static async Task ReadSimpleVariablesAsync(ISession session, CancellationToken ct)
        {
            try
            {
                Console.WriteLine("════════════════════════════════════════════════════════════");
                Console.WriteLine("  Reading Simple Variables");
                Console.WriteLine("════════════════════════════════════════════════════════════");
                Console.WriteLine();

                // Find the namespace index for our custom namespace
                int namespaceIndex = session.NamespaceUris.GetIndex("http://opcfoundation.org/SimpleVariables");
                if (namespaceIndex < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ ERROR: SimpleVariables namespace not found on server!");
                    Console.ResetColor();
                    return;
                }

                // Create NodeIds for our three variables
                var stringVar1NodeId = new NodeId("StringVariable1", (ushort)namespaceIndex);
                var stringVar2NodeId = new NodeId("StringVariable2", (ushort)namespaceIndex);
                var intVarNodeId = new NodeId("IntegerVariable", (ushort)namespaceIndex);

                // Read the three variables
                var nodesToRead = new ReadValueIdCollection
                {
                    new ReadValueId { NodeId = stringVar1NodeId, AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = stringVar2NodeId, AttributeId = Attributes.Value },
                    new ReadValueId { NodeId = intVarNodeId, AttributeId = Attributes.Value }
                };

                ReadResponse response = await session.ReadAsync(
                    null,
                    0,
                    TimestampsToReturn.Both,
                    nodesToRead,
                    ct).ConfigureAwait(false);

                ClientBase.ValidateResponse(response.Results, nodesToRead);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToRead);

                // Display the results
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("String Variable 1:");
                Console.ResetColor();
                Console.WriteLine("  Value: {0}", response.Results[0].Value);
                Console.WriteLine("  Status: {0}", response.Results[0].StatusCode);
                Console.WriteLine("  Timestamp: {0}", response.Results[0].SourceTimestamp);
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("String Variable 2:");
                Console.ResetColor();
                Console.WriteLine("  Value: {0}", response.Results[1].Value);
                Console.WriteLine("  Status: {0}", response.Results[1].StatusCode);
                Console.WriteLine("  Timestamp: {0}", response.Results[1].SourceTimestamp);
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Integer Variable:");
                Console.ResetColor();
                Console.WriteLine("  Value: {0}", response.Results[2].Value);
                Console.WriteLine("  Status: {0}", response.Results[2].StatusCode);
                Console.WriteLine("  Timestamp: {0}", response.Results[2].SourceTimestamp);
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Successfully read all three variables!");
                Console.ResetColor();
                Console.WriteLine("════════════════════════════════════════════════════════════");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ ERROR reading variables: {0}", ex.Message);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <exception cref="ErrorExitException"></exception>
        public static async Task Main(string[] args)
        {
            // Check if monitor mode is requested before any output
            bool quietMode = args.Contains("--monitor") || args.Contains("-monitor");

            if (!quietMode)
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("  OPC UA Console Reference Client");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine();

                Console.WriteLine(
                    "OPC UA library: {0} @ {1} -- {2}",
                    Utils.GetAssemblyBuildNumber(),
                    Utils.GetAssemblyTimestamp().ToString("G", CultureInfo.InvariantCulture),
                    Utils.GetAssemblySoftwareVersion()
                );
            }

            // The application name and config file names
            const string applicationName = "ConsoleReferenceClient";
            const string configSectionName = "Quickstarts.ReferenceClient";
            string usage = $"Usage: dotnet {applicationName}.dll [OPTIONS]";

            // command line options
            bool showHelp = false;
            bool monitorMode = false;

            var options = new Mono.Options.OptionSet
            {
                usage,
                { "h|help", "show this message and exit", h => showHelp = h != null },
                {
                    "monitor",
                    "Monitor mode: subscribe to the 3 SimpleVariables and output only JSON updates",
                    m => monitorMode = m != null
                }
            };

            // Load variable mappings from appsettings.opc.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.opc.json", optional: false, reloadOnChange: false)
                .Build();

            var variableMappings = configuration.GetSection("VariableMappings");
            var variables = new (string Id, string NodeName)[]
            {
                ("SummaryNumber", variableMappings["SummaryNumber"]),
                ("OrderNumber", variableMappings["OrderNumber"]),
                ("ProductionCount", variableMappings["ProductionCount"])
            };

            using var telemetry = new ConsoleTelemetry();
            try
            {
                // parse command line and set options
                string extraArg = ConsoleUtils.ProcessCommandLine(
                    args,
                    options,
                    ref showHelp,
                    "REFCLIENT",
                    false
                );

                if (showHelp)
                {
                    options.WriteOptionDescriptions(Console.Out);
                    return;
                }

                // connect Url - first positional arg or prompt
                Uri serverUrl = null;
                if (!string.IsNullOrEmpty(extraArg))
                {
                    serverUrl = new Uri(extraArg);
                }
                else
                {
                    // Use default in monitor mode, prompt otherwise
                    if (monitorMode)
                    {
                        serverUrl = new Uri("opc.tcp://localhost:62541/Quickstarts/ReferenceServer");
                    }
                    else
                    {
                        // Prompt user for server URL
                        Console.WriteLine("Enter OPC UA Server URL (or press Enter for default):");
                        Console.Write("Server URL: ");
                        string userInput = Console.ReadLine()?.Trim();

                        if (!string.IsNullOrEmpty(userInput))
                        {
                            try
                            {
                                serverUrl = new Uri(userInput);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid URL format. Using default.");
                                serverUrl = new Uri("opc.tcp://localhost:62541/Quickstarts/ReferenceServer");
                            }
                        }
                        else
                        {
                            serverUrl = new Uri("opc.tcp://localhost:62541/Quickstarts/ReferenceServer");
                            Console.WriteLine("Using default: {0}", serverUrl);
                        }
                    }
                }

                // Define the UA Client application
                ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
                var application = new ApplicationInstance(telemetry)
                {
                    ApplicationName = applicationName,
                    ApplicationType = ApplicationType.Client,
                    ConfigSectionName = configSectionName
                };

                // Load configuration
                ApplicationConfiguration config = await application
                    .LoadApplicationConfigurationAsync(silent: true)
                    .ConfigureAwait(false);

                // Setup logging (suppress in monitor mode)
                telemetry.ConfigureLogging(config, applicationName, !monitorMode, false, !monitorMode, LogLevel.Information);

                // Check certificate
                bool haveAppCertificate = await application
                    .CheckApplicationInstanceCertificatesAsync(false)
                    .ConfigureAwait(false);

                if (!haveAppCertificate)
                {
                    throw new ErrorExitException("Application instance certificate invalid!", ExitCode.ErrorCertificate);
                }

                // Setup Ctrl-C handler
                var quitCTS = new CancellationTokenSource();
                CancellationToken ct = quitCTS.Token;
                ManualResetEvent quitEvent = ConsoleUtils.CtrlCHandler(quitCTS);

                // Create UA Client
                using var uaClient = new UAClient(config, telemetry, ClientBase.ValidateResponse)
                {
                    AutoAccept = true,
                    SessionLifeTime = 60_000,
                    UserIdentity = new UserIdentity()
                };

                if (!monitorMode)
                {
                    Console.WriteLine();
                    Console.WriteLine("Attempting to connect to: {0}", serverUrl);
                    Console.WriteLine("Please wait...");
                    Console.WriteLine();
                }

                bool connected = await uaClient
                    .ConnectAsync(serverUrl.ToString(), false, ct) // No security for simplicity
                    .ConfigureAwait(false);
                if (!connected)
                {
                    if (!monitorMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("CONNECTION FAILED to: {0}", serverUrl);
                        Console.ResetColor();
                    }
                    return;
                }

                if (!monitorMode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ CONNECTED to: {0}", serverUrl);
                    Console.ResetColor();
                    Console.WriteLine();
                }

                // Get namespace index
                int namespaceIndex = uaClient.Session.NamespaceUris.GetIndex(SimpleVariablesNamespace);
                if (namespaceIndex < 0)
                {
                    if (!monitorMode)
                    {
                        Console.WriteLine($"Error: Namespace '{SimpleVariablesNamespace}' not found");
                    }
                    return;
                }

                // Build NodeIds for the 3 variables
                NodeId[] nodeIds = [.. variables.Select(v => new NodeId(v.NodeName, (ushort)namespaceIndex))];
                if (monitorMode)
                {
                    // Monitor mode: subscribe and output JSON
                    var subscription = new Subscription(uaClient.Session.DefaultSubscription)
                    {
                        PublishingEnabled = true,
                        PublishingInterval = 1000,
                        FastDataChangeCallback = (s, n, st) =>
                        {
                            foreach (MonitoredItemNotification itemNotification in n.MonitoredItems)
                            {
                                MonitoredItem item = s.FindItemByClientHandle(itemNotification.ClientHandle);
                                if (item != null)
                                {
                                    var data = new { Id = item.DisplayName, Value = itemNotification.Value?.Value };
                                    Console.WriteLine(JsonConvert.SerializeObject(data));
                                }
                            }
                        }
                    };
                    uaClient.Session.AddSubscription(subscription);

                    // Add monitored items
                    for (int i = 0; i < variables.Length; i++)
                    {
                        subscription.AddItem(new(telemetry)
                        {
                            StartNodeId = nodeIds[i],
                            AttributeId = Attributes.Value,
                            SamplingInterval = 100,
                            DisplayName = variables[i].Id,
                            QueueSize = 10,
                            MonitoringMode = MonitoringMode.Reporting
                        });
                    }

                    await subscription.CreateAsync(ct).ConfigureAwait(false);
                    await subscription.ApplyChangesAsync(ct).ConfigureAwait(false);

                    // Wait for Ctrl-C
                    bool quit = false;
                    while (!quit)
                    {
                        quit = quitEvent.WaitOne(1000);
                    }
                }
                else
                {
                    // Check mode: just read once
                    var readNodes = new ReadValueIdCollection();
                    foreach (NodeId nodeId in nodeIds)
                    {
                        readNodes.Add(new ReadValueId { NodeId = nodeId, AttributeId = Attributes.Value });
                    }

                    ReadResponse response = await uaClient.Session.ReadAsync(null, 0, TimestampsToReturn.Both, readNodes, ct).ConfigureAwait(false);

                    Console.WriteLine("Variable Values:");
                    Console.WriteLine("─────────────────────────────────────────────────────────");
                    for (int i = 0; i < variables.Length && i < response.Results.Count; i++)
                    {
                        Console.WriteLine($"{variables[i].Id} ({variables[i].NodeName}): {response.Results[i].Value}");
                    }
                    Console.WriteLine("─────────────────────────────────────────────────────────");
                }

                await uaClient.DisconnectAsync(false, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!quietMode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: {0}", ex.Message);
                    Console.ResetColor();
                }
            }
            finally
            {
                if (!quietMode)
                {
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey(true);
                }
            }
        }

        /// <summary>
        /// A dialog which asks for user input.
        /// </summary>
        public class ApplicationMessageDlg : IApplicationMessageDlg
        {
            private readonly TextWriter m_output;
            private string m_message = string.Empty;
            private bool m_ask;

            public ApplicationMessageDlg(TextWriter output = null)
            {
                m_output = output ?? Console.Out;
            }

            public override void Message(string text, bool ask)
            {
                m_message = text;
                m_ask = ask;
            }

            public override async Task<bool> ShowAsync()
            {
                if (m_ask)
                {
                    var message = new StringBuilder(m_message);
                    message.Append(" (y/n, default y): ");
                    m_output.Write(message.ToString());

                    try
                    {
                        ConsoleKeyInfo result = Console.ReadKey();
                        m_output.WriteLine();
                        return await Task.FromResult(result.KeyChar is 'y' or 'Y' or '\r')
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // intentionally fall through
                    }
                }
                else
                {
                    m_output.WriteLine(m_message);
                }

                return await Task.FromResult(true).ConfigureAwait(false);
            }
        }
    }
}
