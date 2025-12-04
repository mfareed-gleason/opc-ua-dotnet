#!/bin/bash

cd "$(dirname "$0")/.."

# Check for clean flag
for arg in "$@"; do
    if [[ "$arg" == "-c" ]] || [[ "$arg" == "--clean" ]]; then
        echo "Cleaning build artifacts..."
        rm -rf bin/Release/net10.0
        echo ""
        # Remove the -c|--clean argument
        set -- "${@/-c/}"
        set -- "${@/--clean/}"
        break
    fi
done
echo "Running OPC Client..."
echo ""

# Build if needed
if [ ! -d "bin/Release/net10.0" ]; then
    echo "Building..."
    dotnet build -c Release -f net10.0
    echo ""
fi

# Check a build exists
if [ ! -d "bin/Release/net10.0/OpcClient" ]; then
    echo "⚠ No build found. Please build the project first."
    echo "  Build with: ./run.sh -c|--clean"
    exit 1
fi

# Check if server is running
if ! lsof -i :62541 > /dev/null 2>&1; then
    echo "⚠ Reference server not running on port 62541"
    echo "  Start with: ../run-server.sh"
    exit 1
fi

# Run (local testing)
exec dotnet run --no-build -c Release -f net10.0 "$@"
