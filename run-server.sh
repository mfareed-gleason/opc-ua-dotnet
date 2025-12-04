#!/bin/bash

BUILD=false
for arg in "$@"; do
    if [[ "$arg" == "-b" ]] || [[ "$arg" == "--build" ]]; then
        BUILD=true
        break
    fi
done

ARGS=()
for arg in "$@"; do
    if [[ "$arg" != "-b" ]] && [[ "$arg" != "--build" ]]; then
        ARGS+=("$arg")
    fi
done

pushd Applications/ConsoleReferenceServer > /dev/null
trap "popd > /dev/null" EXIT

if [ "$BUILD" = true ]; then
    echo "Building server..."
    dotnet build ConsoleReferenceServer.csproj --framework "net10.0" --configuration Release
    echo ""
fi

exec ./bin/Release/net10.0/ConsoleReferenceServer -lc "${ARGS[@]}"
