param([string]$rid="win-x64")
$ErrorActionPreference = "Stop"
dotnet restore
dotnet publish ./src/MedicalAI.UI -c Release -r $rid -p:PublishSingleFile=true -p:SelfContained=true
$pub = Join-Path "src/MedicalAI.UI" "bin/Release/net8.0/$rid/publish"
Compress-Archive -Path "$pub/*" -DestinationPath "./MedicalAI.ThesisSuite.zip" -Force
Write-Host "Packed MedicalAI.ThesisSuite.zip"
