$ErrorActionPreference = "Stop";

# Clean the artifacts directory
Remove-Item artifacts -Recurse -Force

# Build single file.
dotnet publish ./src/autostep/ -r win-x64 -o .\artifacts\win-x64 -c Release /p:PublishSingleFile=true

Push-Location artifacts/win-x64

# Rename all self-contained exe files to remove the '-cli' portion. We want to publish an autostep.exe.
Get-ChildItem autostep-cli* -Recurse | Rename-Item -NewName {$_.Name -replace 'autostep-cli', 'autostep' }

Pop-Location