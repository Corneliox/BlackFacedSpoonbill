"""
FastAPI Backend Server for Spoonbill Electron Studio V4.
Serves REST APIs for:
- Single Image Inference (SAHI Tiled & Direct Single-Pass) across 11 models
- Head-to-Head Model Comparison & Grad-CAM Feature Attribution Heatmaps
- Batch Population Census + Automated ZIP/CSV/YOLO Label Export
- 13-Model Evolution & Multi-Scale Ablation Benchmark Explorer
- Quick Sample Dataset Gallery
"""

import os
import sys
import csv
import time
import uuid
import base64
import zipfile
from typing import Optional, List, Tuple

import cv2
import numpy as np
from fastapi import FastAPI, File, UploadFile, Form, HTTPException
from fastapi.responses import HTMLResponse, JSONResponse, FileResponse
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware

# Ensure backend directory is in sys.path and CoordAtt is registered in __main__
if getattr(sys, "frozen", False):
    _FROZEN_ROOT = getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
    BACKEND_DIR = os.path.join(_FROZEN_ROOT, "backend")
else:
    BACKEND_DIR = os.path.dirname(os.path.abspath(__file__))

if BACKEND_DIR not in sys.path:
    sys.path.insert(0, BACKEND_DIR)

from unified_engine import UnifiedSpoonbillEngine, CoordAtt, register_coord_att, APP_ROOT_DIR, WORKSPACE_ROOT, USER_DATA_DIR

# Explicitly bind CoordAtt to __main__ for torch.load unpickling
try:
    setattr(sys.modules["__main__"], "CoordAtt", CoordAtt)
    register_coord_att()
except Exception:
    pass

