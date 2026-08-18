# Spoonbill AI Studio - Universal Lean Edition (V3)
### Platform Segmentasi Instans & Sensus Burung Sendok Muka Hitam (*Black-faced Spoonbill*)

**Spoonbill Studio Universal V3** adalah versi teroptimasi yang dirancang khusus untuk distribusi ke pengguna akhir (*client deployment*). Versi ini menghilangkan ketergantungan pustaka berat (*zero PyTorch/CUDA bloat*), memangkas penggunaan memori penyimpanan secara drastis ($>98\%$), namun **tetap mempertahankan bobot model dalam presisi penuh aslinya (*Full Standard Precision / Non-Quantized*)**.

Aplikasi ini dapat dijalankan secara instan lintas platform pada **Windows**, **macOS Intel (x86_64)**, **macOS Apple Silicon (M1/M2/M3/M4 ARM64)**, maupun **Linux**.

---

## 🌟 Keunggulan Utama Versi Universal (V3)

1. **Penyimpanan Sangat Ringan (*Zero Bloated Libraries*)**:
   * Menghilangkan pustaka PyTorch, Torchvision, dan CUDA Toolkit ($3.5\text{ GB}$).
   * Menggunakan runtime murni **ONNX Runtime** yang hanya berukuran $\sim35\text{ MB}$.
   * Bobot model tetap mempertahankan **Presisi Standar Penuh (*Full Precision FP32*)**:
     * `best_spoonbill_11l.onnx` ($105.6\text{ MB}$)
     * `best_spoonbill_11n.onnx` ($11.1\text{ MB}$)

2. **Dukungan Lintas Platform Universal**:
   * **Windows 10/11**: Akselerasi CPU multithreading AVX-512 / DirectML.
   * **macOS Apple Silicon (M1/M2/M3/M4)**: Akselerasi native ARM64 & Apple CoreML.
   * **macOS Intel (x86_64)**: Akselerasi native x86_64.
   * **Linux / Server**: Kompatibel penuh tanpa masalah dependensi GUI display.

3. **Antarmuka Studio Modern & Sangat Responsif**:
   * **Hardware-Accelerated Canvas**: Navigasi gambar resolusi tinggi ($2400\times1600$) dengan *Smooth Pan & Zoom* (roda mouse / trackpad gestures, tombol Fit to Screen, 1:1 Reset).
   * **Dual Controls**: Sinkronisasi dua arah antara **Slider visual** dan **Kotak Teks Angka**.
   * **Ikon Bantuan `?` Interaktif**: Penjelasan instan untuk *Tile Size*, *Overlap*, *Confidence*, dan *NMS IoU*.
   * **Dua Mode Inferensi**: Beralih instan antara **Mode SAHI (Tiled Window)** dan **Mode Direct (Full-Frame Single Pass)**.
   * **Layer Switcher**: Tombol kontrol untuk menyembunyikan/menampilkan masker poligon, kotak pembatas (*bounding box*), dan label skor.
   * **Export Suite**: Simpan gambar hasil anotasi resolusi tinggi dan unduh data sensus ke format **CSV**.

---

## 🚀 Cara Menjalankan Aplikasi (1-Klik)

### 🪟 Untuk Pengguna Windows:
Cukup klik ganda pada berkas:
```
start_windows.bat
```
*(Skrip otomatis memeriksa dependensi ringan dan membuka browser di `http://127.0.0.1:8080`)*

---

### 🍎 Untuk Pengguna macOS (Intel & Apple Silicon M1/M2/M3/M4):
1. Buka Terminal pada folder ini (atau klik ganda):
```bash
./start_mac.command
```
*(Jika muncul peringatan permission pada macOS, jalankan `chmod +x start_mac.command` sekali saja).*

---

### 🌐 Untuk Server / Terminal Manual:
```bash
pip install -r requirements_lean.txt
python app.py
```
Lalu buka browser di `http://127.0.0.1:8080`.

---

## 📁 Struktur Berkas `/Spoonbill_Studio_Universal`

```
Spoonbill_Studio_Universal/
├── start_windows.bat          # Launcher 1-klik untuk Windows
├── start_mac.command          # Launcher 1-klik untuk macOS (Intel & ARM64)
├── app.py                     # Backend server ultra-ringan (FastAPI)
├── engine_onnx.py             # Engine inferensi SAHI ONNX Runtime murni
├── requirements_lean.txt      # Daftar dependensi ringan (<60 MB)
├── README.md                  # Panduan penggunaan multi-platform
├── models/
│   ├── best_spoonbill_11l.onnx # Model YOLO11 Large Presisi Penuh (105.6 MB)
│   └── best_spoonbill_11n.onnx # Model YOLO11 Nano Presisi Penuh (11.1 MB)
└── static/
    ├── index.html             # Antarmuka web modern (Single Page App)
    ├── css/
    │   └── style.css          # Desain Dark Glassmorphism responsif
    └── js/
        └── app.js             # Logika interaktif Canvas, Pan/Zoom & REST API
```
