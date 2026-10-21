# Release Notes: Spoonbill AI Studio v4.1.0-experimental (Non-Destructive HUD & Unified Studio)

## 1. Executive Summary

Spoonbill AI Studio version 4.1.0-experimental represents a major UX and architectural enhancement following the stabilized v4.0.0 multi-platform release. This update decouples ecological census telemetry from underlying image pixels, introducing a **Non-Destructive Floating Info Label (HUD)**, a dedicated **Info Label Customization Tab**, custom sleek dark scrollbars and range sliders across the entire workstation and Windows installer, an 85% reduced macOS Apple Silicon distribution package (99.59 MB), and automated Gatekeeper bypass tooling for macOS Sequoia/Sonoma on Apple Silicon M-series laptops.

---

## 2. Key Capabilities & Architectural Advancements

### A. Non-Destructive Floating Info Label (HUD)
- **Decoupled from Raster Imagery**: In previous builds, summary detection boxes (`Model | SAHI`, `Detected Count`, `Latency`, `Confidence`, `Resolution`) were permanently burned into the image array by OpenCV. In v4.1.0, the canvas stays 100% clean and non-destructive.
- **Dual Rendering Modes**:
  - **Floating Viewport Overlay (Draggable)**: Retains crisp resolution independent of canvas zoom levels and can be freely repositioned or snapped to any corner.
  - **Pinned on Image Canvas (Zoomable)**: Pinned directly to image coordinate space to zoom and pan alongside high-resolution aerial imagery without modifying pixels.
- **Visual Pulse Confirmation**: Opening the Info Label tab, resetting its position, or toggling visibility triggers a smooth orange glow pulse animation (`@keyframes hud-glow-pulse`) to immediately locate the HUD on screen.
- **Granular Field Control**: Real-time toggles for model name, SAHI mode, detected count, tile count, latency, confidence, image dimensions, and custom location notes without re-running neural inference.

### B. Sleek Dark Studio Scrollbars & Custom Sliders
- **Global Dark Scrollbars**: All scrollable viewports (Inspector, Benchmark Ablation Matrix, Instance List, Filmstrip) now utilize custom 6px sleek dark tracks (`rgba(18, 18, 22, 0.65)`) with rounded thumbs that glow orange on hover, replacing native operating system scrollbars.
- **Custom Range Sliders**: Slider controls for Confidence, Tile Size, Overlap, IoU, Opacity, and Scale feature 5px dark tracks (`#27272A`) with 14px orange accent thumbs that expand to 1.25x scale on hover.
- **NVIDIA-Styled Installer Scrollbar**: The Windows Setup wizard (`Setup.cs`) replaces legacy WinForms 3D grey `AutoScroll` with a custom-painted 6px dark scrollbar supporting mouse-wheel scrolling and thumb dragging.

### C. macOS Apple Silicon (ARM64) Optimization
- **85% Size Reduction**: Eliminated multipart model redundancy and utilized `ditto` to preserve `.app/Contents/Frameworks` symlinks, reducing the Apple Silicon DMG from 663 MB to **99.59 MB**.
- **Instant CPU Fast Mode (ARM64 NEON)**: Configured ONNX Runtime `CPUExecutionProvider` on Apple Silicon by default (<0.15s startup, ~25-35ms inference per image), avoiding dynamic-graph reshaping stalls under CoreML.
- **Gatekeeper Bypass Tooling**: Bundled with [PETUNJUK_CARA_BUKA_MAC.txt](App/PETUNJUK_CARA_BUKA_MAC.txt), [README_MAC_USERS.txt](App/README_MAC_USERS.txt), and [Unlock_and_Open_Mac.command](App/Unlock_and_Open_Mac.command) for one-click quarantine removal (`xattr -cr`) on macOS Sequoia and Sonoma.

---

## 3. Platform Compatibility & Distribution Artifacts

| Platform / Architecture | Artifact Name | Description |
| :--- | :--- | :--- |
| **Windows 10 / 11 (Installer)** | `Spoonbill_Studio_v4.1.0_Experimental_Setup.exe` | Native C# NVIDIA-style wizard with dark custom scrollbars, system readiness checks, and uninstaller. |
| **Windows 10 / 11 (Portable)** | `Spoonbill_Studio_v4.1.0_Experimental_Windows_x64.zip` | Standalone portable archive for USB or lab environments. |
| **macOS Apple Silicon (ARM64)** | `Spoonbill_Studio_v4.1.0_Experimental_macOS_AppleSilicon_ARM64.dmg` | Slim signed disk image (99.59 MB) optimized for M1, M2, M3, and M4 Apple Silicon Macs. |
| **macOS Intel (x64)** | `Spoonbill_Studio_v4.1.0_Experimental_macOS_Intel_x64.dmg` | Native signed disk image for legacy Intel Macs. |

---

## 4. Citation and Attribution

When citing this work or software in research publications, please refer to [CITATION.cff](CITATION.cff) or format as:

```bibtex
@software{ramadhan2026spoonbill,
  author       = {Gilang Ramadhan},
  title        = {{Black-Faced Spoonbill AI Detection \& Instance Segmentation Project: Automated Census and Attention-Guided Instance Segmentation for Platalea minor}},
  year         = {2026},
  version      = {4.1.0},
  publisher    = {GitHub},
  institution  = {Department of Computer Science and Information Engineering, Asia University, Taiwan},
  url          = {https://github.com/Corneliox/BlackFacedSpoonbill}
}
```
