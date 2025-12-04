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

Push-Location Applications/ConsoleReferenceServer
try {
    if ($BUILD) {
        Write-Host "Building server..."
        dotnet build ConsoleReferenceServer.csproj --framework "net10.0" --configuration Release
        Write-Host ""
    }

    & ./bin/Release/net10.0/ConsoleReferenceServer -lc @ARGS
}
finally {
    Pop-Location
}
