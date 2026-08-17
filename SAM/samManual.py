import cv2
import torch
import numpy as np
import tkinter as tk
from tkinter import filedialog
import os
from segment_anything import sam_model_registry, SamPredictor

# --- 1. Fungsi Konversi Mask ke Poligon ---
def mask_to_yolo_polygons(mask):
    contours, _ = cv2.findContours(mask.astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_TC89_KCOS)
    polygons = []
    h, w = mask.shape
    for contour in contours:
        if len(contour) < 3: continue 
        polygon = contour.reshape(-1, 2).astype(float)
        polygon[:, 0] /= w
        polygon[:, 1] /= h
        polygons.append(polygon.flatten().tolist())
    return polygons

# --- 2. Setup Folder & Model ---
os.makedirs("dataset_yolo_v11/images", exist_ok=True)
os.makedirs("dataset_yolo_v11/labels", exist_ok=True)

sam_checkpoint = "sam_vit_h_4b8939.pth"
model_type = "vit_h"
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Menggunakan: {device}")

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)
predictor = SamPredictor(sam)

# --- 3. Pilih Banyak File ---
root = tk.Tk(); root.withdraw(); root.attributes('-topmost', True)
paths = filedialog.askopenfilenames(title="Pilih Banyak Gambar", filetypes=[("Images", "*.jpg *.jpeg *.png")])

if paths:
    print(f"Total gambar: {len(paths)}")
    
    # Variabel global sementara untuk menampung klik mouse
    current_points = []
    current_labels = []

    def mouse_click(event, x, y, flags, param):
        # Kita tidak perlu lagi 'nonlocal' karena list dimodifikasi secara in-place
        if event == cv2.EVENT_LBUTTONDOWN:
            current_points.append([x, y])
            current_labels.append(1)
            print(f"Titik: {x}, {y}")
        elif event == cv2.EVENT_RBUTTONDOWN:
            if current_points:
                current_points.pop()
                current_labels.pop()
                print("Titik terakhir dihapus")

    for idx, path in enumerate(paths):
        image = cv2.imread(path)
        if image is None: continue
        
        print(f"\n[{idx+1}/{len(paths)}] Memproses: {os.path.basename(path)}")
        image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        predictor.set_image(image_rgb)

        all_object_polygons = [] 
        current_points.clear() # Reset untuk gambar baru
        current_labels.clear()
        
        image_work = image.copy()
        win_name = f"SAM: {os.path.basename(path)}"
        cv2.namedWindow(win_name, cv2.WINDOW_NORMAL)
        cv2.resizeWindow(win_name, 1000, 700)
        cv2.setMouseCallback(win_name, mouse_click)

        while True:
            display_img = image_work.copy()
            for pt in current_points:
                cv2.circle(display_img, tuple(pt), 5, (0, 255, 0), -1)
            
            cv2.imshow(win_name, display_img)
            key = cv2.waitKey(1) & 0xFF
            
            if key == ord('s'): # Simpan Objek
                if current_points:
                    masks, scores, _ = predictor.predict(
                        point_coords=np.array(current_points),
                        point_labels=np.array(current_labels),
                        multimask_output=True
                    )
                    best_mask = masks[np.argmax(scores)]
                    polys = mask_to_yolo_polygons(best_mask)
                    all_object_polygons.extend(polys)
                    
                    # Tandai objek yang sudah selesai di layar
                    image_work[best_mask] = image_work[best_mask] * 0.5 + np.array([0, 255, 0], dtype=np.uint8) * 0.5
                    current_points.clear()
                    current_labels.clear()
                    print(f" Objek dikunci. Total poligon: {len(all_object_polygons)}")

            elif key == ord('r'):
                current_points.clear()
                current_labels.clear()
            
            elif key == ord('q'):
                break

        cv2.destroyWindow(win_name)

        if all_object_polygons:
            base_name = os.path.splitext(os.path.basename(path))[0]
            label_path = f"dataset_yolo_v11/labels/{base_name}.txt"
            with open(label_path, "w") as f:
                for poly in all_object_polygons:
                    # Format: class_id x1 y1 x2 y2 ...
                    line = "0 " + " ".join([f"{coord:.6f}" for coord in poly])
                    f.write(line + "\n")
            
            cv2.imwrite(f"dataset_yolo_v11/images/{os.path.basename(path)}", image)

    print("\nProses selesai!")
    cv2.destroyAllWindows()