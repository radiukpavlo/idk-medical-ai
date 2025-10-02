# MedicalAI Thesis Suite

**Research use only. Not a medical device.**

This repository implements a cross-platform desktop application (.NET 8, Avalonia 11) that operationalizes a Ukrainian PhD thesis on AI for medical diagnostic complexes: DICOM/NIfTI ingest + anonymization, SKIF‑Seg‑style segmentation, KI‑GCN classification, multi‑teacher knowledge distillation orchestration, and Ukrainian NLP reasoning.

## Getting Started

This guide will walk you through the process of setting up, building, and running the MedicalAI Thesis Suite on your local machine.

### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/MedicalAI.ThesisSuite.git
    cd MedicalAI.ThesisSuite
    ```

2.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```

## How to Run

The MedicalAI Thesis Suite includes both a graphical user interface (UI) and a command-line interface (CLI).

### Running the UI Application

To launch the desktop application, run the following command from the root of the repository:

```bash
dotnet run --project src/MedicalAI.UI
```

### Running the Command-Line Interface (CLI)

The CLI provides a way to run the application's core logic from the command line. It supports several arguments to customize its behavior.

**Basic Usage:**

To run the CLI with the default settings (using the sample data), execute the following command:

```bash
dotnet run --project src/MedicalAI.CLI
```

**Command-Line Arguments:**

*   `--input <path>`: Specifies the path to the input NIfTI file (e.g., `datasets/samples/sample.nii`).
*   `--model <path>`: Specifies the path to the segmentation model (e.g., `models/segmentation/mock.onnx`).
*   `--output <path>`: Specifies the path for the output PDF report (e.g., `CaseReport.pdf`).

**Example:**

```bash
dotnet run --project src/MedicalAI.CLI -- --input "path/to/your/image.nii" --model "path/to/your/model.onnx" --output "MyReport.pdf"
```
*(Note the `--` before the arguments, which is necessary to separate the application's arguments from `dotnet run`'s options.)*

## Architecture Overview

The solution is organized into several projects, each with a specific responsibility:

*   **`MedicalAI.Core`**: Contains the core domain models, interfaces, and business logic of the application.
*   **`MedicalAI.Infrastructure`**: Implements the interfaces defined in `Core`, providing concrete implementations for data access, file I/O, and other infrastructure-level concerns.
*   **`MedicalAI.Application`**: Contains the application logic, such as command and query handlers, that orchestrates the core domain.
*   **`MedicalAI.UI`**: The Avalonia-based desktop application.
*   **`MedicalAI.CLI`**: The command-line interface for the application.
*   **`MedicalAI.Plugins`**: Contains various plugins for specific functionalities, such as different segmentation or classification models.
*   **`tests/`**: Contains the unit and integration tests for the solution.

## Building and Publishing

### Building

To build the entire solution, run the following command:

```bash
dotnet build
```

### Publishing

To create a self-contained, single-file executable for distribution, use the `dotnet publish` command.

**Windows:**
```bash
dotnet publish src/MedicalAI.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Linux:**
```bash
dotnet publish src/MedicalAI.UI -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

**macOS (x64):**
```bash
dotnet publish src/MedicalAI.UI -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

## Notes
- **DICOM Support:** The application uses the `fo-dicom` library for DICOM I/O and anonymization. Anonymize-by-default is recommended.
- **Mock Inference:** The default configuration uses mock inference engines, which allows the application to run fully offline for demonstration purposes without requiring real ONNX models.
- **Sample Data:** Sample synthetic data can be found in the `datasets/samples/` directory.