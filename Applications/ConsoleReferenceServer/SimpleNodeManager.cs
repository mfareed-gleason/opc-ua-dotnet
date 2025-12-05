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
        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new SimpleNodeManager(server, configuration);
        }

        /// <inheritdoc/>
        public StringCollection NamespacesUris => ["http://opcfoundation.org/SimpleVariables"];
    }

    /// <summary>
    /// A simple node manager that exposes three variables: two strings and one integer.
    /// </summary>
    public class SimpleNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public SimpleNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration)
            : base(
                  server,
                  configuration,
                  false,
                  server.Telemetry.CreateLogger<SimpleNodeManager>(),
                  "http://opcfoundation.org/SimpleVariables")
        {
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
                BaseDataVariableState stringVar1 = CreateVariable(
                    myFolder,
                    "StringVariable1",
                    "String Variable 1",
                    DataTypeIds.String,
                    ValueRanks.Scalar);
                stringVar1.Value = "Hello from Variable 1";

                BaseDataVariableState stringVar2 = CreateVariable(
                    myFolder,
                    "StringVariable2",
                    "String Variable 2",
                    DataTypeIds.String,
                    ValueRanks.Scalar);
                stringVar2.Value = "Hello from Variable 2";

                // Create one integer variable
                BaseDataVariableState intVar = CreateVariable(
                    myFolder,
                    "IntegerVariable",
                    "Integer Variable",
                    DataTypeIds.Int32,
                    ValueRanks.Scalar);
                intVar.Value = 42;

                // Add the nodes to the system
                AddPredefinedNode(SystemContext, myFolder);
            }
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
