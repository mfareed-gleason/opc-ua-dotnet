#!/bin/bash

pushd Applications/ConsoleReferenceServer > /dev/null
trap "popd > /dev/null" EXIT

dotnet run --project ConsoleReferenceServer.csproj --framework "net10.0" --configuration Release
