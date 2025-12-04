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

pushd Applications/ConsoleReferenceSubscriber > /dev/null
trap "popd > /dev/null" EXIT

if [ "$BUILD" = true ]; then
    echo "Building client..."
    dotnet build ConsoleReferenceSubscriber.csproj --framework "net10.0" --configuration Release
    echo ""
fi

exec ./bin/Release/net10.0/ConsoleReferenceSubscriber "${ARGS[@]}"
