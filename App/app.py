"""
FastAPI Backend Server for Spoonbill Studio Universal.
Provides REST API endpoints for ONNX SAHI & Direct inference and serves the web interface.
"""

import os
import sys
import io
import base64
import time
from typing import Optional
import cv2
import numpy as np
from fastapi import FastAPI, File, UploadFile, Form, HTTPException
from fastapi.responses import HTMLResponse, JSONResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
from engine_onnx import SpoonbillOnnxEngine, get_base_dir

app = FastAPI(
    title="Spoonbill Studio Universal API",
    description="Lean Cross-Platform API for Black-faced Spoonbill SAHI Segmentation",
    version="3.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

BASE_DIR = get_base_dir()
STATIC_DIR = os.path.join(BASE_DIR, "static")
MODELS_DIR = os.path.join(BASE_DIR, "models")

# Mount static folder
if os.path.exists(STATIC_DIR):
    app.mount("/static", StaticFiles(directory=STATIC_DIR), name="static")

# Initialize Engine globally
engine = SpoonbillOnnxEngine(model_type="11l", models_dir=MODELS_DIR)


@app.get("/", response_class=HTMLResponse)
async def serve_index():
    index_path = os.path.join(STATIC_DIR, "index.html")
    if os.path.exists(index_path):
        return FileResponse(index_path)
    return HTMLResponse("<h1>Spoonbill Studio Universal API is Running.</h1>")


@app.get("/api/health")
async def health_check():
    return {
        "status": "healthy",
        "active_model": engine.active_model_type.upper(),
        "providers": engine.providers,
        "base_dir": BASE_DIR
    }


@app.get("/api/models")
async def get_models():
    return {
        "models": [
            {
                "id": "11l",
                "name": "YOLO11 Large (11L) - High Precision",
                "parameters": "27.6M",
                "size_mb": 105.6,
                "recommended_for": "Crowded flocks, distant small birds, ultra-high-res panoramas"
            },
            {
                "id": "11n",
                "name": "YOLO11 Nano (11N) - Ultra Fast",
                "parameters": "2.8M",
                "size_mb": 11.1,
                "recommended_for": "Fast laptop/mobile inference, low power usage"
            }
        ],
        "active_model": engine.active_model_type
    }


@app.post("/api/predict")
async def predict_image(
    file: UploadFile = File(...),
    model_type: str = Form("11l"),
    use_sahi: bool = Form(True),
    slice_size: int = Form(640),
    overlap: float = Form(0.20),
    conf: float = Form(0.25),
    iou: float = Form(0.45),
    draw_boxes: bool = Form(True),
    draw_masks: bool = Form(True),
    draw_labels: bool = Form(True)
):
    try:
        # Read file bytes
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

        if img is None:
            raise HTTPException(status_code=400, detail="Invalid image file format.")

        # Ensure active model
        if engine.active_model_type != model_type:
            engine.load_model(model_type)

        # Update engine parameters
        engine.slice_height = slice_size
        engine.slice_width = slice_size
        engine.overlap_ratio = overlap
        engine.conf_threshold = conf
        engine.iou_threshold = iou

        # Run inference
        result = engine.predict(img, conf=conf, iou=iou, use_sahi=use_sahi)

        # Render visualization
        vis_img = engine.visualize(
            img,
            result,
            draw_boxes=draw_boxes,
            draw_masks=draw_masks,
            draw_labels=draw_labels
        )

        # Encode image to JPEG base64 for fast frontend transmission
        _, buffer = cv2.imencode(".jpg", vis_img, [cv2.IMWRITE_JPEG_QUALITY, 88])
        b64_str = base64.b64encode(buffer).decode("utf-8")

        # Format detection details for json
        formatted_detections = []
        for idx, d in enumerate(result["detections"]):
            box = d["bbox"].tolist()
            poly = d["polygon"].tolist() if d["polygon"] is not None else None
            formatted_detections.append({
                "id": idx + 1,
                "class_name": d["class_name"],
                "score": round(float(d["score"]), 4),
                "bbox": [round(c, 1) for c in box],
                "area": round(float(d["area"]), 1),
                "polygon": poly
            })

        return {
            "success": True,
            "filename": file.filename,
            "mode": result["mode"],
            "model_type": result["model_type"],
            "image_shape": result["image_shape"],
            "total_count": result["total_count"],
            "total_slices": result["total_slices"],
            "inference_time_ms": round(result["inference_time"] * 1000, 1),
            "detections": formatted_detections,
            "image_data": f"data:image/jpeg;base64,{b64_str}"
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


import zipfile
import uuid
import csv

BATCH_DIR = os.path.join(BASE_DIR, "runs", "batch_exports")
os.makedirs(BATCH_DIR, exist_ok=True)


@app.post("/api/predict_batch")
async def predict_batch(
    files: list[UploadFile] = File(...),
    model_type: str = Form("11l"),
    use_sahi: bool = Form(True),
    slice_size: int = Form(640),
    overlap: float = Form(0.20),
    conf: float = Form(0.25),
    iou: float = Form(0.45)
):
    """
    Processes multiple uploaded images, calculates spoonbill counts for each,
    saves all annotated images + CSV summary, and packages them into a downloadable ZIP.
    """
    if not files:
        raise HTTPException(status_code=400, detail="No files uploaded.")

    if engine.active_model_type != model_type:
        engine.load_model(model_type)

    engine.slice_height = slice_size
    engine.slice_width = slice_size
    engine.overlap_ratio = overlap
    engine.conf_threshold = conf
    engine.iou_threshold = iou

    batch_id = f"batch_{int(time.time())}_{uuid.uuid4().hex[:6]}"
    batch_folder = os.path.join(BATCH_DIR, batch_id)
    annotated_folder = os.path.join(batch_folder, "annotated_images")
    os.makedirs(annotated_folder, exist_ok=True)

    summary_list = []
    all_instances = []
    total_spoonbills = 0

    for idx, upload_file in enumerate(files, 1):
        try:
            contents = await upload_file.read()
            nparr = np.frombuffer(contents, np.uint8)
            img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            if img is None:
                continue

            h, w = img.shape[:2]
            fname = upload_file.filename
            bname, _ = os.path.splitext(fname)

            # Predict
            res = engine.predict(img, conf=conf, iou=iou, use_sahi=use_sahi)
            count = res["total_count"]
            latency = round(res["inference_time"] * 1000, 1)
            total_spoonbills += count

            # Visualize & Save image
            vis_img = engine.visualize(img, res)
            out_img_path = os.path.join(annotated_folder, f"{bname}_annotated.jpg")
            cv2.imwrite(out_img_path, vis_img)

            # Record summary
            summary_list.append({
                "id": idx,
                "filename": fname,
                "resolution": f"{w}x{h}",
                "bird_count": count,
                "latency_ms": latency,
                "slices": res.get("total_slices", 1),
                "mode": res["mode"]
            })

            # Record instances
            for d_idx, d in enumerate(res["detections"], 1):
                box = d["bbox"].tolist()
                all_instances.append({
                    "image_name": fname,
                    "bird_id": d_idx,
                    "class": d["class_name"],
                    "confidence": round(float(d["score"]), 4),
                    "x1": round(box[0], 1),
                    "y1": round(box[1], 1),
                    "x2": round(box[2], 1),
                    "y2": round(box[3], 1),
                    "area_px2": round(float(d["area"]), 1)
                })

        except Exception as e:
            print(f"[Batch Warning] Failed to process {upload_file.filename}: {e}")

    # 1. Write Summary CSV
    summary_csv_path = os.path.join(batch_folder, "Spoonbill_Census_Summary.csv")
    with open(summary_csv_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["#", "Image_Filename", "Resolution", "Total_Spoonbills_Detected", "Inference_Mode", "Latency_ms", "Slices_Count"])
        for item in summary_list:
            writer.writerow([
                item["id"],
                item["filename"],
                item["resolution"],
                item["bird_count"],
                item["mode"],
                item["latency_ms"],
                item["slices"]
            ])
        writer.writerow([])
        writer.writerow(["TOTAL", f"{len(summary_list)} Images", "-", total_spoonbills, "-", "-", "-"])

    # 2. Write Detailed Instances CSV
    detailed_csv_path = os.path.join(batch_folder, "Detailed_Instances_Data.csv")
    with open(detailed_csv_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["Image_Filename", "Bird_#", "Class_Name", "Confidence_Score", "BBox_X1", "BBox_Y1", "BBox_X2", "BBox_Y2", "Area_px2"])
        for inst in all_instances:
            writer.writerow([
                inst["image_name"],
                inst["bird_id"],
                inst["class"],
                inst["confidence"],
                inst["x1"],
                inst["y1"],
                inst["x2"],
                inst["y2"],
                inst["area_px2"]
            ])

    # 3. Create ZIP Archive
    zip_filename = f"Spoonbill_Census_{batch_id}.zip"
    zip_path = os.path.join(BATCH_DIR, zip_filename)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
        zipf.write(summary_csv_path, arcname="Spoonbill_Census_Summary.csv")
        zipf.write(detailed_csv_path, arcname="Detailed_Instances_Data.csv")
        for root, _, files_in_dir in os.walk(annotated_folder):
            for file_name in files_in_dir:
                full_p = os.path.join(root, file_name)
                arc_name = os.path.join("annotated_images", file_name)
                zipf.write(full_p, arcname=arc_name)

    return {
        "success": True,
        "batch_id": batch_id,
        "total_images": len(summary_list),
        "total_spoonbills": total_spoonbills,
        "summary": summary_list,
        "download_url": f"/api/download_batch_zip/{zip_filename}"
    }


@app.get("/api/download_batch_zip/{zip_name}")
async def download_batch_zip(zip_name: str):
    zip_path = os.path.join(BATCH_DIR, zip_name)
    if not os.path.exists(zip_path):
        raise HTTPException(status_code=404, detail="Batch ZIP file not found.")
    return FileResponse(
        zip_path,
        media_type="application/zip",
        filename=zip_name
    )


if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("PORT", 8080))
    print(f"\n=======================================================")
    print(f"   SPOONBILL STUDIO UNIVERSAL (V3 LEAN)")
    print(f"   Running on http://127.0.0.1:{port}")
    print(f"   Cross-Platform: Windows | Mac Intel | Mac Apple Silicon")
    print(f"=======================================================\n")
    uvicorn.run(app, host="127.0.0.1", port=port, log_level="info")
