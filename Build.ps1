Push-Location $PSScriptRoot

if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

& dotnet restore

$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];

Push-Location src/Serilog.Sinks.GoogleCloudPubSub

& dotnet pack -c Release -o ..\..\.\artifacts --version-suffix=$revision
if($LASTEXITCODE -ne 0) { exit 1 }    

Pop-Location
Push-Location test/Serilog.Sinks.GoogleCloudPubSub

& dotnet test -c Release
if($LASTEXITCODE -ne 0) { exit 2 }

Pop-Location
Pop-Location