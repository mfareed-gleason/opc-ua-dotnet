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
using Microsoft.Extensions.Logging;
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

            // The application name and config file names
            const string applicationName = "ConsoleReferenceClient";
            const string configSectionName = "Quickstarts.ReferenceClient";
            string usage = $"Usage: dotnet {applicationName}.dll [OPTIONS]";

            // command line options
            bool showHelp = false;
            bool autoAccept = true; // Default to true for easy setup
            string username = null;
            byte[] userpassword = null;
            string userCertificateThumbprint = null;
            byte[] userCertificatePassword = null;
            bool logConsole = false;
            bool appLog = false;
            bool fileLog = false;
            bool renewCertificate = false;
            bool loadTypes = false;
            bool managedbrowseall = false;
            bool browseall = false;
            bool fetchall = false;
            bool jsonvalues = false;
            bool verbose = false;
            bool subscribe = true; // Default to subscribe mode
            bool noSecurity = true; // Default to no security for easy setup
            byte[] pfxPassword = null;
            int timeout = Timeout.Infinite;
            string logFile = null;
            string reverseConnectUrlString = null;
            bool leakChannels = false;
            bool forever = false;
            bool enableDurableSubscriptions = false;

            var options = new Mono.Options.OptionSet
            {
                usage,
                { "h|help", "show this message and exit", h => showHelp = h != null },
                {
                    "a|autoaccept",
                    "auto accept certificates (for testing only)",
                    a => autoAccept = a != null },
                {
                    "nsec|nosecurity",
                    "select endpoint with security NONE, least secure if unavailable",
                    s => noSecurity = s != null
                },
                {
                    "un|username=",
                    "the name of the user identity for the connection",
                    u => username = u },
                {
                    "up|userpassword=",
                    "the password of the user identity for the connection",
                    u => userpassword = Encoding.UTF8.GetBytes(u) },
                {
                    "uc|usercertificate=",
                    "the thumbprint of the user certificate for the user identity",
                    u => userCertificateThumbprint = u
                },
                {
                    "ucp|usercertificatepassword=",
                    "the password of the user certificate for the user identity",
                    u => userCertificatePassword = Encoding.UTF8.GetBytes(u)
                },
                { "c|console", "log to console", c => logConsole = c != null },
                { "l|log", "log app output", c => appLog = c != null },
                { "f|file", "log to file", f => fileLog = f != null },
                {
                    "p|password=",
                    "optional password for private key",
                    p => pfxPassword = Encoding.UTF8.GetBytes(p)
                },
                { "r|renew", "renew application certificate", r => renewCertificate = r != null },
                {
                    "t|timeout=",
                    "timeout in seconds to exit application",
                    (int t) => timeout = t * 1000 },
                {
                    "logfile=",
                    "custom file name for log output",
                    l =>
                    {
                        if (l != null)
                        {
                            logFile = l;
                        }
                    }
                },
                {
                    "lt|loadtypes",
                    "Load custom types",
                    lt =>
                    {
                        if (lt != null)
                        {
                            loadTypes = true;
                        }
                    }
                },
                {
                    "m|managedbrowseall",
                    "Browse all references using the MangedBrowseAsync method",
                    m =>
                    {
                        if (m != null)
                        {
                            managedbrowseall = true;
                        }
                    }
                },
                {
                    "b|browseall",
                    "Browse all references",
                    b =>
                    {
                        if (b != null)
                        {
                            browseall = true;
                        }
                    }
                },
                {
                    "fa|fetchall",
                    "Fetch all nodes",
                    f =>
                    {
                        if (f != null)
                        {
                            fetchall = true;
                        }
                    }
                },
                {
                    "j|json",
                    "Output all Values as JSON",
                    j =>
                    {
                        if (j != null)
                        {
                            jsonvalues = true;
                        }
                    }
                },
                {
                    "v|verbose",
                    "Verbose output",
                    v =>
                    {
                        if (v != null)
                        {
                            verbose = true;
                        }
                    }
                },
                {
                    "s|subscribe",
                    "Subscribe",
                    s =>
                    {
                        if (s != null)
                        {
                            subscribe = true;
                        }
                    }
                },
                {
                    "rc|reverseconnect=",
                    "Connect using the reverse connect endpoint. (e.g. rc=opc.tcp://localhost:65300)",
                    url => reverseConnectUrlString = url
                },
                {
                    "forever",
                    "run inner connect/disconnect loop forever",
                    f =>
                    {
                        if (f != null)
                        {
                            forever = true;
                        }
                    }
                },
                {
                    "leakchannels",
                    "Leave a channel leak open when disconnecting a session.",
                    l =>
                    {
                        if (l != null)
                        {
                            leakChannels = true;
                        }
                    }
                },
                {
                    "ds|durablesubscription",
                    "SetDurableSubscription example",
                    ds =>
                    {
                        if (ds != null)
                        {
                            enableDurableSubscriptions = true;
                        }
                    }
                }
            };

            ReverseConnectManager reverseConnectManager = null;
            using var telemetry = new ConsoleTelemetry();
            ILogger logger = LoggerUtils.Null.Logger;
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

                // connect Url?
                Uri serverUrl = null;
                if (!string.IsNullOrEmpty(extraArg))
                {
                    serverUrl = new Uri(extraArg);
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

                // log console output to logger
                if (logConsole && appLog)
                {
                    logger = telemetry.CreateLogger("Main");
                }

                // Define the UA Client application
                ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
                var passwordProvider = new CertificatePasswordProvider(pfxPassword);
                var application = new ApplicationInstance(telemetry)
                {
                    ApplicationName = applicationName,
                    ApplicationType = ApplicationType.Client,
                    ConfigSectionName = configSectionName,
                    CertificatePasswordProvider = passwordProvider
                };

                // load the application configuration.
                ApplicationConfiguration config = await application
                    .LoadApplicationConfigurationAsync(silent: false)
                    .ConfigureAwait(false);

                // override logfile
                if (logFile != null)
                {
                    string logFilePath = config.TraceConfiguration.OutputFilePath;
                    string filename = Path.GetFileNameWithoutExtension(logFilePath);
                    config.TraceConfiguration.OutputFilePath = logFilePath.Replace(
                        filename,
                        logFile,
                        StringComparison.Ordinal
                    );
                    config.TraceConfiguration.DeleteOnLoad = true;
#pragma warning disable CS0618 // Type or member is obsolete
                    config.TraceConfiguration.ApplySettings();
#pragma warning restore CS0618 // Type or member is obsolete
                }

                // setup the logging
                telemetry.ConfigureLogging(
                    config,
                    applicationName,
                    logConsole,
                    fileLog,
                    appLog,
                    LogLevel.Information);

                // delete old certificate
                if (renewCertificate)
                {
                    await application.DeleteApplicationInstanceCertificateAsync()
                        .ConfigureAwait(false);
                }

                // check the application certificate.
                bool haveAppCertificate = await application
                    .CheckApplicationInstanceCertificatesAsync(false)
                    .ConfigureAwait(false);
                if (!haveAppCertificate)
                {
                    throw new ErrorExitException(
                        "Application instance certificate invalid!",
                        ExitCode.ErrorCertificate
                    );
                }

                if (reverseConnectUrlString != null)
                {
                    // start the reverse connection manager
                    logger.LogInformation(
                        "Create reverse connection endpoint at {Url}.",
                        reverseConnectUrlString);
                    reverseConnectManager = new ReverseConnectManager(telemetry);
                    reverseConnectManager.AddEndpoint(new Uri(reverseConnectUrlString));
                    reverseConnectManager.StartService(config);
                }

                // wait for timeout or Ctrl-C
                var quitCTS = new CancellationTokenSource();
                CancellationToken ct = quitCTS.Token;
                ManualResetEvent quitEvent = ConsoleUtils.CtrlCHandler(quitCTS);

                var userIdentity = new UserIdentity();

                // set user identity of type username/pw
                if (!string.IsNullOrEmpty(username))
                {
                    if (userpassword == null)
                    {
                        logger.LogInformation(
                            "No password provided for user {Username}, using empty password.",
                            username);
                    }

                    userIdentity = new UserIdentity(username, userpassword ?? ""u8);
                    logger.LogInformation(
                        "Connect with user identity for user {Username}",
                        username);
                }

                // set user identity of type certificate
                if (!string.IsNullOrEmpty(userCertificateThumbprint))
                {
                    CertificateIdentifier userCertificateIdentifier
                            = await FindUserCertificateIdentifierAsync(
                                userCertificateThumbprint,
                                application.ApplicationConfiguration.SecurityConfiguration
                                    .TrustedUserCertificates,
                                telemetry,
                                ct
                            )
                            .ConfigureAwait(true);

                    if (userCertificateIdentifier != null)
                    {
                        userIdentity = UserIdentity.CreateAsync(
                            userCertificateIdentifier,
                            new CertificatePasswordProvider(userCertificatePassword),
                            telemetry,
                            ct
                        ).GetAwaiter().GetResult();

                        logger.LogInformation(
                            "Connect with user certificate with Thumbprint {UserCertificateThumbprint}",
                            userCertificateThumbprint);
                    }
                    else
                    {
                        logger.LogInformation(
                            "Failed to load user certificate with Thumbprint {UserCertificateThumbprint}",
                            userCertificateThumbprint);
                    }
                }

                // connect to a server until application stops
                bool quit = false;
                DateTime start = DateTime.UtcNow;
                int waitTime = int.MaxValue;
                do
                {
                    if (timeout > 0)
                    {
                        waitTime = timeout - (int)DateTime.UtcNow.Subtract(start).TotalMilliseconds;
                        if (waitTime <= 0)
                        {
                            if (!forever)
                            {
                                break;
                            }

                            waitTime = 0;
                        }

                        if (forever)
                        {
                            start = DateTime.UtcNow;
                        }
                    }

                    // create the UA Client object and connect to configured server.
                    using var uaClient = new UAClient(
                        application.ApplicationConfiguration,
                        reverseConnectManager,
                        telemetry,
                        ClientBase.ValidateResponse
                    )
                    {
                        AutoAccept = autoAccept,
                        SessionLifeTime = 60_000,
                        UserIdentity = userIdentity
                    };

                    if (enableDurableSubscriptions)
                    {
                        uaClient.ReconnectPeriodExponentialBackoff = 60000;
                    }

                    Console.WriteLine();
                    Console.WriteLine("Attempting to connect to: {0}", serverUrl);
                    Console.WriteLine("Please wait...");
                    Console.WriteLine();

                    bool connected = await uaClient
                        .ConnectAsync(serverUrl.ToString(), !noSecurity, ct)
                        .ConfigureAwait(false);
                    if (connected)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine("  ✓ CONNECTION SUCCESSFUL!");
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine("Connected to: {0}", serverUrl);
                        Console.WriteLine("Session ID: {0}", uaClient.Session.SessionId);
                        Console.WriteLine();

                        logger.LogInformation("Connected! Ctrl-C to quit.");

                        // Read the three simple variables from the server
                        await ReadSimpleVariablesAsync(uaClient.Session, ct).ConfigureAwait(false);

                        // enable subscription transfer
                        uaClient.ReconnectPeriod = 1000;
                        uaClient.ReconnectPeriodExponentialBackoff = 10000;
                        uaClient.Session.MinPublishRequestCount = 3;
                        uaClient.Session.TransferSubscriptionsOnReconnect = true;
                        var samples = new ClientSamples(
                            telemetry,
                            ClientBase.ValidateResponse,
                            quitEvent,
                            verbose);
                        if (loadTypes)
                        {
                            Opc.Ua.Client.ComplexTypes.ComplexTypeSystem complexTypeSystem
                                = await samples
                                .LoadTypeSystemAsync(uaClient.Session, ct)
                                .ConfigureAwait(false);
                        }

                        if (browseall || fetchall || jsonvalues || managedbrowseall)
                        {
                            NodeIdCollection variableIds = null;
                            NodeIdCollection variableIdsManagedBrowse = null;
                            ReferenceDescriptionCollection referenceDescriptions = null;
                            ReferenceDescriptionCollection referenceDescriptionsFromManagedBrowse
                                = null;

                            if (browseall)
                            {
                                logger.LogInformation("Browse the full address space.");
                                referenceDescriptions = await samples
                                    .BrowseFullAddressSpaceAsync(uaClient, Objects.RootFolder, ct: ct)
                                    .ConfigureAwait(false);
                                variableIds =
                                [
                                    .. referenceDescriptions
                                        .Where(r =>
                                            r.NodeClass == NodeClass.Variable &&
                                            r.TypeDefinition.NamespaceIndex != 0
                                        )
                                        .Select(r => ExpandedNodeId.ToNodeId(
                                            r.NodeId,
                                            uaClient.Session.NamespaceUris))
                                ];
                            }

                            if (managedbrowseall)
                            {
                                logger.LogInformation("ManagedBrowse the full address space.");
                                referenceDescriptionsFromManagedBrowse = await samples
                                    .ManagedBrowseFullAddressSpaceAsync(
                                        uaClient,
                                        Objects.RootFolder,
                                        ct: ct)
                                    .ConfigureAwait(false);
                                variableIdsManagedBrowse =
                                [
                                    .. referenceDescriptionsFromManagedBrowse
                                        .Where(r =>
                                            r.NodeClass == NodeClass.Variable &&
                                            r.TypeDefinition.NamespaceIndex != 0
                                        )
                                        .Select(r => ExpandedNodeId.ToNodeId(
                                            r.NodeId,
                                            uaClient.Session.NamespaceUris))
                                ];
                            }

                            // treat managedBrowseall result like browseall results if the latter is missing
                            if (!browseall && managedbrowseall)
                            {
                                referenceDescriptions = referenceDescriptionsFromManagedBrowse;
                                browseall = managedbrowseall;
                            }

                            IList<INode> allNodes = null;
                            if (fetchall)
                            {
                                allNodes = await samples
                                    .FetchAllNodesNodeCacheAsync(
                                        uaClient,
                                        Objects.RootFolder,
                                        true,
                                        true,
                                        false,
                                        ct: ct)
                                    .ConfigureAwait(false);
                                variableIds =
                                [
                                    .. allNodes
                                        .Where(r =>
                                            r.NodeClass == NodeClass.Variable &&
                                            r is VariableNode v &&
                                            v.DataType.NamespaceIndex != 0
                                        )
                                        .Select(r => ExpandedNodeId.ToNodeId(
                                            r.NodeId,
                                            uaClient.Session.NamespaceUris))
                                ];
                            }

                            if (jsonvalues && variableIds != null)
                            {
                                (
                                    IReadOnlyList<DataValue> allValues,
                                    IReadOnlyList<ServiceResult> results
                                ) = await samples
                                    .ReadAllValuesAsync(uaClient, variableIds, ct)
                                    .ConfigureAwait(false);
                            }

                            if (subscribe && (browseall || fetchall))
                            {
                                // subscribe to 1000 random variables
                                const int maxVariables = 1000;
                                var variables = new NodeCollection();

                                if (fetchall)
                                {
                                    variables.AddRange(
                                        allNodes
                                            .Where(r =>
                                                r.NodeClass == NodeClass.Variable &&
                                                r.NodeId.NamespaceIndex > 1
                                            )
                                            .Cast<VariableNode>()
                                            .OrderBy(o => UnsecureRandom.Shared.Next())
                                            .Take(maxVariables)
                                    );
                                }
                                else if (browseall)
                                {
                                    var variableReferences = referenceDescriptions
                                        .Where(r => r.NodeClass == NodeClass.Variable &&
                                            r.NodeId.NamespaceIndex > 1)
                                        .Select(r => r.NodeId)
                                        .OrderBy(o => UnsecureRandom.Shared.Next())
                                        .Take(maxVariables)
                                        .ToList();
                                    variables.AddRange(
                                        (await uaClient.Session.NodeCache.FindAsync(variableReferences, ct)
                                            .ConfigureAwait(false))
                                            .Cast<Node>()
                                    );
                                }

                                await samples
                                    .SubscribeAllValuesAsync(
                                        uaClient,
                                        variableIds: [.. variables],
                                        samplingInterval: 100,
                                        publishingInterval: 1000,
                                        queueSize: 10,
                                        lifetimeCount: 60,
                                        keepAliveCount: 2,
                                        ct: ct
                                    )
                                    .ConfigureAwait(false);

                                // Wait for DataChange notifications from MonitoredItems
                                logger.LogInformation(
                                    "Subscribed to {Count} variables. Press Ctrl-C to exit.",
                                    maxVariables);

                                // free unused memory
                                uaClient.Session.NodeCache.Clear();

                                waitTime = timeout -
                                    (int)DateTime.UtcNow.Subtract(start).TotalMilliseconds;
                                DateTime endTime =
                                    waitTime > 0
                                        ? DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(waitTime))
                                        : DateTime.MaxValue;
                                List<Node>.Enumerator variableIterator = variables.GetEnumerator();
                                while (!quit && endTime > DateTime.UtcNow)
                                {
                                    if (variableIterator.MoveNext())
                                    {
                                        try
                                        {
                                            DataValue value = await uaClient
                                                .Session.ReadValueAsync(
                                                    variableIterator.Current.NodeId, ct)
                                                .ConfigureAwait(false);
                                            logger.LogInformation(
                                                "Value of {NodeId} is {Value}",
                                                variableIterator.Current.NodeId,
                                                value
                                            );
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogInformation(ex,
                                                "Error reading value of {NodeId}",
                                                variableIterator.Current.NodeId
                                            );
                                        }
                                    }
                                    else
                                    {
                                        variableIterator = variables.GetEnumerator();
                                    }
                                    quit = quitEvent.WaitOne(500);
                                }
                            }
                            else
                            {
                                quit = true;
                            }
                        }
                        else
                        {
                            // Skip all the reference server simulation tests
                            // Just read our simple variables and exit
                            logger.LogInformation("Skipping reference server tests. Press Ctrl-C to exit.");
                            quit = true;

                            // Wait for some DataChange notifications from MonitoredItems
                            int waitCounters = 0;
                            const int quitTimeout = 65_000;
                            const int checkForWaitTime = 1000;
                            const int closeSessionTime = checkForWaitTime * 15;
                            const int restartSessionTime = checkForWaitTime * 45;
                            const bool stopNotQuit = false;
                            int stopCount = 0;
                            while (!quit && !stopNotQuit && waitCounters < quitTimeout)
                            {
                                quit = quitEvent.WaitOne(checkForWaitTime);
                                waitCounters += checkForWaitTime;
                                if (enableDurableSubscriptions)
                                {
                                    if (waitCounters == closeSessionTime &&
                                        uaClient.Session.SubscriptionCount == 1)
                                    {
                                        logger.LogInformation(
                                            "Closing Session (CurrentTime: {Time})",
                                            DateTime.Now.ToLongTimeString());
                                        await uaClient.Session.CloseAsync(closeChannel: false, ct: ct)
                                            .ConfigureAwait(false);
                                    }

                                    if (waitCounters == restartSessionTime)
                                    {
                                        logger.LogInformation("Restarting Session (CurrentTime: {Time})",
                                            DateTime.Now.ToLongTimeString());
                                        await uaClient
                                            .DurableSubscriptionTransferAsync(
                                                serverUrl.ToString(),
                                                useSecurity: !noSecurity,
                                                ct
                                            )
                                            .ConfigureAwait(true);
                                    }

                                    if (waitCounters is > closeSessionTime and < restartSessionTime)
                                    {
                                        Console.WriteLine(
                                            "No Communication Interval " +
                                            stopCount.ToString(CultureInfo.InvariantCulture)
                                        );
                                        stopCount++;
                                    }
                                }
                            }
                        }

                        logger.LogInformation("Client disconnected.");

                        await uaClient.DisconnectAsync(leakChannels, ct).ConfigureAwait(false);

                        // Exit the loop after successful connection and monitoring
                        quit = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.WriteLine("  ✗ CONNECTION FAILED!");
                        Console.WriteLine("═══════════════════════════════════════════════════════════");
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine("Could not connect to: {0}", serverUrl);
                        Console.WriteLine();

                        logger.LogInformation(
                            "Could not connect to server!");
                        quit = true; // Don't retry, just exit
                    }
                } while (!quit);

                logger.LogInformation("Client stopped.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("  ✗ ERROR OCCURRED!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Error: {0}", ex.Message);
                Console.WriteLine();
                logger.LogInformation("{Error}", ex.Message);
            }
            finally
            {
                Utils.SilentDispose(reverseConnectManager);

                // Wait for user input before closing
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// returns a CertificateIdentifier of the Certificate with the specified thumbprint
        /// if it is found in the trustedUserCertificates TrustList
        /// </summary>
        /// <param name="thumbprint">the thumbprint of the certificate to select</param>
        /// <param name="trustedUserCertificates">the trustlist of the user certificates</param>
        /// <param name="ct">the cancellation token</param>
        /// <returns>Certificate Identifier</returns>
        private static async Task<CertificateIdentifier> FindUserCertificateIdentifierAsync(
            string thumbprint,
            CertificateTrustList trustedUserCertificates,
            ITelemetryContext telemetry,
            CancellationToken ct = default)
        {
            CertificateIdentifier userCertificateIdentifier = null;

            X509Certificate2Collection userCertificatesWithMatchingThumbprint =
                await trustedUserCertificates.GetCertificatesAsync(telemetry, ct).ConfigureAwait(false);
            // get user certificate with matching thumbprint
            userCertificatesWithMatchingThumbprint =
                userCertificatesWithMatchingThumbprint.Find(X509FindType.FindByThumbprint, thumbprint, false);

            // create Certificate Identifier
            if (userCertificatesWithMatchingThumbprint.Count == 1)
            {
                userCertificateIdentifier = new CertificateIdentifier(
                    userCertificatesWithMatchingThumbprint[0])
                {
                    StorePath = trustedUserCertificates.StorePath,
                    StoreType = trustedUserCertificates.StoreType
                };
            }

            return userCertificateIdentifier;
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
