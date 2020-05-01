Param(
  [Parameter()]
  [string]
  $versionSuffix = $null
)

# You need chocolatey installed.

$ErrorActionPreference = "Stop";

# Clean the artifacts directory
Remove-Item artifacts/win-x64 -Recurse -Force

if ($versionSuffix)
{
  # Build single file windows.
  dotnet publish ./src/autostep/ -r win-x64 -o ./artifacts/win-x64 -c Release --version-suffix $versionSuffix /p:PublishSingleFile=true
}
else
{
  # Build single file windows.
  dotnet publish ./src/autostep/ -r win-x64 -o ./artifacts/win-x64 -c Release /p:PublishSingleFile=true
}

Push-Location artifacts/win-x64

# Rename all self-contained exe files to remove the '-cli' portion. We want to publish an autostep.exe.
Get-ChildItem autostep-cli* -Recurse | Rename-Item -NewName {$_.Name -replace 'autostep-cli', 'autostep' }

Pop-Location

$binaryPath = Resolve-Path ./artifacts/win-x64

if (Test-Path ./artifacts/choco)
{
  Remove-Item ./artifacts/choco -Recurse -Force;
}

New-Item -ItemType Directory ./artifacts/choco;

if ($versionSuffix)
{
  # Create the chocolatey package.
  choco pack ./build/choco/autostep.nuspec --out ./artifacts/choco versionsuffix=-$versionSuffix "binaryFolder=$binaryPath"
}
else 
{
  choco pack ./build/choco/autostep.nuspec --out ./artifacts/choco "versionsuffix= " "binaryFolder=$binaryPath"
}