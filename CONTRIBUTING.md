# Contributing Guidelines

Thank you for your interest in contributing to the Black-Faced Spoonbill AI Detection & Instance Segmentation Project. We welcome contributions from researchers, software engineers, and conservation biologists.

---

## 1. Licensing and Intellectual Property Terms

All contributions to this repository—including source code, neural network architectures, pre-trained weights, training scripts, annotations, and documentation—are governed by the **Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License (CC BY-NC-SA 4.0)**.

By submitting a pull request, patch, or issue, you agree that:
1. Your contribution is made under the non-commercial terms of this repository.
2. The work does not infringe on third-party proprietary rights or violate commercial software licenses.
3. Your work is intended for academic research, education, and ecological conservation purposes.

---

## 2. Types of Contributions Welcomed

We encourage contributions in the following domains:
- **Ecology & Datasets**: Additional verified, annotated imagery of *Platalea minor* across diverse weather conditions, lighting angles, and wetland backgrounds.
- **Model Architecture & Benchmarking**: Architectural refinements to attention mechanisms, loss functions for overlapping instances, or novel post-processing NMS strategies.
- **Cross-Platform Software Engineering**: Improvements to desktop stability, Electron lifecycle handling, ONNX Runtime hardware acceleration, or packaging workflows.
- **Documentation & Localization**: Translation of technical manuals, setup guides, and scientific abstracts.

---

## 3. Contribution Workflow

### Step 1: Open an Issue First
Before embarking on significant architectural modifications, open a GitHub Issue describing:
- The proposed improvement or bug fix.
- The rationale and expected performance or usability impact.
- Any new dependencies or hardware prerequisites.

### Step 2: Create a Feature Branch
Fork the repository and branch from `master`:
```bash
git checkout -b feature/your-feature-name
```

### Step 3: Code and Engineering Standards
- **Python Code**: Follow PEP 8 guidelines. Maintain modularity and avoid coupling model inference logic to specific GUI toolkits.
- **Frontend / Electron Code**: Ensure zero external runtime dependencies that require active internet connections; packaged applications must operate offline in remote field settings.
- **Deterministic Reproducibility**: When introducing new training pipelines, ensure random seeds, dataset splits, and hyperparameters are explicitly logged.

### Step 4: Submit a Pull Request
- Provide a clear, factual description of the changes made.
- Reference any relevant Issue numbers.
- Ensure all existing features and builds remain functional across Windows, macOS, and Linux targets.

---

## 4. Code of Conduct

All contributors and participants are expected to adhere to the project's [Code of Conduct](CODE_OF_CONDUCT.md). Professional, constructive, and respectful collaboration is strictly enforced.
