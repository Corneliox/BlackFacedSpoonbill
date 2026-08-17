from ultralytics import YOLO

# Load model Anda
model = YOLO("C:/Users/user/Documents/Code/Yolov11_InsSeg/best.pt")

# Tampilkan daftar kelas
print(model.names)