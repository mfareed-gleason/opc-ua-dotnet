#!/bin/bash

# Clean output
cd "$(dirname "$0")/.."
rm -rf Output

# Detect current platform
ARCH=$(uname -m)
OS=$(uname -s)

if [ "$OS" == "Darwin" ]; then
    if [ "$ARCH" == "arm64" ]; then
        RID="osx-arm64"
    else
        RID="osx-x64"
    fi
elif [ "$OS" == "Linux" ]; then
    if [ "$ARCH" == "x86_64" ]; then
        RID="linux-x64"
    elif [ "$ARCH" == "aarch64" ]; then
        RID="linux-arm64"
    fi
else
    # Windows
    RID="win-x64"
fi

# Publish as self-contained executable
echo "Building OPC Client for $RID..."
echo "Building executable..."
dotnet publish -c Release -f net10.0 -r $RID --self-contained true
echo "Compiling libraries..."
dotnet publish -c Release -f netstandard2.1 -r $RID

if [ $? -eq 0 ]; then
    # Create Output directory
    mkdir -p Output

    # Copy files
    cp bin/Release/net10.0/$RID/publish/OpcClient Output/ 2>/dev/null || cp bin/Release/net10.0/$RID/publish/OpcClient.exe Output/
    cp -r bin/Release/netstandard2.1/$RID/publish/* Output/ 2>/dev/null || cp -r bin/Release/netstandard2.1/$RID/publish/* Output/
    cp -r Config Output/

    echo ""
    echo "✓ Build complete at: Output/"
else
    echo ""
    echo "✗ Build failed"
    exit 1
fi
