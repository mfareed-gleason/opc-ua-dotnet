#!/bin/bash

pushd Applications/ConsoleReferenceClient > /dev/null
trap "popd > /dev/null" EXIT

dotnet build ConsoleReferenceClient.csproj --framework "net10.0" --configuration Release
./bin/Release/net10.0/ConsoleReferenceClient $@
