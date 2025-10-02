# MedicalAI Thesis Suite

**Research use only. Not a medical device.**

This repository implements a cross-platform desktop application (.NET 8, Avalonia 11) that operationalizes a Ukrainian PhD thesis on AI for medical diagnostic complexes: DICOM/NIfTI ingest + anonymization, SKIF‑Seg‑style segmentation, KI‑GCN classification, multi‑teacher knowledge distillation orchestration, and Ukrainian NLP reasoning.

## Build

```
dotnet restore
dotnet build
dotnet run --project src/MedicalAI.UI
```

## Publish & Zip
Use the scripts in `build/` or run:

```
# Windows
dotnet publish src/MedicalAI.UI -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Linux
dotnet publish src/MedicalAI.UI -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true

# macOS (x64)
dotnet publish src/MedicalAI.UI -c Release -r osx-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

Then compress the publish output into `MedicalAI.ThesisSuite.zip`.

## Notes
- FO-DICOM is used for DICOM I/O and anonymization; anonymize-by-default is recommended.
- ONNX Runtime is wired but the default engines run deterministic mock inference so the app works offline.
- Sample data: `datasets/samples/sample.nii` (synthetic) and `datasets/samples/sample.dcm` (synthetic).

