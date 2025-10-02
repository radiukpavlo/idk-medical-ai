# User Guide

Welcome to the MedicalAI Thesis Suite - a comprehensive AI-powered medical diagnostic application for research purposes. This guide will help you understand and use all the features of the application effectively.

## Table of Contents
- [Getting Started](#getting-started)
- [Application Overview](#application-overview)
- [Main Features](#main-features)
- [Workflows and Use Cases](#workflows-and-use-cases)
- [Data Management](#data-management)
- [Settings and Configuration](#settings-and-configuration)
- [Tips and Best Practices](#tips-and-best-practices)

## Getting Started

### First Launch

When you first launch the MedicalAI Thesis Suite, you'll see:

1. **Research Warning Banner**: A red banner at the top stating "Research use only. Not a medical device" (displayed in Ukrainian by default)
2. **Navigation Panel**: A left sidebar with all available features
3. **Dashboard**: The main welcome screen with basic information

### Language Support

The application supports both Ukrainian and English:
- **Ukrainian**: Default interface language (reflecting the thesis origin)
- **English**: Available for international users
- Language can be changed in the Settings section

### Sample Data

The application includes synthetic sample data for testing:
- **DICOM Sample**: `datasets/samples/sample.dcm` - Synthetic medical imaging data
- **NIfTI Sample**: `datasets/samples/sample.nii` - Synthetic neuroimaging data

## Application Overview

### Main Interface

The application uses a modern navigation-based interface with the following sections:

#### Navigation Menu
- **Dashboard (Панель)**: Main overview and welcome screen
- **Datasets (Набори даних)**: Data management and file operations
- **Segmentation (Сегментація)**: Medical image segmentation tools
- **Classification (Класифікація)**: Image classification and analysis
- **Distillation (Дистиляція)**: Knowledge distillation workflows
- **NLP (НЛП)**: Ukrainian natural language processing
- **Reports (Звіти)**: Analysis results and documentation
- **Settings (Налаштування)**: Application configuration
- **About (Про дисертацію)**: Information about the research thesis

### Key Concepts

#### Research Focus
This application implements workflows from Ukrainian PhD research in medical AI diagnostics, including:
- **Knowledge Integration**: Combining multiple AI models and data sources
- **Cardiac MRI Segmentation**: Using SKIF-Seg methodology
- **Graph Classification**: Implementing KI-GCN (Knowledge-Informed Graph Convolutional Networks)
- **Multi-Teacher Knowledge Distillation**: Advanced model optimization techniques
- **Ukrainian Medical NLP**: Natural language processing for Ukrainian medical terminology

#### Offline Operation
The application is designed to work offline with mock inference engines, making it suitable for:
- Demonstration purposes
- Educational use
- Research environments without internet access
- Secure medical environments

## Main Features

### 1. Dashboard

**Purpose**: Central hub for application overview and quick access to features

**Features**:
- Welcome message and application status
- Quick access to recent files and projects
- System status indicators
- Usage statistics and metrics

**How to Use**:
1. Launch the application
2. Review the dashboard for system status
3. Use quick access buttons for common tasks
4. Check for any system notifications or alerts

### 2. Dataset Management

**Purpose**: Handle medical imaging files and data organization

**Supported Formats**:
- **DICOM (.dcm)**: Digital Imaging and Communications in Medicine files
- **NIfTI (.nii, .nii.gz)**: Neuroimaging Informatics Technology Initiative files
- **Standard Images**: JPEG, PNG, TIFF for general medical imaging

**Features**:
- File import and export
- Data validation and integrity checking
- Metadata extraction and display
- Batch processing capabilities
- Anonymization tools

**How to Use**:
1. Navigate to "Datasets" section
2. Click "Import" to load medical images
3. Select files or drag-and-drop into the application
4. Review file information and metadata
5. Use batch operations for multiple files

### 3. Medical Image Segmentation

**Purpose**: Segment medical images using SKIF-Seg methodology

**Capabilities**:
- Cardiac MRI segmentation
- Automated region detection
- Manual refinement tools
- Multi-class segmentation
- 3D volume processing

**Workflow**:
1. Load medical image (DICOM or NIfTI)
2. Select segmentation algorithm
3. Configure segmentation parameters
4. Run automated segmentation
5. Review and refine results manually if needed
6. Export segmented regions

**Parameters**:
- **Threshold Settings**: Adjust sensitivity for region detection
- **Region Types**: Select specific anatomical structures
- **Processing Mode**: Choose between speed and accuracy
- **Output Format**: Select result file format

### 4. Image Classification

**Purpose**: Classify medical images using KI-GCN (Knowledge-Informed Graph Convolutional Networks)

**Features**:
- Multi-class medical image classification
- Graph-based feature analysis
- Knowledge integration from medical databases
- Confidence scoring
- Batch classification

**Classification Types**:
- **Pathology Detection**: Identify abnormal conditions
- **Anatomical Classification**: Categorize body regions
- **Severity Assessment**: Grade condition severity
- **Tissue Type Recognition**: Identify different tissue types

**How to Use**:
1. Load preprocessed medical images
2. Select classification model
3. Configure classification parameters
4. Run classification analysis
5. Review results and confidence scores
6. Export classification reports

### 5. Knowledge Distillation

**Purpose**: Optimize AI models using multi-teacher knowledge distillation

**Concepts**:
- **Teacher Models**: Large, accurate models that provide knowledge
- **Student Models**: Smaller, efficient models that learn from teachers
- **Knowledge Transfer**: Process of transferring learned features
- **Model Compression**: Reducing model size while maintaining accuracy

**Workflow**:
1. Load teacher models (pre-trained AI models)
2. Define student model architecture
3. Configure distillation parameters
4. Run knowledge transfer process
5. Evaluate student model performance
6. Deploy optimized model

**Benefits**:
- Reduced model size for deployment
- Faster inference times
- Maintained accuracy
- Better resource utilization

### 6. Ukrainian NLP Processing

**Purpose**: Process Ukrainian medical text and reasoning

**Capabilities**:
- **Medical Terminology Recognition**: Identify Ukrainian medical terms
- **Clinical Note Analysis**: Process medical documentation
- **Symptom Extraction**: Extract symptoms from text descriptions
- **Medical Reasoning**: Logical inference from medical information
- **Report Generation**: Create structured medical reports

**Features**:
- Ukrainian language-specific processing
- Medical domain vocabulary
- Context-aware analysis
- Multi-document processing
- Export to various formats

**Use Cases**:
- Analyzing Ukrainian medical records
- Processing patient descriptions
- Generating medical summaries
- Extracting clinical insights
- Supporting medical decision-making

### 7. Reports and Documentation

**Purpose**: Generate comprehensive analysis reports

**Report Types**:
- **Segmentation Reports**: Detailed segmentation analysis
- **Classification Results**: Classification outcomes and statistics
- **Processing Logs**: Technical processing information
- **Performance Metrics**: System and algorithm performance
- **Medical Summaries**: Clinical findings and recommendations

**Export Formats**:
- PDF documents
- Excel spreadsheets
- JSON data files
- HTML reports
- Medical imaging formats

## Workflows and Use Cases

### Workflow 1: Basic Medical Image Analysis

**Scenario**: Analyze a DICOM cardiac MRI scan

**Steps**:
1. **Import Data**:
   - Go to Datasets section
   - Import DICOM file
   - Verify file integrity and metadata

2. **Segmentation**:
   - Navigate to Segmentation section
   - Load the DICOM file
   - Select cardiac segmentation algorithm
   - Run automated segmentation
   - Review and refine results

3. **Classification**:
   - Go to Classification section
   - Load segmented image
   - Run pathology detection
   - Review classification results

4. **Generate Report**:
   - Navigate to Reports section
   - Select analysis results
   - Generate comprehensive report
   - Export as PDF

### Workflow 2: Batch Processing Multiple Images

**Scenario**: Process multiple NIfTI brain scans

**Steps**:
1. **Batch Import**:
   - Use Datasets section
   - Select multiple NIfTI files
   - Import all files simultaneously

2. **Automated Processing**:
   - Configure batch processing settings
   - Apply segmentation to all images
   - Run classification on all results

3. **Results Review**:
   - Review batch processing results
   - Identify any processing errors
   - Manually review flagged cases

4. **Export Results**:
   - Generate batch report
   - Export all results in preferred format

### Workflow 3: Model Optimization

**Scenario**: Create an optimized model for deployment

**Steps**:
1. **Prepare Teacher Models**:
   - Load pre-trained segmentation models
   - Load classification models
   - Verify model performance

2. **Design Student Model**:
   - Define smaller model architecture
   - Set performance targets
   - Configure distillation parameters

3. **Knowledge Distillation**:
   - Run multi-teacher distillation
   - Monitor training progress
   - Evaluate student model performance

4. **Model Deployment**:
   - Export optimized model
   - Test deployment performance
   - Document model specifications

### Workflow 4: Ukrainian Medical Text Analysis

**Scenario**: Analyze Ukrainian medical records

**Steps**:
1. **Text Import**:
   - Navigate to NLP section
   - Import Ukrainian medical text
   - Verify text encoding and format

2. **Text Processing**:
   - Run medical terminology extraction
   - Perform symptom analysis
   - Generate clinical insights

3. **Results Integration**:
   - Combine with imaging analysis
   - Create comprehensive patient profile
   - Generate integrated report

## Data Management

### File Organization

**Recommended Structure**:
```
MedicalAI_Projects/
├── Raw_Data/
│   ├── DICOM_Files/
│   ├── NIfTI_Files/
│   └── Text_Data/
├── Processed_Data/
│   ├── Segmented_Images/
│   ├── Classification_Results/
│   └── NLP_Results/
├── Models/
│   ├── Segmentation_Models/
│   ├── Classification_Models/
│   └── Distilled_Models/
└── Reports/
    ├── Analysis_Reports/
    ├── Performance_Reports/
    └── Medical_Summaries/
```

### Data Import Guidelines

**DICOM Files**:
- Ensure files are properly anonymized
- Verify DICOM compliance
- Check file integrity before processing
- Maintain original file backups

**NIfTI Files**:
- Verify proper orientation and spacing
- Check for compression compatibility
- Ensure consistent coordinate systems
- Validate image dimensions

**Text Data**:
- Use UTF-8 encoding for Ukrainian text
- Ensure proper medical terminology
- Remove personal identifiers
- Maintain structured format

### Data Security

**Anonymization**:
- Always anonymize patient data before processing
- Use built-in anonymization tools
- Verify anonymization completeness
- Maintain anonymization logs

**Data Protection**:
- Store data in secure locations
- Use encrypted storage when possible
- Implement access controls
- Regular backup procedures

## Settings and Configuration

### General Settings

**Language Preferences**:
- Interface language (Ukrainian/English)
- Medical terminology language
- Report language settings

**Performance Settings**:
- Memory allocation limits
- Processing thread count
- GPU acceleration options
- Temporary file management

### Processing Configuration

**Segmentation Settings**:
- Default algorithm parameters
- Quality vs. speed preferences
- Output format preferences
- Batch processing options

**Classification Settings**:
- Confidence thresholds
- Model selection preferences
- Feature extraction options
- Result filtering settings

**NLP Configuration**:
- Ukrainian language models
- Medical vocabulary updates
- Processing pipeline options
- Output format preferences

### System Configuration

**File Paths**:
- Default import directories
- Output file locations
- Model storage paths
- Temporary file locations

**Logging and Monitoring**:
- Log level settings
- Performance monitoring
- Error reporting options
- Usage statistics collection

## Tips and Best Practices

### Performance Optimization

1. **Memory Management**:
   - Close unused files and results
   - Process large files in batches
   - Monitor system memory usage
   - Use appropriate image resolutions

2. **Processing Efficiency**:
   - Use batch processing for multiple files
   - Configure optimal thread counts
   - Enable GPU acceleration when available
   - Cache frequently used models

### Quality Assurance

1. **Data Validation**:
   - Always verify input data integrity
   - Check file formats and compatibility
   - Validate processing parameters
   - Review results before final export

2. **Result Verification**:
   - Compare results with known standards
   - Use multiple validation methods
   - Document processing parameters
   - Maintain processing logs

### Troubleshooting

1. **Common Issues**:
   - File format compatibility problems
   - Memory limitations with large files
   - Processing parameter optimization
   - Model loading failures

2. **Error Resolution**:
   - Check application logs for details
   - Verify system requirements
   - Update models and dependencies
   - Contact support with specific error messages

### Research Best Practices

1. **Documentation**:
   - Document all processing parameters
   - Maintain detailed processing logs
   - Record model versions and settings
   - Keep backup copies of original data

2. **Reproducibility**:
   - Use consistent processing workflows
   - Document all configuration changes
   - Maintain version control for models
   - Record system specifications

3. **Validation**:
   - Use multiple validation datasets
   - Compare with established benchmarks
   - Perform cross-validation studies
   - Document validation methodologies

## Advanced Features

### Custom Model Integration

The application supports integration of custom AI models:
- ONNX format model loading
- Custom preprocessing pipelines
- Model performance evaluation
- Integration with existing workflows

### API Integration

For advanced users, the application provides:
- Command-line interface options
- Batch processing scripts
- Integration with external tools
- Automated workflow execution

### Research Extensions

The application can be extended for specific research needs:
- Custom segmentation algorithms
- Specialized classification models
- Domain-specific NLP processing
- Custom report generation

## Support and Resources

### Documentation
- **Build Guide**: Detailed compilation and installation instructions
- **API Documentation**: Technical reference for developers
- **Troubleshooting Guide**: Solutions for common issues
- **Deployment Guide**: Production deployment information

### Sample Data
- Synthetic DICOM files for testing
- Sample NIfTI neuroimaging data
- Example Ukrainian medical text
- Processing result examples

### Community
- Research collaboration opportunities
- Feature request submissions
- Bug reporting procedures
- Academic partnership programs

---

**Important Reminder**: This software is designed for research purposes only and should not be used for clinical diagnosis or treatment decisions. Always follow appropriate medical data handling regulations and institutional guidelines when working with medical imaging data.