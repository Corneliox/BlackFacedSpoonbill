# Spoonbill AI Studio — Electron Desktop Edition (V4)
### Aplikasi Desktop Terpadu Deteksi, Segmentasi Instans, Ablasi Coordinate Attention (`CoordAtt @ P3`), Grad-CAM & Sensus Otomatis *Platalea minor*

**Spoonbill Electron Studio V4** mengintegrasikan seluruh modul riset dan kode program yang telah dikembangkan dari **Fase 1 hingga Fase 4** ke dalam satu aplikasi desktop berbasis **Electron** yang simpel, responsif, dan ramah pengguna (*UI/UX Friendly*).

---

## 🌟 Fitur Utama & Modul yang Diintegrasikan

1. **11 Model AI Lintas Generasi dalam Satu Aplikasi (`backend/unified_engine.py`)**:
   * **Phase 2 — Production V3 (ONNX Runtime)**: `YOLO11 Large V3 (ONNX)` & `YOLO11 Nano V3 (ONNX)` tanpa ketergantungan GPU.
   * **Phase 4 — Full Multi-Scale Ablation Suite (PyTorch CUDA/CPU + Custom `CoordAtt @ P3`)**:
     * `Baseline_N` & `BeakAttn_N` (Skala Nano — Rekomendasi Edge/Drone terbaik, Mask mAP50 **94.50%**)
     * `Baseline_S` & `BeakAttn_S` (Skala Small — Mask Recall **100%**)
     * `Baseline_M` & `BeakAttn_M` (Skala Medium — Puncak Mask mAP50 **95.13%**)
     * `Baseline_L` & `BeakAttn_L` (Skala Large — Lonjakan Box Precision **+19.38%** untuk menekan *false-positive* pantulan air rawa & burung kuntul)
   * **Phase 1 — Legacy Prototype**: `Legacy YOLO11n (V1)`.

2. **Preset Skenario 1-Klik & Galeri Contoh Cepat**:
   * **🦅 Panorama Koloni**: Otomatis mengaktifkan `11L ONNX` + `SAHI Tiling 640px` + `Overlap 20%`.
   * **🛡️ Anti-False Positive**: Otomatis mengaktifkan `BeakAttn-L (CoordAtt @ P3)` untuk memfilter pantulan air rawa dan burung air putih lainnya.
   * **⚡ Cepat / Close-Up**: Mode `Direct Single-Pass` dengan model ringan Nano.
   * **Galeri Contoh Cepat**: Uji langsung 6 foto representatif dari folder `Dataset/` hanya dengan 1 klik tanpa perlu mencari berkas di File Explorer.

3. **4 Ruang Kerja Terpadu (Progressive Disclosure UI)**:
   * **🖼️ 1. Studio Deteksi**: Kanvas interaktif (Pan, Zoom, Fit, 1:1), sakelar layer (*Polygon Masks*, *Bounding Boxes*, *Labels*), tabel *Instance Inspector* dengan fitur **Klik-untuk-Zoom ke Burung**, serta ekspor 1-klik ke **JPG**, **CSV**, dan **Label Poligon YOLO (`.txt`)**.
   * **⚔️ 2. Komparasi & Grad-CAM Lab**: Bandingkan dua model secara *Head-to-Head* (*Model A vs Model B*) baik dalam mode **Segmentasi Instans** maupun **Grad-CAM Spatial Attention Heatmap** (melihat fokus aktivasi *Coordinate Attention* pada paruh spatula).
   * **📂 3. Sensus Batch Otomatis**: Unggah banyak foto sekaligus untuk dihitung populasinya secara otomatis, lengkap dengan unduhan arsip **ZIP** (berisi foto beranotasi, CSV ringkasan, CSV detail instans, dan label poligon YOLO `.txt`) serta tombol native Electron **Buka Folder di Explorer**.
   * **📊 4. Benchmark & Ablasi Riset (13 Model)**: Tabel interaktif yang membaca `Comprehensive_Model_Evolution_Benchmark.csv` beserta galeri *Lightbox* 4 grafik publikasi IEEE (`Figure 1` – `Figure 4`).

---

## 🚀 Cara Menjalankan Aplikasi

Klik ganda pada berkas peluncur di direktori utama:
```bat
Launch_Spoonbill_Electron_Studio.bat
```
Atau jalankan melalui terminal dari dalam folder `Spoonbill_Electron_Studio`:
```powershell
npm start
```
