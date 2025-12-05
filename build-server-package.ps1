param(
    [string]$OutputFolder = "ServerPackage"
)

Push-Location Applications/ConsoleReferenceServer
try {
    Write-Host "Building and packaging server..."
    dotnet publish ConsoleReferenceServer.csproj --framework "net10.0" --runtime "win-x64" --configuration Release --self-contained -p:PublishSingleFile=true -o "../../$OutputFolder"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Package created at: $OutputFolder" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
