$ErrorActionPreference = "Stop";

# Build single file.
dotnet publish ./src/autostep/ -r win-x64 -o .\artifacts\win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true

# Rename all self-contained exe files to remove the '-cli' portion. We want to publish an autostep.exe.
Get-ChildItem autostep-cli* -Recurse | Rename-Item -NewName {$_.Name -replace 'autostep-cli', 'autostep' }