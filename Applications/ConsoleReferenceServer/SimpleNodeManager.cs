/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
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
using System.Threading;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;

namespace Quickstarts.ReferenceServer
{
    /// <summary>
    /// Factory for creating SimpleNodeManager instances.
    /// </summary>
    public class SimpleNodeManagerFactory : INodeManagerFactory
    {
        private readonly bool m_enableSimulation;

        public SimpleNodeManagerFactory(bool enableSimulation = false)
        {
            m_enableSimulation = enableSimulation;
        }

        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new SimpleNodeManager(server, configuration, m_enableSimulation);
        }

        /// <inheritdoc/>
        public StringCollection NamespacesUris => ["http://opcfoundation.org/SimpleVariables"];
    }

    /// <summary>
    /// A simple node manager that exposes three variables: two strings and one integer.
    /// </summary>
    public class SimpleNodeManager : CustomNodeManager2
    {
        private Timer m_simulationTimer;
        private BaseDataVariableState m_stringVar1;
        private BaseDataVariableState m_stringVar2;
        private BaseDataVariableState m_intVar;
        private readonly bool m_enableSimulation;
        private readonly string m_persistenceFilePath;
        private int m_counter;

        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public SimpleNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration,
            bool enableSimulation = false)
            : base(
                  server,
                  configuration,
                  false,
                  server.Telemetry.CreateLogger<SimpleNodeManager>(),
                  "http://opcfoundation.org/SimpleVariables")
        {
            m_enableSimulation = enableSimulation;
            m_persistenceFilePath = ResolvePersistenceFilePath(configuration);
        }

        /// <summary>
        /// Disposes the node manager.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_simulationTimer?.Dispose();
                PersistCurrentState();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates the address space with three simple variables.
        /// </summary>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                // Get or create references to the ObjectsFolder
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference> references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = [];
                }

                // Create a folder to organize our variables
                FolderState myFolder = CreateFolder(null, "MyVariables", "My Variables");
                myFolder.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, myFolder.NodeId));

                // Create two string variables
                m_stringVar1 = CreateVariable(
                    myFolder,
                    "StringVariable1",
                    "Part Number",
                    DataTypeIds.String,
                    ValueRanks.Scalar);
                m_stringVar1.Value = string.Empty;
                m_stringVar1.OnSimpleWriteValue = OnWritePersistedValue;

                m_stringVar2 = CreateVariable(
                    myFolder,
                    "StringVariable2",
                    "Order Number",
                    DataTypeIds.String,
                    ValueRanks.Scalar);
                m_stringVar2.Value = string.Empty;
                m_stringVar2.OnSimpleWriteValue = OnWritePersistedValue;

                // Create one integer variable
                m_intVar = CreateVariable(
                    myFolder,
                    "IntegerVariable",
                    "Production Count",
                    DataTypeIds.Int32,
                    ValueRanks.Scalar);
                m_intVar.Value = 0;
                m_intVar.OnSimpleWriteValue = OnWritePersistedValue;

                RestorePersistedState();

                // Add the nodes to the system
                AddPredefinedNode(SystemContext, myFolder);

                // Start simulation timer if enabled via command line
                if (m_enableSimulation)
                {
                    m_counter = 0;
                    m_simulationTimer = new Timer(DoSimulation, null, 2000, 2000);
                }
            }
        }

        /// <summary>
        /// Updates the variable values periodically.
        /// </summary>
        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    m_counter++;

                    // Update string variables
                    m_stringVar1.Value = $"Value {m_counter} at {DateTime.Now:HH:mm:ss}";
                    m_stringVar1.Timestamp = DateTime.UtcNow;
                    m_stringVar1.ClearChangeMasks(SystemContext, false);

                    m_stringVar2.Value = $"Updated {m_counter} times";
                    m_stringVar2.Timestamp = DateTime.UtcNow;
                    m_stringVar2.ClearChangeMasks(SystemContext, false);

                    // Update integer variable (count up)
                    m_intVar.Value = m_counter;
                    m_intVar.Timestamp = DateTime.UtcNow;
                    m_intVar.ClearChangeMasks(SystemContext, false);
                }
            }
            catch (Exception e)
            {
                m_logger.LogError(e, "Unexpected error during simulation.");
            }
        }

        private ServiceResult OnWritePersistedValue(ISystemContext context, NodeState node, ref object value)
        {
            try
            {
                PersistCurrentState(node, value);
                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                m_logger.LogError(e, "Failed to persist value written to node {NodeId}.", node?.NodeId);
                return StatusCodes.BadUnexpectedError;
            }
        }

        private void RestorePersistedState()
        {
            if (string.IsNullOrWhiteSpace(m_persistenceFilePath) || !File.Exists(m_persistenceFilePath))
            {
                return;
            }

            try
            {
                XDocument document = XDocument.Load(m_persistenceFilePath);
                XElement root = document.Element("SimpleNodeManagerState");

                if (root == null)
                {
                    return;
                }

                m_stringVar1.Value = (string)root.Element("StringVariable1") ?? string.Empty;
                m_stringVar1.Timestamp = DateTime.UtcNow;
                m_stringVar1.StatusCode = StatusCodes.Good;

                m_stringVar2.Value = (string)root.Element("StringVariable2") ?? string.Empty;
                m_stringVar2.Timestamp = DateTime.UtcNow;
                m_stringVar2.StatusCode = StatusCodes.Good;

                m_intVar.Value = (int?)root.Element("IntegerVariable") ?? 0;
                m_intVar.Timestamp = DateTime.UtcNow;
                m_intVar.StatusCode = StatusCodes.Good;

                m_logger.LogInformation(
                    "Loaded persisted simple variable state from {PersistenceFilePath}.",
                    m_persistenceFilePath);
            }
            catch (Exception e)
            {
                m_logger.LogError(
                    e,
                    "Failed to restore simple variable state from {PersistenceFilePath}.",
                    m_persistenceFilePath);
            }
        }

        private void PersistCurrentState(NodeState pendingNode = null, object pendingValue = null)
        {
            if (string.IsNullOrWhiteSpace(m_persistenceFilePath) ||
                m_stringVar1 == null ||
                m_stringVar2 == null ||
                m_intVar == null)
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(m_persistenceFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            XDocument document = new(
                new XElement(
                    "SimpleNodeManagerState",
                    new XElement(
                        "StringVariable1",
                        GetPersistedStringValue(m_stringVar1, pendingNode, pendingValue)),
                    new XElement(
                        "StringVariable2",
                        GetPersistedStringValue(m_stringVar2, pendingNode, pendingValue)),
                    new XElement(
                        "IntegerVariable",
                        GetPersistedIntegerValue(m_intVar, pendingNode, pendingValue))));

            string temporaryPath = m_persistenceFilePath + ".tmp";
            document.Save(temporaryPath);

            if (File.Exists(m_persistenceFilePath))
            {
                File.Replace(temporaryPath, m_persistenceFilePath, null);
                return;
            }

            File.Move(temporaryPath, m_persistenceFilePath);
        }

        private static string ResolvePersistenceFilePath(ApplicationConfiguration configuration)
        {
            string configuredPath = configuration?.ServerConfiguration?.NodeManagerSaveFile;

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        }

        private static string GetPersistedStringValue(
            BaseDataVariableState variable,
            NodeState pendingNode,
            object pendingValue)
        {
            if (ReferenceEquals(variable, pendingNode))
            {
                return pendingValue?.ToString() ?? string.Empty;
            }

            return variable.Value?.ToString() ?? string.Empty;
        }

        private static int GetPersistedIntegerValue(
            BaseDataVariableState variable,
            NodeState pendingNode,
            object pendingValue)
        {
            object valueToConvert = ReferenceEquals(variable, pendingNode) ? pendingValue : variable.Value;

            if (valueToConvert == null)
            {
                return 0;
            }

            return Convert.ToInt32(valueToConvert, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates a folder node.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);
            return folder;
        }

        /// <summary>
        /// Creates a variable node.
        /// </summary>
        private BaseDataVariableState CreateVariable(
            NodeState parent,
            string path,
            string name,
            NodeId dataType,
            int valueRank)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false,
                Value = null,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0]);
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
            }

            parent?.AddChild(variable);
            return variable;
        }
    }
}
