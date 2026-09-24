param([switch]$IncludeDesktop)
$ErrorActionPreference = 'Stop'
$solution = if ($IncludeDesktop -or $env:OS -eq 'Windows_NT') { 'FotoAutomat.slnx' } else { 'FotoAutomat.Portable.slnx' }

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    Invoke-Dotnet restore $solution --locked-mode
    Invoke-Dotnet format $solution --verify-no-changes --no-restore
    Invoke-Dotnet build $solution --configuration Release --no-restore
    Invoke-Dotnet test tests/FotoAutomat.Tests --configuration Release --no-build --no-restore --logger 'trx;LogFileName=tests.trx' --results-directory artifacts/test-results
    Invoke-Dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build
}
finally {
    Pop-Location
}
