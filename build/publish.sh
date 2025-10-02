#!/usr/bin/env bash
set -euo pipefail
rid=${1:-linux-x64}
dotnet restore
dotnet publish ./src/MedicalAI.UI -c Release -r "$rid" -p:PublishSingleFile=true -p:SelfContained=true
pub="src/MedicalAI.UI/bin/Release/net8.0/$rid/publish"
(cd "$pub" && zip -r "../../../../../../MedicalAI.ThesisSuite.zip" .)
echo "Packed MedicalAI.ThesisSuite.zip"
