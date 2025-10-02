# MedicalAI Thesis Suite

**⚠️ Research use only. Not a medical device.**

A cross-platform desktop application (.NET 8, Avalonia 11) that implements AI-powered medical diagnostic workflows based on Ukrainian PhD research. The suite provides comprehensive medical image processing capabilities including DICOM/NIfTI handling, advanced segmentation, classification, knowledge distillation, and Ukrainian natural language processing for medical reasoning.

## 🚀 Features

### Medical Image Processing
- **DICOM Support**: Complete DICOM file ingestion, processing, and anonymization using FO-DICOM
- **NIfTI Processing**: Advanced NIfTI file handling for neuroimaging and medical volume analysis
- **Image Anonymization**: Built-in patient data anonymization with configurable privacy levels

### AI-Powered Analysis
- **SKIF-Seg Segmentation**: Advanced medical image segmentation using SKIF-Seg methodology
- **KI-GCN Classification**: Intelligent image classification with Knowledge-Informed Graph Convolutional Networks
- **Multi-Teacher Distillation**: Sophisticated knowledge distillation orchestration for model optimization
- **Ukrainian NLP Reasoning**: Specialized natural language processing for Ukrainian medical terminology and reasoning

### Cross-Platform Desktop Application
- **Modern UI**: Built with Avalonia 11 for native performance across platforms
- **Real-time Visualization**: Interactive medical image viewing with ScottPlot integration
- **Responsive Design**: Fluent design system with adaptive layouts
- **Offline Capability**: Works without internet connection using mock inference engines

## 📋 System Requirements

### Minimum Requirements
- **Operating System**: Windows 10/11, Ubuntu 20.04+, macOS 10.15+
- **Runtime**: .NET 8.0 Runtime (included in self-contained builds)
- **Memory**: 4 GB RAM minimum, 8 GB recommended
- **Storage**: 2 GB available disk space
- **Graphics**: DirectX 11 compatible graphics card (Windows) or equivalent

### Development Requirements
- **.NET SDK**: .NET 8.0 SDK or later
- **IDE**: Visual Studio 2022, JetBrains Rider, or VS Code with C# extension
- **Git**: For source code management
- **PowerShell**: For Windows build scripts
- **Bash**: For Linux/macOS build scripts

### Optional Dependencies
- **ONNX Runtime**: For real AI model inference (included in builds)
- **CUDA**: For GPU-accelerated AI processing (optional)
- **DirectML**: For Windows GPU acceleration (included)

## 🛠️ Installation

### Option 1: Download Pre-built Binaries (Recommended)
1. Download the latest release from the [Releases](../../releases) page
2. Extract `MedicalAI.ThesisSuite.zip` to your desired location
3. Run the executable:
   - **Windows**: `MedicalAI.UI.exe`
   - **Linux**: `./MedicalAI.UI`
   - **macOS**: `./MedicalAI.UI`

### Option 2: Build from Source

#### Prerequisites Installation

**Windows:**
```powershell
# Install .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Or download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

**Ubuntu/Debian:**
```bash
# Install .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

**macOS:**
```bash
# Install .NET 8 SDK using Homebrew
brew install dotnet

# Or download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

#### Build Instructions

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd MedicalAI.ThesisSuite
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Solution**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run the Application**
   ```bash
   dotnet run --project src/MedicalAI.UI
   ```

#### Create Distributable Packages

**Windows (PowerShell):**
```powershell
# Build for Windows x64
.\build\publish.ps1 win-x64

# Build for Windows ARM64
.\build\publish.ps1 win-arm64
```

**Linux/macOS (Bash):**
```bash
# Build for Linux x64
./build/publish.sh linux-x64

# Build for macOS x64
./build/publish.sh osx-x64

