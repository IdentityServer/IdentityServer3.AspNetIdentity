param(
    [int]$buildNumber = 0,
    [string]$preRelease = $null
)

if(Test-Path Env:\APPVEYOR_BUILD_NUMBER){
    $buildNumber = [int]$Env:APPVEYOR_BUILD_NUMBER
    Write-Host "Using APPVEYOR_BUILD_NUMBER"
}

"Build number $buildNumber"

source\.nuget\nuget.exe i source\.nuget\packages.config -o source\packages

$packageConfigs = Get-ChildItem . -Recurse | where{$_.Name -like "packages.*.config"}
foreach($packageConfig in $packageConfigs){
    Write-Host "Restoring" $packageConfig.FullName
    source\.nuget\nuget.exe i $packageConfig.FullName -o source\packages
}

Import-Module .\source\packages\psake.4.4.1\tools\psake.psm1
Invoke-Psake .\default.ps1 default -framework "4.0x64" -properties @{ buildNumber=$buildNumber; preRelease=$preRelease }
Remove-Module psake
