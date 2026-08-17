import torch
from segment_anything import sam_model_registry, SamPredictor

# Cek apakah GPU terdeteksi
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Menggunakan perangkat: {device}")

# Load model (pastikan file .pth ada di folder yang sama)
sam_checkpoint = "sam_vit_h_4b8939.pth"
model_type = "vit_h"

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)
predictor = SamPredictor(sam)

print("SAM siap digunakan!")