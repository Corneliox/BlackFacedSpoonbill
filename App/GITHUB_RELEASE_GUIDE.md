# Panduan Publikasi & Instalasi via GitHub Release (Seperti GitHub Desktop)

Panduan ini menjelaskan cara membagikan aplikasi **Spoonbill AI Studio** kepada pengguna/klien melalui **GitHub Repository & GitHub Releases** sehingga mereka bisa langsung mengunduh dan memasangnya (*1-Click Install*) ke komputer mereka tanpa perlu repot mengetik perintah terminal.

---

## 📦 Alur Pengguna Akhir (Cara Klien Menginstal dari GitHub)

Pengguna/Klien hanya perlu melakukan 3 langkah mudah:

```
1. Download file ZIP dari tab 'Releases' di GitHub (contoh: Spoonbill_Studio_Universal_v3.0.zip)
                          │
                          ▼
2. Ekstrak file ZIP ke komputer mereka (di folder mana saja)
                          │
                          ▼
3. Klik ganda 'Install_Desktop_App.bat' (Windows) atau 'Install_Mac_App.command' (Mac)
                          │
                          ▼
4. SELESAI! Ikon aplikasi 'Spoonbill AI Studio' otomatis muncul di Desktop mereka.
```

---

## 🚀 Cara Anda Mempublikasikan ke GitHub (Langkah Developer)

### 1. Inisialisasi & Push ke GitHub
Buka terminal pada folder `Spoonbill_Studio_Universal`:
```bash
git init
git add .
git commit -m "feat: initial release of Spoonbill AI Studio Universal V3"
git branch -M main
git remote add origin https://github.com/<username-anda>/Spoonbill-AI-Studio.git
git push -u origin main
```

### 2. Membuat Rilis Siap Pakai (*GitHub Release Assets*)
1. Kompres seluruh isi folder `Spoonbill_Studio_Universal` menjadi file `.zip` (misal: `Spoonbill_AI_Studio_v3.0.0_Portable.zip`).
2. Di halaman GitHub Anda, klik menu **Releases** $\longrightarrow$ **Draft a new release**.
3. Masukkan Tag Version: `v3.0.0` dan Release Title: `Spoonbill AI Studio Universal Edition v3.0.0`.
4. Unggah file `Spoonbill_AI_Studio_v3.0.0_Portable.zip` ke kotak *Attach binaries*.
5. Klik **Publish release**.

---

## 🌟 Mengapa Format Ini Sangat Praktis Bagi Klien?

1. **Pengalaman Seperti Aplikasi Desktop Native**:
   * Membuka jendela aplikasi desktop mandiri (*Dedicated Native Window*) tanpa bilah URL browser.
   * Ikon pintasan (*shortcut*) otomatis terpasang di Desktop layar utama pengguna.
2. **Tidak Memerlukan Server Internet**:
   * Model ONNX dan inferensi berjalan secara *offline* langsung pada CPU/GPU laptop klien.
3. **Kompatibilitas 100% Lintas-Platform**:
   * Paket yang sama dapat langsung digunakan oleh tim yang memakai Windows maupun tim yang memakai Mac (Intel dan Apple Silicon M1-M4).