# Build for macOS ARM64 (Apple Silicon)
./build/publish.sh osx-arm64
```

The build scripts will create a `MedicalAI.ThesisSuite.zip` file containing the self-contained application.

## 🏗️ Project Architecture

### Solution Structure
```
MedicalAI.ThesisSuite/
├── src/
│   ├── MedicalAI.Core/              # Domain models and interfaces
│   ├── MedicalAI.Application/       # Application services and use cases
│   ├── MedicalAI.Infrastructure/    # Data access and external services
│   ├── MedicalAI.UI/               # Avalonia desktop application
│   ├── MedicalAI.CLI/              # Command-line interface
│   └── MedicalAI.Plugins/          # AI model plugins
│       ├── Segmentation.SKIFSeg/   # Medical image segmentation
│       ├── Classification.KIGCN/   # Image classification
│       ├── Distillation.MultiTeacher/ # Knowledge distillation
│       └── NLP.MedReasoning.UA/    # Ukrainian NLP reasoning
├── tests/                          # Unit and integration tests
├── datasets/                       # Sample medical data
├── models/                         # AI model files
├── build/                          # Build and deployment scripts
└── docs/                          # Documentation (generated)
```

### Technology Stack
- **Framework**: .NET 8.0
- **UI Framework**: Avalonia 11.0
- **Design System**: FluentAvaloniaUI
- **MVVM**: CommunityToolkit.Mvvm
- **Medical Imaging**: FO-DICOM, ImageSharp
- **AI/ML**: ONNX Runtime, Microsoft.ML
- **Data Visualization**: ScottPlot
- **Database**: Entity Framework Core with SQLite
- **Logging**: Serilog
- **Testing**: xUnit, FluentAssertions

## 🚀 Quick Start

### Running the Application
1. Launch the application using one of the installation methods above
2. The main window will open with the medical image processing interface
3. Use the File menu to load sample data or your own medical images

### Loading Sample Data
The application includes synthetic sample data for testing:
- **DICOM Sample**: `datasets/samples/sample.dcm`
- **NIfTI Sample**: `datasets/samples/sample.nii`

### Basic Workflow
1. **Load Medical Images**: Use File → Open to load DICOM or NIfTI files
2. **Process Images**: Apply segmentation, classification, or other AI processing
3. **View Results**: Examine processed images and analysis results
4. **Export Data**: Save processed images and reports

## 🧪 Development

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/MedicalAI.Core.Tests
```

### Code Quality
The project follows clean architecture principles and includes:
- Comprehensive unit tests
- Integration tests for critical workflows
- Code analysis and style enforcement
- Nullable reference types throughout

### Plugin Development
The application supports custom AI plugins. See the existing plugins in `src/MedicalAI.Plugins/` for examples of:
- Implementing the plugin interface
- Integrating with ONNX Runtime
- Handling medical image data

## 📚 Documentation

- **API Documentation**: Generated from code comments (see `docs/api/`)
- **User Guide**: Comprehensive feature documentation (see `docs/user-guide/`)
- **Developer Guide**: Architecture and development information (see `docs/developer/`)
- **Troubleshooting**: Common issues and solutions (see `docs/troubleshooting/`)

## 🔒 Security and Privacy

### Medical Data Handling
- **Anonymization**: Automatic DICOM anonymization with configurable privacy levels
- **Local Processing**: All AI processing happens locally - no data sent to external services
- **Secure Storage**: Temporary data is securely cleaned up after processing
- **Audit Logging**: Comprehensive logging of medical data access and processing

### Compliance Considerations
- Designed for research use only
- Not intended for clinical diagnosis or treatment decisions
- Users responsible for ensuring compliance with local regulations (HIPAA, GDPR, etc.)
- No patient data is transmitted or stored permanently without explicit user action

## 🤝 Contributing

This is a research project. For questions or collaboration opportunities, please contact the project maintainers.

### Development Setup
1. Fork the repository
2. Create a feature branch
3. Follow the existing code style and architecture
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

See [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Based on Ukrainian PhD research in medical AI diagnostics
- Uses FO-DICOM for medical image processing
- Powered by ONNX Runtime for AI inference
- Built with Avalonia for cross-platform desktop development

## 📞 Support

For technical issues or questions:
1. Check the [Troubleshooting Guide](docs/troubleshooting/)
2. Review existing [Issues](../../issues)
3. Create a new issue with detailed information about your problem

---

**Remember**: This software is for research purposes only and should not be used for medical diagnosis or treatment decisions.