app = FastAPI(
    title="Spoonbill Electron Studio V4.1 Experimental API",
    description="Unified AI Backend for Black-faced Spoonbill Instance Segmentation, Ablation & Census",
    version="4.1.0-experimental"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

RENDERER_DIR = os.path.join(APP_ROOT_DIR, "renderer")


def _resolve_batch_export_dir() -> str:
    if getattr(sys, "frozen", False) or sys.platform == "darwin":
        d = os.path.join(USER_DATA_DIR, "exports")
        os.makedirs(d, exist_ok=True)
        return d
    try:
        d = os.path.join(APP_ROOT_DIR, "exports")
        os.makedirs(d, exist_ok=True)
        return d
    except Exception:
        d = os.path.join(USER_DATA_DIR, "exports")
        os.makedirs(d, exist_ok=True)
        return d


def _resolve_dataset_dir() -> str:
    for cand in [
        os.path.join(APP_ROOT_DIR, "samples"),
        os.path.join(APP_ROOT_DIR, "Dataset"),
        os.path.join(WORKSPACE_ROOT, "Dataset"),
        os.path.join(WORKSPACE_ROOT, "docs", "samples"),
    ]:
        if os.path.isdir(cand):
            return cand
    return os.path.join(APP_ROOT_DIR, "samples")


BATCH_EXPORT_DIR = _resolve_batch_export_dir()
DATASET_DIR = _resolve_dataset_dir()
FIGURES_DIR = (
    os.path.join(APP_ROOT_DIR, "Result_Draft_Figures")
    if os.path.exists(os.path.join(APP_ROOT_DIR, "Result_Draft_Figures"))
    else os.path.join(WORKSPACE_ROOT, "12_Full", "Result_Draft_Figures")
)
BENCHMARK_CSV = (
    os.path.join(APP_ROOT_DIR, "Comprehensive_Model_Evolution_Benchmark.csv")
    if os.path.exists(os.path.join(APP_ROOT_DIR, "Comprehensive_Model_Evolution_Benchmark.csv"))
    else os.path.join(WORKSPACE_ROOT, "Comprehensive_Model_Evolution_Benchmark.csv")
)

if os.path.exists(RENDERER_DIR):
    app.mount("/renderer", StaticFiles(directory=RENDERER_DIR), name="renderer")

engine = UnifiedSpoonbillEngine()


def encode_img_b64(img: np.ndarray, quality: int = 90) -> str:
    _, buffer = cv2.imencode(".jpg", img, [cv2.IMWRITE_JPEG_QUALITY, quality])
    return f"data:image/jpeg;base64,{base64.b64encode(buffer).decode('utf-8')}"


def load_image_from_request(upload_file: Optional[UploadFile], sample_name: Optional[str]) -> Tuple[np.ndarray, str]:
    if sample_name:
        sample_path = os.path.join(DATASET_DIR, os.path.basename(sample_name))
        if not os.path.exists(sample_path):
            raise HTTPException(status_code=404, detail=f"Sample image '{sample_name}' not found.")
        img = cv2.imread(sample_path)
        if img is None:
            raise HTTPException(status_code=400, detail=f"Failed to decode sample image '{sample_name}'.")
        return img, os.path.basename(sample_name)

    if upload_file is not None:
        contents = upload_file.file.read()
        nparr = np.frombuffer(contents, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if img is None:
            raise HTTPException(status_code=400, detail="Invalid image file format.")
        return img, upload_file.filename or "uploaded_image.jpg"

    raise HTTPException(status_code=400, detail="No image file or sample_name provided.")


@app.get("/", response_class=HTMLResponse)
async def serve_root():
    idx = os.path.join(RENDERER_DIR, "index.html")
    if os.path.exists(idx):
        return FileResponse(idx)
    return HTMLResponse("<h2>Spoonbill Electron Studio V4.1 Experimental Backend Running</h2>")


@app.get("/api/health")
async def health_check():
    models = engine.get_available_models()
    return {
        "status": "healthy",
        "version": "4.1.0-experimental",
        "active_mode": engine.active_mode,
        "device": engine.device.upper(),
        "hw_info": engine.hw_info,
        "onnx_providers": engine.onnx_providers,
        "available_models_count": len(models),
        "export_dir": BATCH_EXPORT_DIR
    }


@app.get("/api/hardware/probe")
async def get_hardware_probe():
    return {
        "active_mode": engine.active_mode,
        "device": engine.device,
        "hw_info": engine.hw_info
    }


@app.post("/api/hardware/switch_device")
async def switch_hardware_device(mode: str = Form(...)):
    res = engine.switch_hardware_mode(mode)
    return res


@app.get("/api/models")
async def list_models():
    return {
        "active_mode": engine.active_mode,
        "device": engine.device.upper(),
        "models": engine.get_available_models()
    }


@app.get("/api/samples")
async def list_sample_images():
    """Returns curated sample images from the workspace Dataset folder for 1-click testing."""
    preferred = [
        ("1.jpg", "Burung Tunggal (Close-Up Paruh)"),
        ("100.jpg", "Kelompok Kecil di Rawa"),
        ("101.jpg", "Gerombolan Sedang"),
        ("102.jpg", "Habitat Perairan Pesisir"),
        ("208.jpg", "Panorama Koloni Padat"),
        ("210.jpg", "Panorama Jarak Jauh (Uji SAHI)")
    ]
    samples = []
    if os.path.isdir(DATASET_DIR):
        for fname, label in preferred:
            fpath = os.path.join(DATASET_DIR, fname)
            if os.path.exists(fpath):
                samples.append({
                    "filename": fname,
                    "label": label,
                    "url": f"/api/sample_image/{fname}"
                })
        # If fewer than 6 found, add any other jpgs
        if len(samples) < 6:
            for f in sorted(os.listdir(DATASET_DIR)):
                if f.lower().endswith((".jpg", ".jpeg", ".png")) and not any(s["filename"] == f for s in samples):
                    samples.append({
                        "filename": f,
                        "label": f"Dataset Sample ({f})",
                        "url": f"/api/sample_image/{f}"
                    })
                    if len(samples) >= 6:
                        break
    return {"samples": samples}


@app.get("/api/sample_image/{filename}")
async def get_sample_image(filename: str):
    safe_name = os.path.basename(filename)
    fpath = os.path.join(DATASET_DIR, safe_name)
    if not os.path.exists(fpath):
        raise HTTPException(status_code=404, detail="Sample image not found.")
    return FileResponse(fpath)


@app.post("/api/predict")
def predict_single(
    file: Optional[UploadFile] = File(None),
    sample_name: Optional[str] = Form(None),
    model_id: str = Form("v3_11l_onnx"),
    use_sahi: bool = Form(True),
    slice_size: int = Form(640),
    overlap: float = Form(0.20),
    conf: float = Form(0.25),
    iou: float = Form(0.45),
    draw_boxes: bool = Form(True),
    draw_masks: bool = Form(True),
    draw_labels: bool = Form(True),
    draw_hud: bool = Form(False)
):
    try:
        img, fname = load_image_from_request(file, sample_name)
        res = engine.predict(
            img=img,
            model_id=model_id,
            conf=conf,
            iou=iou,
            use_sahi=use_sahi,
            slice_size=slice_size,
            overlap=overlap
        )

        vis_img = engine.visualize(
            img,
            res,
            draw_boxes=draw_boxes,
            draw_masks=draw_masks,
            draw_labels=draw_labels,
            draw_hud=draw_hud
        )

        formatted_dets = []
        for idx, d in enumerate(res["detections"], 1):
            box = d["bbox"].tolist()
            poly = d["polygon"].tolist() if d["polygon"] is not None else None
            formatted_dets.append({
                "id": idx,
                "class_name": d["class_name"],
                "score": round(float(d["score"]), 4),
                "bbox": [round(c, 1) for c in box],
                "area": round(float(d["area"]), 1),
                "polygon": poly
            })

        return {
            "success": True,
            "filename": fname,
            "mode": res["mode"],
            "model_id": res["model_id"],
            "model_name": res["model_name"],
            "model_full_name": res["model_full_name"],
            "backend": res["backend"],
            "image_shape": res["image_shape"],
            "total_count": res["total_count"],
            "mean_confidence": res["mean_confidence"],
            "total_slices": res["total_slices"],
            "inference_time_ms": round(res["inference_time"] * 1000, 1),
            "detections": formatted_dets,
            "yolo_labels_txt": res["yolo_labels_txt"],
            "image_data": encode_img_b64(vis_img)
        }
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/api/compare")
def compare_models_endpoint(
    file: Optional[UploadFile] = File(None),
    sample_name: Optional[str] = Form(None),
    model_a: str = Form("ablation_baseline_l"),
    model_b: str = Form("ablation_beakattn_l"),
    compare_mode: str = Form("segmentation"),  # "segmentation" or "gradcam"
    use_sahi: bool = Form(True),
    slice_size: int = Form(640),
    overlap: float = Form(0.20),
    conf: float = Form(0.25),
    iou: float = Form(0.45),
    draw_hud: bool = Form(False)
):
    """
    Runs Head-to-Head comparison between Model A and Model B (either Instance Segmentation or Grad-CAM Attribution).
    """
    try:
        img, fname = load_image_from_request(file, sample_name)

        res_a = engine.predict(img, model_id=model_a, conf=conf, iou=iou, use_sahi=use_sahi, slice_size=slice_size, overlap=overlap)
        res_b = engine.predict(img, model_id=model_b, conf=conf, iou=iou, use_sahi=use_sahi, slice_size=slice_size, overlap=overlap)

        if compare_mode == "gradcam":
            vis_a = engine.generate_gradcam_overlay(img, model_a)
            vis_b = engine.generate_gradcam_overlay(img, model_b)
        else:
            vis_a = engine.visualize(img, res_a, draw_boxes=True, draw_masks=True, draw_labels=True, draw_hud=draw_hud)
            vis_b = engine.visualize(img, res_b, draw_boxes=True, draw_masks=True, draw_labels=True, draw_hud=draw_hud)

        return {
            "success": True,
            "filename": fname,
            "compare_mode": compare_mode,
            "model_a": {
                "id": res_a["model_id"],
                "name": res_a["model_name"],
                "full_name": res_a["model_full_name"],
                "count": res_a["total_count"],
                "mean_conf": res_a["mean_confidence"],
                "latency_ms": round(res_a["inference_time"] * 1000, 1),
                "slices": res_a["total_slices"],
                "image_data": encode_img_b64(vis_a)
            },
            "model_b": {
                "id": res_b["model_id"],
                "name": res_b["model_name"],
                "full_name": res_b["model_full_name"],
                "count": res_b["total_count"],
                "mean_conf": res_b["mean_confidence"],
                "latency_ms": round(res_b["inference_time"] * 1000, 1),
                "slices": res_b["total_slices"],
                "image_data": encode_img_b64(vis_b)
            },
            "delta": {
                "count_diff": res_b["total_count"] - res_a["total_count"],
                "conf_diff_pct": round((res_b["mean_confidence"] - res_a["mean_confidence"]) * 100, 2),
                "latency_diff_ms": round((res_b["inference_time"] - res_a["inference_time"]) * 1000, 1)
            }
        }
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/api/predict_batch")
def predict_batch_endpoint(
    files: List[UploadFile] = File(...),
    model_id: str = Form("v3_11l_onnx"),
    use_sahi: bool = Form(True),
    slice_size: int = Form(640),
    overlap: float = Form(0.20),
    conf: float = Form(0.25),
    iou: float = Form(0.45)
):
    if not files:
        raise HTTPException(status_code=400, detail="No files uploaded.")

    batch_id = f"census_{int(time.time())}_{uuid.uuid4().hex[:5]}"
    batch_folder = os.path.join(BATCH_EXPORT_DIR, batch_id)
    annotated_folder = os.path.join(batch_folder, "annotated_images")
    labels_folder = os.path.join(batch_folder, "yolo_polygon_labels")
    os.makedirs(annotated_folder, exist_ok=True)
    os.makedirs(labels_folder, exist_ok=True)

    summary_list = []
    all_instances = []
    total_spoonbills = 0

    for idx, upload_file in enumerate(files, 1):
        try:
            contents = upload_file.file.read()
            nparr = np.frombuffer(contents, np.uint8)
            img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            if img is None:
                continue

            h, w = img.shape[:2]
            fname = upload_file.filename or f"image_{idx}.jpg"
            bname, _ = os.path.splitext(fname)

            res = engine.predict(
                img=img,
                model_id=model_id,
                conf=conf,
                iou=iou,
                use_sahi=use_sahi,
                slice_size=slice_size,
                overlap=overlap
            )
            count = res["total_count"]
            latency = round(res["inference_time"] * 1000, 1)
            total_spoonbills += count

            vis_img = engine.visualize(img, res)
            cv2.imwrite(os.path.join(annotated_folder, f"{bname}_annotated.jpg"), vis_img)

            # Save YOLO polygon label .txt
            with open(os.path.join(labels_folder, f"{bname}.txt"), "w", encoding="utf-8") as lf:
                lf.write(res["yolo_labels_txt"])

            summary_list.append({
                "id": idx,
                "filename": fname,
                "resolution": f"{w}x{h}",
                "bird_count": count,
                "mean_conf": round(res["mean_confidence"] * 100, 1),
                "latency_ms": latency,
                "slices": res["total_slices"],
                "mode": res["mode"],
                "model_name": res["model_name"]
            })

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
            print(f"[Batch Error] {upload_file.filename}: {e}")

    # Write Summary CSV
    summary_csv_path = os.path.join(batch_folder, "Spoonbill_Census_Summary.csv")
    with open(summary_csv_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["#", "Image_Filename", "Resolution", "Total_Spoonbills", "Mean_Conf_%", "Model", "Inference_Mode", "Latency_ms", "Slices"])
        for item in summary_list:
            writer.writerow([
                item["id"], item["filename"], item["resolution"], item["bird_count"],
                item["mean_conf"], item["model_name"], item["mode"], item["latency_ms"], item["slices"]
            ])
        writer.writerow([])
        writer.writerow(["TOTAL", f"{len(summary_list)} Images", "-", total_spoonbills, "-", "-", "-", "-", "-"])

    # Write Detailed Instances CSV
    detailed_csv_path = os.path.join(batch_folder, "Detailed_Instances_Data.csv")
    with open(detailed_csv_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["Image_Filename", "Bird_#", "Class_Name", "Confidence_Score", "BBox_X1", "BBox_Y1", "BBox_X2", "BBox_Y2", "Area_px2"])
        for inst in all_instances:
            writer.writerow([
                inst["image_name"], inst["bird_id"], inst["class"], inst["confidence"],
                inst["x1"], inst["y1"], inst["x2"], inst["y2"], inst["area_px2"]
            ])

    # Create ZIP Archive
    zip_filename = f"Spoonbill_Census_{batch_id}.zip"
    zip_path = os.path.join(BATCH_EXPORT_DIR, zip_filename)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
        zipf.write(summary_csv_path, arcname="Spoonbill_Census_Summary.csv")
        zipf.write(detailed_csv_path, arcname="Detailed_Instances_Data.csv")
        for folder_name, folder_path in [("annotated_images", annotated_folder), ("yolo_polygon_labels", labels_folder)]:
            for root, _, files_in_dir in os.walk(folder_path):
                for file_name in files_in_dir:
                    full_p = os.path.join(root, file_name)
                    zipf.write(full_p, arcname=os.path.join(folder_name, file_name))

    return {
        "success": True,
        "batch_id": batch_id,
        "export_folder": batch_folder,
        "total_images": len(summary_list),
        "total_spoonbills": total_spoonbills,
        "summary": summary_list,
        "download_url": f"/api/download_batch_zip/{zip_filename}"
    }


@app.get("/api/download_batch_zip/{zip_name}")
async def download_batch_zip(zip_name: str):
    safe_zip = os.path.basename(zip_name)
    zip_path = os.path.join(BATCH_EXPORT_DIR, safe_zip)
    if not os.path.exists(zip_path):
        raise HTTPException(status_code=404, detail="Batch ZIP not found.")
    return FileResponse(zip_path, media_type="application/zip", filename=safe_zip)


@app.get("/api/benchmarks")
async def get_benchmarks():
    """Reads Comprehensive_Model_Evolution_Benchmark.csv and available research figures."""
    rows = []
    if os.path.exists(BENCHMARK_CSV):
        with open(BENCHMARK_CSV, "r", encoding="utf-8") as f:
            reader = csv.DictReader(f)
            for r in reader:
                rows.append(r)

    figures = []
    fig_meta = [
        ("Figure1_Comprehensive_Evolution.png", "Figure 1: Evolusi Komprehensif 13 Model (Phase 1 - Phase 4)"),
        ("Figure2_HeadToHead_Ablation.png", "Figure 2: Head-to-Head Baseline vs Beak-Attention (Skala N, S, M, L)"),
        ("Figure3_Precision_Suppression.png", "Figure 3: Peningkatan Presisi +19.38% & Penekanan False-Positive pada Skala Large"),
        ("Figure4_Overparameterization_Curve.png", "Figure 4: Kurva Kapasitas Parameter vs Saturasi Akurasi Segmentasi")
    ]
    if os.path.isdir(FIGURES_DIR):
        for fname, title in fig_meta:
            if os.path.exists(os.path.join(FIGURES_DIR, fname)):
                figures.append({
                    "filename": fname,
                    "title": title,
                    "url": f"/api/figures/{fname}"
                })

    return {
        "benchmarks": rows,
        "figures": figures
    }


@app.get("/api/figures/{fig_name}")
async def get_figure(fig_name: str):
    safe_name = os.path.basename(fig_name)
    fpath = os.path.join(FIGURES_DIR, safe_name)
    if not os.path.exists(fpath):
        raise HTTPException(status_code=404, detail="Figure not found.")
    return FileResponse(fpath)


if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("SPOONBILL_PORT", 8765))
    print(f"\n=======================================================")
    print(f"   SPOONBILL ELECTRON STUDIO V4 - UNIFIED AI SERVER")
    print(f"   Listening on http://127.0.0.1:{port}")
    print(f"=======================================================\n")
    uvicorn.run(app, host="127.0.0.1", port=port, log_level="warning")
