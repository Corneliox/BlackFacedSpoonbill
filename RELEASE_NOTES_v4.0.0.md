# Release Notes: Spoonbill AI Studio v4.0.0 (Universal Multi-Platform Edition)

## 1. Executive Summary

Spoonbill AI Studio version 4.0.0 is a major release of the computer vision platform designed for automated detection, instance segmentation, and census of the endangered Black-faced Spoonbill (*Platalea minor*). 

This release resolves initialization freezes on consumer laptops lacking discrete GPUs by transitioning the backend architecture to a deterministic **CPU-First Safe Mode**. In addition, it introduces an in-app hardware inspection engine, an ultra-compact workstation UI modeled after professional creative software, and automated CI/CD distribution matrices targeting Windows 10/11, macOS (both Apple Silicon and Intel), and Linux.

---

## 2. Technical Architecture and Key Capabilities

### A. Deterministic CPU-First Safe Mode
- **Zero-Dependency Startup**: The execution engine defaults to CPU execution via ONNX Runtime (`CPUExecutionProvider`). This eliminates runtime crashes or hangs caused by unconfigured CUDA libraries or missing GPU drivers.
- **Optimized CPU Target**: The high-speed **YOLO11 Nano (11N ONNX)** model is pre-selected by default, delivering sub-60ms inference latency without thermal throttling or heavy battery drain.

### B. Hardware Acceleration Inspector and Guardrails
- **Real-Time Environment Probing**: Integrated hardware diagnostics scan the operating system, processor core counts, physical display controllers (NVIDIA GeForce/RTX, AMD Radeon, Intel Iris/Arc, and Apple Silicon unified memory), and PyTorch CUDA environment status.
- **Validated GPU Mode Switching**: The NVIDIA GPU mode toggle requires explicit hardware and runtime verification before shifting model weights into VRAM. If unsupported hardware is detected, the switch is safely prevented.
- **Dynamic Workload Warnings**: Production models scaled to Medium (11M) and Large (11L / BeakAttn) feature explicit visual warnings when selected in CPU mode, alerting researchers to potential latency overhead.

### C. Streamlined Creative Workspace
- **High-Performance Canvas**: Supports pan and zoom with full-resolution rendering of aerial panoramic imagery.
- **Integrated Filmstrip Gallery**: Enables one-click validation across curated habitat test datasets.
- **Advanced Diagnostic Views**: Features dual-model split comparison, Coordinate Attention activation mapping (Grad-CAM), and multi-format export pipelines (annotated imagery, population CSVs, and YOLO-compatible bounding annotations).

---

## 3. Platform Compatibility and Release Artifacts

All distribution packages are self-contained and available directly from the release repository:

| Target Platform & Architecture | Artifact Name | Description |
| :--- | :--- | :--- |
| **Windows 10 / 11 (64-bit)** | `Spoonbill_Studio_v4.0.0_Setup.exe` | Self-extracting setup wizard; installs to `%LOCALAPPDATA%` and generates Desktop / Start Menu shortcuts. |
| **Windows 10 / 11 (Portable)** | `Spoonbill_Studio_v4.0.0_Windows_x64.zip` | Standalone portable archive; requires no administrative privileges. |
| **Windows 10 / 11 (Single Binary)** | `Spoonbill_Studio_v4.0.0_DirectRun_Windows_x64.exe` | Single-file executable distribution. |
| **macOS Apple Silicon (ARM64)** | `Spoonbill_Studio_v4.0.0_macOS_AppleSilicon_ARM64.dmg` | Native Apple disk image compiled for Apple M1, M2, M3, and M4 processors utilizing Metal Performance Shaders (MPS). |
| **macOS Intel (x64)** | `Spoonbill_Studio_v4.0.0_macOS_Intel_x64.dmg` | Native Apple disk image compiled for legacy Intel x86_64 Mac systems. |
| **Linux (Universal x86_64)** | `Spoonbill_Studio_v4.0.0_Linux_x64.tar.gz` | Compressed tarball distribution for Ubuntu, Debian, Fedora, and Arch Linux environments. |

---

## 4. Operation Guidelines

1. **Standard Deployment (CPU-Only, Integrated Intel, or AMD APU)**:
   Launch the application. It immediately enters CPU Safe Mode with the lightweight YOLO11 Nano model active. Standard aerial surveillance images can be processed without latency penalties.

2. **Workstation Deployment (Discrete NVIDIA GeForce / RTX GPU)**:
   Click the hardware status pill in the top-right header, then select **Activate NVIDIA CUDA Mode**. Once hardware verification completes, the system unbinds CPU mode and activates high-accuracy YOLO11 Large and BeakAttn-L inference engines.
