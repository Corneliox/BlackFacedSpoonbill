# Black-Faced Spoonbill AI Detection & Instance Segmentation Project
### Sistem Pendeteksi, Segmentasi Instans & Penghitung Populasi Burung Sendok Muka Hitam (*Platalea minor*)

Repositori ini berisi seluruh alur riset, kode sumber, dataset, model bobot *deep learning*, dan aplikasi grafis desktop mandiri yang dikembangkan untuk mendeteksi dan mensegmentasi burung langka **Black-faced Spoonbill** (Burung Sendok Muka Hitam) di habitat perairan/pesisir Taiwan.

---

## 📑 Daftar Isi
1. [Latar Belakang & Arsitektur Sistem](#-latar-belakang--arsitektur-sistem)
2. [Struktur Direktori Repositori](#-struktur-direktori-repositori)
3. [Rangkuman Model & Bobot (.pt)](#-rangkuman-model--bobot-pt)
4. [Evolusi Modul & Pipeline](#-evolusi-modul--pipeline)
   * [1. Modul YOLOv11 Baseline (Yolov11_InsSeg)](#1-modul-yolov11-baseline-yolov11_insseg)
   * [2. Modul Segment Anything Model (SAM)](#2-modul-segment-anything-model-sam)
   * [3. Modul SAHI V1 & V2 (Tiled Instance Segmentation)](#3-modul-sahi-v1--v2-tiled-instance-segmentation)
5. [Panduan Penggunaan & Eksekusi](#-panduan-penggunaan--eksekusi)
6. [Metrik Performa & Hasil Evaluasi](#-metrik-performa--hasil-evaluasi)

---

## 🔬 Latar Belakang & Arsitektur Sistem

Burung Sendok Muka Hitam (*Platalea minor*) merupakan spesies burung air langka yang memiliki ciri khas berupa paruh berbentuk spatula berwarna hitam pekat. Tantangan utama dalam visi komputer untuk spesies ini meliputi:
* **Gerombolan Burung yang Berhimpitan**: Burung sering berdiri rapat di atas air sehingga kotak pembatas (*bounding box*) standar saling menumpuk.
* **Resolusi Citra Ekstrem & Panorama Luas**: Citra drone atau kamera tele sering berukuran $2400\times1600$ hingga $4000\times3000$ piksel, di mana burung di kejauhan hanya berukuran puluhan piksel.
* **Detail Kunci Paruh Sendok**: Diperlukan segmentasi tingkat piksel (*instance mask*) agar paruh hitam sendok dapat dibedakan dari burung air lainnya seperti Kuntul Putih (*Egret*) atau Cangak (*Heron*).

Sistem ini memadukan **YOLOv11 Instance Segmentation**, **SAM (Segment Anything Model)**, dan **SAHI (Sliced Aided Hyper Inference)** untuk menghasilkan segmentasi beresolusi penuh tanpa kehilangan detail fitur penting.

---

## 📁 Struktur Direktori Repositori

```
BlackFacedSpoonbill/
├── README.md                  # Dokumentasi utama repositori ini
├── .gitignore                 # Konfigurasi pengabaian file Git
│
├── Yolov11_InsSeg/            # [Modul 1] Pelatihan & Inferensi Dasar YOLOv11
│   ├── Train.py               # Skrip pelatihan YOLOv11 (CPU/GPU)
│   ├── Predict.py             # GUI inferensi menggunakan model dasar COCO
│   ├── PredictBest.py         # GUI inferensi menggunakan model kustom best.pt
│   ├── cek.py                 # Utilitas pemeriksa nama kelas model
│   ├── Classes.txt            # Definisi kelas: Black-faced spoonbill
│   ├── dataset_custom.yaml    # Konfigurasi path dataset latih & validasi
│   ├── best.pt                # Bobot model terbaik awal (Nano Instance Seg)
│   ├── yolo11n-seg.pt         # Pretrained base weights Nano Instance Seg
│   ├── yolo11l-seg.pt         # Pretrained base weights Large Instance Seg
│   ├── Train/                 # Dataset gambar & label segmentasi lokal (Latih)
│   ├── Val/                   # Dataset gambar & label segmentasi lokal (Validasi)
│   ├── ObjDet/                # Dataset versi kotak pembatas (Bounding Box)
│   ├── runs/segment/train/    # Hasil pelatihan 50 epoch (loss, kurva mAP, confusion matrix)
│   └── dist/PredictBest/      # Aplikasi Windows Executable PredictBest.exe
│
├── SAM/                       # [Modul 2] Segment Anything Model (Zero-shot / Interactive)
│   ├── cobasam.py             # Skrip uji coba fondasi SAM
│   ├── samManual.py           # GUI anotasi / segmentasi interaktif berbasis SAM
│   └── dataset_yolo_v11/      # Dataset anotasi yang dihasilkan modul SAM
│
└── SAHI/                      # [Modul 3] Sliced Aided Hyper Inference V1
    ├── sahi_tiled_engine.py   # Core sliding window slicer & coordinate stitcher
    ├── run_pipeline.py        # Skrip batch processing folder dataset
    ├── gui_app.py             # Antarmuka desktop interaktif V1
    ├── requirements.txt       # Daftar dependensi pustaka
    └── runs/sahi_results/     # Output visualisasi inferensi ubin
```

---

## 🧠 Rangkuman Model & Bobot (.pt)

| Model | Ukuran | Arsitektur | Kegunaan |
| :--- | :---: | :---: | :--- |
| **`best_spoonbill_11l.pt`** | ~55.8 MB | YOLO11 Large ($27.6$M param) | **Model Akurasi Tertinggi**. Dilatih dengan data gabungan + Roboflow v3 di GPU Kaggle T4 $\times$ 2. Sangat kuat untuk memisahkan gerombolan burung dan segmentasi paruh sendok. |
| **`best_spoonbill_11n.pt` / `best.pt`** | ~6.0 MB | YOLO11 Nano ($2.84$M param) | **Model Cepat & Hemat Daya**. Sangat responsif (<100 ms), cocok untuk laptop tanpa GPU atau komputasi ringan. |
| **`yolo11n-seg.pt`** | ~6.18 MB | YOLO11 Nano Base | Bobot awal resmi Ultralytics COCO (Instance Segmentation). |
| **`yolo11l-seg.pt`** | ~56.1 MB | YOLO11 Large Base | Bobot awal resmi Ultralytics COCO Large. |

---

## 🚀 Evolusi Modul & Pipeline

### 1. Modul YOLOv11 Baseline (`Yolov11_InsSeg`)
* **Pelatihan Lokal 50 Epoch**: Model Nano dilatih pada citra lokal $640\times640$ piksel dan mencapai skor **mAP50 > 95%**.
* **GUI Pemilih Berkas**: `PredictBest.py` memanfaatkan `tkinter` untuk memudahkan pengguna memilih gambar dan otomatis menyimpan hasil segmentasi.
* **Kompilasi `.exe`**: Telah dikompilasi menjadi `PredictBest.exe` via PyInstaller.

### 2. Modul Segment Anything Model (`SAM`)
* Menyediakan fungsionalitas segmentasi berbasis model fondasi *Segment Anything* (Meta AI) untuk menghasilkan masker kontur halus dengan *prompting* titik (*points*) atau kotak pembatas (*boxes*).

### 3. Modul SAHI (Sliced Aided Hyper Inference) — Solusi Objek Kecil & Panorama
* **Cara Kerja**: Memotong citra beresolusi tinggi (misal $2400\times1600$) menjadi ubin-ubin (*tiles*) $640\times640$ dengan *overlap* $20\%$, menjalankan inferensi AI pada setiap ubin secara paralel pada GPU, lalu menata ulang koordinat poligon masker dan kotak deteksi ke skala global citra asli menggunakan **Global NMS**.
* **Versi Lanjutan (SAHI V2)**:
  * Dilengkapi studio desktop GUI lengkap dengan pemilih model (*Large 11L* vs *Nano 11N*).
  * Pengaturan parameter ganda (*Slider* visual + *Kotak Teks Numerik* tersinkronisasi).
  * Dilengkapi ikon bantuan interaktif **❓** pada setiap hyperparameter.
  * Aplikasi mandiri Windows `.exe` siap pakai di `SAHI_V2/dist/Spoonbill_SAHI_Studio_V2/`.

---

## 🛠️ Panduan Penggunaan & Eksekusi

### 1. Menjalankan Aplikasi GUI Standalone (.exe)
Buka folder `SAHI_V2/` dan klik ganda pada:
```
Launch_SAHI_V2_Studio.bat
```
*(Atau jalankan `SAHI_V2/dist/Spoonbill_SAHI_Studio_V2/Spoonbill_SAHI_Studio_V2.exe`)*

### 2. Menjalankan Training Ulang di Komputer Lokal
```powershell
python "Yolov11_InsSeg/Train.py"
```

### 3. Menjalankan Inferensi Batch pada Dataset
```powershell
python "SAHI/run_pipeline.py" --source "Dataset" --output "SAHI/runs/sahi_results"
```

---

## 📈 Metrik Performa & Hasil Evaluasi

* **Model 11N (Nano)**:
  * Mask mAP50: **95.5%**
  * Precision: **99.67%** | Recall: **95.24%**
  * Latensi GPU: **~40 – 150 ms**
* **Model 11L (Large Reinforced)**:
  * Mask mAP50: **> 96.5%**
  * Deteksi Objek Kejauhan pada Panorama $2400\times1440$: Mampu mendeteksi **42 Burung**, menangkap $10$ burung ekstra di kejauhan yang tidak terdeteksi oleh model standar.
  * Latensi GPU: **~300 – 1200 ms** (dengan 15 ubin beresolusi penuh).
