from ultralytics import YOLO

if __name__ == "__main__":
    model = YOLO("yolo11n-seg.pt") 

    # JALANKAN TRAINING
    model.train(
        data="dataset_custom.yaml", 
        imgsz=640,      # Mengecilkan dari 640 ke 416 mempercepat training di CPU secara drastis
        batch=4,        # Batch kecil lebih bersahabat dengan RAM laptop
        epochs=50,      # Mulai dengan 50 dulu untuk melihat progress
        device='cpu',   
        amp=False,      
        workers=2,      # Ryzen 7 kuat menangani 2-4 workers untuk mempercepat load data
        patience=10     # Berhenti otomatis jika tidak ada peningkatan dalam 10 epoch
    )