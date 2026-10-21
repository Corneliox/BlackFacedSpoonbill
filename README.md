# Black-Faced Spoonbill AI Detection & Instance Segmentation Project
### Automated Instance Segmentation, Attention-Guided Feature Extraction, and Demographic Census Platform for *Platalea minor*

[![Release v4.0.0](https://img.shields.io/badge/Release-v4.0.0-blue.svg)](https://github.com/Corneliox/BlackFacedSpoonbill/releases/tag/v4.0.0)
[![Live Web Demo](https://img.shields.io/badge/Web_Demo-GitHub_Pages_(Nano_Tier)-green.svg)](https://corneliox.github.io/BlackFacedSpoonbill/)
[![License](https://img.shields.io/badge/License-CC_BY--NC--SA_4.0-orange.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows_%7C_macOS_%7C_Linux-lightgrey.svg)](https://github.com/Corneliox/BlackFacedSpoonbill/releases)

---

## 1. Overview and Ecological Context

The **Black-faced Spoonbill (*Platalea minor*)** is an endangered migratory wading bird classified on the IUCN Red List. With major wintering populations congregating along the coastal mudflats and estuaries of Western and Southwestern Taiwan (notably the Chiku, Qigu, and Tainan wetland reserves), reliable demographic monitoring is critical to conservation efforts.

Automated computer vision surveys in wetland habitats face several technical hurdles:
- **Dense Clustering**: Birds frequently roost in closely packed flocks, causing standard bounding boxes to severely overlap.
- **High-Aspect Aerial Panoramas**: Wide-angle telephoto and drone surveys ($2400 \times 1600$ to $4000 \times 3000$ pixels) render distant subjects at scales under 30 pixels.
- **Fine-Grained Morphology**: Discriminating spoonbills from co-occurring ardeids (e.g., Little Egrets, Great Egrets, Grey Herons) requires precise contour segmentation of the diagnostic spatulate bill.

This repository provides an integrated suite spanning neural network training, coordinate attention ablation models, sliding-window tiled inference (SAHI), a client-side web evaluation interface, and a standalone cross-platform desktop studio application.

---

## 2. Interactive Demonstrations and Release Downloads

### A. Online Web Demonstration (Restricted Evaluation Tier)
Test the detection pipeline directly in your web browser:
- **URL**: [https://corneliox.github.io/BlackFacedSpoonbill/](https://corneliox.github.io/BlackFacedSpoonbill/)
- **Scope**: Runs 100% client-side via ONNX Runtime WebAssembly/WebGL using the **YOLO11 Nano (11N)** model.
- **Constraint**: To prevent excessive bandwidth and client compute overhead, high-resolution tiled inference (SAHI), batch multi-image processing, and large ablation models are restricted to the desktop software.

### B. Desktop Studio v4.0.0 (Native Release Binaries)
The native desktop application features the complete Photoshop-inspired analysis workstation, hardware auto-probing, dual-model comparison, and full-resolution panoramic census:

| Operating System | Distribution Format | Download Link |
| :--- | :--- | :--- |
| **Windows 10 / 11** | Setup Wizard (.exe) | [Download Setup Installer](https://github.com/Corneliox/BlackFacedSpoonbill/releases/download/v4.0.0/Spoonbill_Studio_v4.0.0_Setup.exe) |
| **Windows 10 / 11** | Standalone Portable (.zip) | [Download Portable ZIP](https://github.com/Corneliox/BlackFacedSpoonbill/releases/download/v4.0.0/Spoonbill_Studio_v4.0.0_Windows_x64.zip) |
| **Windows 10 / 11** | Single Direct Binary (.exe) | [Download Direct Binary](https://github.com/Corneliox/BlackFacedSpoonbill/releases/download/v4.0.0/Spoonbill_Studio_v4.0.0_DirectRun_Windows_x64.exe) |
| **macOS Apple Silicon** | Native DMG (M1, M2, M3, M4) | [Download Apple Silicon DMG](https://github.com/Corneliox/BlackFacedSpoonbill/releases/download/v4.0.0/Spoonbill_Studio_v4.0.0_macOS_AppleSilicon_ARM64.dmg) |
| **macOS Intel** | Native DMG (x86_64) | [Download Intel Mac DMG](https://github.com/Corneliox/BlackFacedSpoonbill/releases/download/v4.0.0/Spoonbill_Studio_v4.0.0_macOS_Intel_x64.dmg) |
| **Linux (Universal)** | Compressed Tarball (.tar.gz) | [Download Linux Tarball](https://github.com/Corneliox/BlackFacedSpoonbill/releases/download/v4.0.0/Spoonbill_Studio_v4.0.0_Linux_x64.tar.gz) |

Full release metadata and checksums are accessible at the [GitHub Releases Page](https://github.com/Corneliox/BlackFacedSpoonbill/releases/tag/v4.0.0).

---

## 3. System Architecture and Research Phases

```
BlackFacedSpoonbill/
|-- docs/                          # GitHub Pages static client-side web application
|   |-- index.html                 # WebAssembly inference interface (YOLO11 Nano tier)
|   |-- models/                    # Bundled ONNX Nano model weights (11.6 MB)
|   `-- samples/                   # Evaluation sample imagery
|-- Spoonbill_Electron_Studio/     # Desktop Studio Application (Photoshop Warehouse Edition)
|   |-- main.js                    # Electron runtime lifecycle and Python process supervision
|   |-- package_app.js             # Automated packaging script
|   |-- Setup.cs                   # C# native setup wizard compiler source
|   |-- backend/
|   |   |-- server.py              # FastAPI internal application server
|   |   `-- unified_engine.py      # Cross-platform execution engine (ONNX + PyTorch)
|   |-- renderer/                  # Professional dark studio user interface
|   `-- models/                    # Bundled FP32 ONNX model weights
|-- Yolov11_InsSeg/                # Phase 1: Baseline instance segmentation prototypes
|-- SAM/                           # Phase 2: Segment Anything Model zero-shot mask generation
|-- YOLOv12_BeakAttention/         # Phase 4: Multi-scale Coordinate Attention ablation suite
|-- App/                           # Universal cross-platform engine core
`-- .github/workflows/             # Multi-runner CI/CD matrix build automation
```

The system evolved across four structured engineering phases:

### Phase 1: Baseline Instance Segmentation (`Yolov11_InsSeg`)
Initial exploration using standard YOLO11 architectures trained on localized 640-pixel crops. Yielded functional instance detection but suffered from bounding box overlap on tightly huddled bird clusters.

### Phase 2: Foundation Model Annotation (`SAM`)
Integration of the Segment Anything Model (Meta AI) for interactive and zero-shot contour mask extraction. Enabled high-quality mask labeling along complex mudflat edges and water reflection boundaries.

### Phase 3: Sliced Aided Hyper Inference (`SAHI`)
Introduction of sliding-window tile slicing (640-pixel crops with 20% overlap) across 4K surveillance images. Employs Global Non-Maximum Suppression (NMS) to recombine cropped detections, elevating distant bird recovery by over 30%.

### Phase 4: Multi-Scale Coordinate Attention (`BeakAttention @ P3`)
Custom injection of Coordinate Attention modules into the high-resolution feature pyramid layer (P3) of the YOLO backbone. Encodes spatial horizontal and vertical positional dependencies, achieving an empirical **+19.4% breakthrough in box precision** on difficult roosting poses.

---

## 4. Quantitative Benchmarks and Evaluation Metrics

Models were trained and evaluated on wetland surveillance datasets compiled across diverse observation sites in Taiwan.

| Architecture / Scale | Attention Mechanism | Params | Best Epoch | Mask mAP50 | Mask mAP50-95 | Box Precision | Box mAP50 | Validation Loss |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **YOLO11L Production V3** | Standard Backbone | 27.68M | 50 | **0.9394** | **0.7812** | 0.8738 | 0.9410 | 1.1020 |
| **YOLO11N Production V3** | Standard Lightweight | 2.87M | 50 | 0.9199 | 0.7450 | 0.8383 | 0.9220 | 1.2480 |
| **BeakAttention-L (P3)** | CoordAtt @ P3 | 27.72M | 50 | 0.8622 | 0.6980 | **0.8775** | 0.8850 | 1.4120 |
| **Baseline-L** | None (Control) | 27.68M | 42 | 0.8510 | 0.6840 | 0.6830 | 0.8620 | 1.4890 |
| **BeakAttention-S (P3)** | CoordAtt @ P3 | 10.11M | 48 | 0.8942 | 0.7120 | 0.7956 | 0.9010 | 1.3050 |
| **Baseline-S** | None (Control) | 10.09M | 45 | 0.9284 | 0.7510 | 0.8200 | 0.9340 | 1.1870 |

Key Findings:
- In the Large scale comparison, injecting Coordinate Attention at P3 increased bounding box precision from **68.3% to 87.75% (+19.45% relative gain)**, effectively suppressing false positives triggered by water ripples and mirror reflections.
- The **YOLO11 Nano (11N)** model achieves 0.9199 Mask mAP50 with sub-60ms execution latency on standard multi-core CPUs.

---

## 5. Desktop Application Capabilities (Build 4.0.0)

The desktop application (`Spoonbill_Electron_Studio`) provides a zero-clutter workstation tailored for field biologists:
- **Deterministic CPU-First Safe Mode**: Guarantees instant startup without GPU initialization freezes.
- **Hardware Acceleration Inspector**: Real-time modal scanning discrete GPUs (NVIDIA, AMD, Intel, Apple Silicon MPS) and driver status before activating VRAM alocation.
- **Dynamic Guardrail Badges**: Clear visual alerts distinguishing low-latency CPU-recommended models from high-load GPU-recommended architectures.
- **Dual-Model Split Comparison**: Side-by-side inspection between baseline and attention-enhanced predictions.
- **Multi-Format Export**: Generates annotated JPEG imagery, population census summary CSVs, and standard YOLO segmentation coordinate text files.

---

## 6. Local Installation & Development

### Prerequisites
- Python 3.10, 3.11, or 3.12
- Node.js 18+ and npm (for desktop Electron studio packaging)
- Git

### Quickstart (Source Run)
```bash
# Clone the repository
git clone https://github.com/Corneliox/BlackFacedSpoonbill.git
cd BlackFacedSpoonbill

# Set up Python virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install core runtime dependencies
pip install -r App/requirements_lean.txt

# Launch Electron Desktop Studio
cd Spoonbill_Electron_Studio
npm install
npm start
```

---

## 7. Authors and Institutional Affiliations

- **Gilang Ramadhan**
  - Department of Computer Science and Information Engineering
  - Artificial Intelligence and Computer Vision Laboratory
  - Asia University, Taichung, Taiwan
  - GitHub: [@Corneliox](https://github.com/Corneliox) / [@ghilangtr](https://github.com/ghilangtr)

For full institutional affiliations and contributor profiles, refer to [AUTHORS.md](AUTHORS.md).

---

## 8. Citation

If you utilize this codebase, trained model checkpoints, or findings in academic or conservation publications, please cite using [CITATION.cff](CITATION.cff) or the following format:

```bibtex
@software{ramadhan2026spoonbill,
  author       = {Gilang Ramadhan},
  title        = {{Black-Faced Spoonbill AI Detection & Instance Segmentation Project}},
  year         = {2026},
  version      = {4.0.0},
  publisher    = {GitHub},
  institution  = {Asia University, Taiwan},
  url          = {https://github.com/Corneliox/BlackFacedSpoonbill}
}
```

---

## 9. License and Usage Policy

This project is licensed under the **Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License (CC BY-NC-SA 4.0)**.

- **Non-Commercial Research & Education**: Free to use, adapt, and distribute with proper academic attribution.
- **Commercial Restrictions**: Commercial exploitation, fee-based consulting, integration into proprietary paid software, or closed monetization is **strictly prohibited without prior written consent and a formal contribution agreement with the authors**.
- For licensing inquiries, refer to [LICENSE](LICENSE).
