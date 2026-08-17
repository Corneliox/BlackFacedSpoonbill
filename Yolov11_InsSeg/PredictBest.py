import tkinter as tk
from tkinter import filedialog
from ultralytics import YOLO
import os

# 1. Setup agar jendela utama tkinter tidak muncul
root = tk.Tk()
root.withdraw() 
root.attributes('-topmost', True) # Agar jendela pilih file muncul di paling depan

# 2. Buka dialog untuk memilih file gambar
# Anda bisa memilih satu atau banyak file sekaligus
file_paths = filedialog.askopenfilenames(
    title="Upload Images",
    filetypes=[("Image files", "*.jpg *.jpeg *.png *.bmp *.webp")]
)

if file_paths:
    # Load model
    model_path = os.path.join(os.path.dirname(__file__), "best.pt")
    model = YOLO(model_path)

    # 3. Jalankan prediksi pada file yang dipilih
    # file_paths berbentuk list, YOLO bisa langsung memprosesnya
    model.predict(
        source=list(file_paths), 
        show=False, 
        save=True, 
        # conf=0.75, 
        classes=[0]
    )
    print(f"Finish processing image {len(file_paths)} .")
else:
    print("No files chosen.")