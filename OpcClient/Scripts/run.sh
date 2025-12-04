#!/bin/bash

echo "Running OPC Client..."
echo ""

cd "$(dirname "$0")/.."

# Build if needed
if [ ! -d "bin/Release/net10.0" ]; then
    echo "Building..."
    dotnet build -c Release
    echo ""
fi

# Check if server is running
if ! lsof -i :62541 > /dev/null 2>&1; then
    echo "⚠ Reference server not running on port 62541"
    echo "  Start with: ./run-server.sh"
    echo ""
fi

# Run (local testing)
exec dotnet run --no-build -c Release -f net10.0 "$@"
