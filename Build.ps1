Push-Location $PSScriptRoot

if(Test-Path .\artifacts) {
    echo "build: Cleaning .\artifacts" 
    Remove-Item .\artifacts -Force -Recurse 
}
if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

& dotnet restore

$branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$env:APPVEYOR_REPO_BRANCH -ne $NULL];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $env:APPVEYOR_BUILD_NUMBER, 10); $false = "local" }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]
$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];

echo "build: Version suffix is $suffix"

Push-Location src/Serilog.Sinks.GoogleCloudPubSub

    echo "build: Packaging project in $src"
& dotnet pack -c Release -o ..\..\.\artifacts --version-suffix=$revision
if($LASTEXITCODE -ne 0) { exit 1 }    

    & dotnet pack -c Release -o ..\..\.\artifacts --version-suffix=$suffix
    if($LASTEXITCODE -ne 0) { exit 1 }    




Pop-Location
Push-Location test/Serilog.Sinks.GoogleCloudPubSub
Pop-Location
Push-Location test/Serilog.Sinks.GoogleCloudPubSub.Tests

foreach ($test in ls test/Serilog.*.Tests) {
    Push-Location $test
& dotnet test -c Release
if($LASTEXITCODE -ne 0) { exit 2 }

    echo "build: Testing project in $test"
    
    & dotnet test -c Release
    if($LASTEXITCODE -ne 0) { exit 2 }
Pop-Location

    Pop-Location
}
& dotnet build -c Release
if($LASTEXITCODE -ne 0) { exit 2 }

Pop-Location
Pop-Location