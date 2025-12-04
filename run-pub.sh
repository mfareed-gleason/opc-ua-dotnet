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

pushd Applications/ConsoleReferencePublisher > /dev/null
trap "popd > /dev/null" EXIT

if [ "$BUILD" = true ]; then
    echo "Building Publisher..."
    dotnet build ConsoleReferencePublisher.csproj --framework "net10.0" --configuration Release
    echo ""
fi

exec ./bin/Release/net10.0/ConsoleReferencePublisher "${ARGS[@]}"
