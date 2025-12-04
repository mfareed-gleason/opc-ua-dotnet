param(
    [switch]$Build,
    [switch]$b,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$BUILD = $Build -or $b

$ARGS = @()
foreach ($arg in $RemainingArgs) {
    $ARGS += $arg
}

Push-Location Applications/ConsoleReferenceClient
try {
    if ($BUILD) {
        Write-Host "Building client..."
        dotnet build ConsoleReferenceClient.csproj --framework "net10.0" --configuration Release
        Write-Host ""
    }

    & ./bin/Release/net10.0/ConsoleReferenceClient -lc @ARGS
}
finally {
    Pop-Location
}
