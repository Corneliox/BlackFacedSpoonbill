"""
Unified AI Inference & Feature Attribution Engine for Spoonbill Electron Studio V4.
Integrates all project modules developed across Phases 1-4:
1. Pure ONNX Runtime Sliced (SAHI) & Direct Inference (V3 Production 11L & 11N)
2. PyTorch / Ultralytics Multi-Scale Ablation Suite (N, S, M, L — Baseline & BeakAttn CoordAtt @ P3)
3. Legacy Baseline YOLOv11n
4. Head-to-Head Model Comparator (Side-by-Side Segmentation & Metrics Delta)
5. Grad-CAM / Eigen-CAM Spatial Feature Attribution Heatmap Generator
6. Automated YOLO Polygon Label Formatter (from SAM / YOLO pipeline)
"""

import os
import sys
import time
import math
import builtins
import platform
import subprocess
import shutil
from typing import List, Dict, Tuple, Optional, Union

import cv2
import numpy as np
import onnxruntime as ort
try:
    import torch
    import torch.nn as nn
    HAS_TORCH = True
except ImportError:
    torch = None
    nn = None
    HAS_TORCH = False

# =====================================================================
# 1. COORDINATE ATTENTION MODULE & GLOBAL ULTRALYTICS REGISTRATION
# =====================================================================
if HAS_TORCH:
    class CoordAtt(nn.Module):
        """
        Coordinate Attention block with dynamic channel adaptation for YOLOv11/12 scales.
        Decomposes 2D spatial pooling into horizontal and vertical directional 1D encoding.
        """
        def __init__(self, *args, reduction=32, **kwargs):
            super().__init__()
            self.reduction = reduction
            self.pool_h = nn.AdaptiveAvgPool2d((None, 1))
            self.pool_w = nn.AdaptiveAvgPool2d((1, None))
            self.act = nn.SiLU()
            self.conv1 = None
            self.bn1 = None
            self.conv_h = None
            self.conv_w = None
            self.built = False

        def _build(self, c, device):
            mip = max(8, c // self.reduction)
            self.conv1 = nn.Conv2d(c, mip, kernel_size=1, stride=1, padding=0).to(device)
            self.bn1 = nn.BatchNorm2d(mip).to(device)
            self.conv_h = nn.Conv2d(mip, c, kernel_size=1, stride=1, padding=0).to(device)
            self.conv_w = nn.Conv2d(mip, c, kernel_size=1, stride=1, padding=0).to(device)
            self.built = True

        def forward(self, x):
            if not self.built:
                self._build(x.size(1), x.device)
            identity = x
            n, c, h, w = x.size()
            x_h = self.pool_h(x)
            x_w = self.pool_w(x).permute(0, 1, 3, 2)
            y = torch.cat([x_h, x_w], dim=2)
            y = self.act(self.bn1(self.conv1(y)))
            x_h, x_w = torch.split(y, [h, w], dim=2)
            x_w = x_w.permute(0, 1, 3, 2)
            return identity * self.conv_w(x_w).sigmoid() * self.conv_h(x_h).sigmoid()
else:
    class CoordAtt:
        """Fallback CoordAtt dummy when running in pure ONNX Runtime mode without PyTorch."""
        def __init__(self, *args, **kwargs):
            pass


def register_coord_att():
    """Registers CoordAtt across all namespaces required by PyTorch pickle & Ultralytics."""
    if not HAS_TORCH:
        return
    setattr(builtins, "CoordAtt", CoordAtt)
    for mod_name in ["__main__", "__mp_main__"]:
        if mod_name in sys.modules:
            setattr(sys.modules[mod_name], "CoordAtt", CoordAtt)
    try:
        import ultralytics.nn.modules as u_modules
        import ultralytics.nn.tasks as u_tasks
        setattr(u_modules, "CoordAtt", CoordAtt)
        setattr(u_tasks, "CoordAtt", CoordAtt)
    except Exception as e:
        print(f"[Engine Warning] Could not register CoordAtt in ultralytics: {e}")


register_coord_att()


# =====================================================================
# 2. WORKSPACE PATH RESOLUTION & MODEL CATALOG
# =====================================================================
BACKEND_DIR = os.path.dirname(os.path.abspath(__file__))
APP_ROOT_DIR = os.path.dirname(BACKEND_DIR)

def resolve_workspace_root() -> str:
    p = BACKEND_DIR
    for _ in range(6):
        if os.path.exists(os.path.join(p, "12_Full")) or os.path.exists(os.path.join(p, "NewModels Reinforced")):
            return p
        parent = os.path.dirname(p)
        if parent == p:
            break
        p = parent
    default_ws = r"d:\~Ideas n Innovation\~~Taiwan\AU\Gilang\Segmentasi 26-8-17"
    if os.path.exists(default_ws):
        return default_ws
    return os.path.dirname(APP_ROOT_DIR)

WORKSPACE_ROOT = resolve_workspace_root()


def probe_system_hardware() -> Dict:
    """Probes system hardware (CPU, NVIDIA, AMD, Intel, Apple Silicon) and PyTorch CUDA environment."""
    system_os = platform.system()  # 'Windows', 'Darwin', 'Linux'
    arch = platform.machine().lower()
    is_mac = system_os == "Darwin"
    is_apple_silicon = is_mac and ("arm" in arch or "m" in platform.processor().lower())

    cpu_name = platform.processor() or platform.machine()
    cpu_cores = os.cpu_count() or 4

    has_torch = False
    cuda_available = False
    cuda_device_name = None
    cuda_vram_gb = 0.0
    cuda_compute = None
    cuda_version = None
    mps_available = False

    try:
        import torch
        has_torch = True
        cuda_available = torch.cuda.is_available()
        if cuda_available and torch.cuda.device_count() > 0:
            cuda_device_name = torch.cuda.get_device_name(0)
            cuda_vram_gb = round(torch.cuda.get_device_properties(0).total_memory / (1024**3), 2)
            cuda_compute = f"{torch.cuda.get_device_capability(0)[0]}.{torch.cuda.get_device_capability(0)[1]}"
            cuda_version = getattr(torch.version, "cuda", None)
        if hasattr(torch.backends, "mps") and torch.backends.mps.is_available():
            mps_available = True
    except Exception:
        pass

    physical_gpus = []
    has_nvidia_hardware = False
    has_amd_hardware = False
    has_intel_gpu = False

    try:
        if system_os == "Windows":
            cmd = ["powershell", "-NoProfile", "-Command", "Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name"]
            res = subprocess.run(cmd, capture_output=True, text=True, timeout=3)
            if res.returncode == 0:
                for line in res.stdout.strip().splitlines():
                    name = line.strip()
                    if name:
                        physical_gpus.append(name)
                        low = name.lower()
                        if any(k in low for k in ["nvidia", "geforce", "quadro", "rtx", "gtx", "tesla"]):
                            has_nvidia_hardware = True
                        elif any(k in low for k in ["amd", "radeon"]):
                            has_amd_hardware = True
                        elif "intel" in low:
                            has_intel_gpu = True
        elif is_mac:
            cmd = ["system_profiler", "SPDisplaysDataType"]
            res = subprocess.run(cmd, capture_output=True, text=True, timeout=3)
            if res.returncode == 0:
                for line in res.stdout.splitlines():
                    if "Chipset Model:" in line:
                        chip = line.split(":", 1)[1].strip()
                        physical_gpus.append(chip)
        elif system_os == "Linux":
            cmd = ["lspci"]
            res = subprocess.run(cmd, capture_output=True, text=True, timeout=3)
            if res.returncode == 0:
                for line in res.stdout.splitlines():
                    if any(k in line.lower() for k in ["vga", "3d", "display"]):
                        gpu_item = line.split(":", 2)[-1].strip()
                        physical_gpus.append(gpu_item)
                        if "nvidia" in gpu_item.lower():
                            has_nvidia_hardware = True
    except Exception:
        pass

    if cuda_available:
        status_id = "nvidia_cuda_ready"
        status_label = "NVIDIA CUDA Acceleration"
        badge_color = "#10B981"
        desc = f"NVIDIA GPU terdeteksi & CUDA aktif ({cuda_device_name}, {cuda_vram_gb} GB VRAM). Akselerasi penuh aktif untuk semua skala model (N, S, M, L)."
    elif has_nvidia_hardware and not cuda_available:
        status_id = "nvidia_no_cuda"
        status_label = "Hardware NVIDIA Terdeteksi (PyTorch CUDA Belum Terpasang)"
        badge_color = "#F59E0B"
        desc = f"Hardware NVIDIA terdeteksi ({', '.join(physical_gpus)}), namun PyTorch berjalan dalam mode CPU. Untuk mengaktifkan akselerasi GPU, install PyTorch dengan dukungan CUDA."
    elif mps_available or is_apple_silicon:
        status_id = "apple_silicon"
        status_label = "Apple Silicon Metal Acceleration"
        badge_color = "#3B82F6"
        desc = "Apple Silicon SoC terdeteksi. Menggunakan akselerasi terpadu CPU + Metal Performance Shaders (MPS)."
    else:
        status_id = "cpu_only"
        status_label = "CPU Safe Mode"
        badge_color = "#71717A"
        gpu_str = (", ".join(physical_gpus)) if physical_gpus else "Integrated Graphics / Standard CPU"
        desc = f"Perangkat berjalan dalam mode CPU aman ({gpu_str}). Sangat direkomendasikan menggunakan model Nano (11N) untuk kecepatan instan (<60ms)."

    return {
        "system_os": system_os,
        "arch": arch,
        "is_mac": is_mac,
        "is_apple_silicon": is_apple_silicon,
        "cpu_name": cpu_name,
        "cpu_cores": cpu_cores,
        "physical_gpus": physical_gpus,
        "has_nvidia_hardware": has_nvidia_hardware,
        "has_amd_hardware": has_amd_hardware,
        "has_intel_gpu": has_intel_gpu,
        "has_torch": has_torch,
        "cuda_available": cuda_available,
        "cuda_device_name": cuda_device_name,
        "cuda_vram_gb": cuda_vram_gb,
        "cuda_compute": cuda_compute,
        "cuda_version": cuda_version,
        "mps_available": mps_available,
        "status_id": status_id,
        "status_label": status_label,
        "badge_color": badge_color,
        "can_enable_gpu": bool(cuda_available or mps_available),
        "description": desc
    }


def build_model_catalog() -> Dict[str, Dict]:
    """Builds the complete registry of trained models across Phases 1-4."""
    w = WORKSPACE_ROOT
    catalog = {
        "v3_11l_onnx": {
            "id": "v3_11l_onnx",
            "name": "YOLO11 Large V3 (ONNX Runtime)",
            "short_name": "11L V3 (ONNX)",
            "phase": "Phase 2: Production V3",
            "backend": "onnx",
            "scale": "L",
            "params": "27.68M",
            "mask_map50": 0.9394,
            "box_prec": 0.8738,
            "badge": "High Accuracy Panorama",
            "desc": "Model produksi presisi penuh (FP32 ONNX) untuk panorama resolusi tinggi & gerombolan padat.",
            "candidates": [
                os.path.join(APP_ROOT_DIR, "models", "best_spoonbill_11l.onnx"),
                os.path.join(w, "Spoonbill_Studio_Universal", "models", "best_spoonbill_11l.onnx"),
                os.path.join(w, "PullGit", "BlackFacedSpoonbill", "App", "models", "best_spoonbill_11l.onnx"),
                os.path.join(w, "NewModels Reinforced", "11l", "best_spoonbill_11l.onnx"),
            ]
        },
        "v3_11n_onnx": {
            "id": "v3_11n_onnx",
            "name": "YOLO11 Nano V3 (ONNX Runtime)",
            "short_name": "11N V3 (ONNX)",
            "phase": "Phase 2: Production V3",
            "backend": "onnx",
            "scale": "N",
            "params": "2.87M",
            "mask_map50": 0.9199,
            "box_prec": 0.8383,
            "badge": "Ultra-Fast CPU/Edge",
            "desc": "Model ringan dan super cepat (<60ms) tanpa ketergantungan GPU berat. Sangat direkomendasikan untuk laptop CPU/Baterai.",
            "candidates": [
                os.path.join(APP_ROOT_DIR, "models", "best_spoonbill_11n.onnx"),
                os.path.join(w, "Spoonbill_Studio_Universal", "models", "best_spoonbill_11n.onnx"),
                os.path.join(w, "PullGit", "BlackFacedSpoonbill", "App", "models", "best_spoonbill_11n.onnx"),
                os.path.join(w, "NewModels Reinforced", "11n", "best.onnx"),
            ]
        },
        "ablation_beakattn_l": {
            "id": "ablation_beakattn_l",
            "name": "YOLO11L + BeakAttention (CoordAtt @ P3)",
            "short_name": "BeakAttn-L (P3)",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "L",
            "params": "27.72M",
            "mask_map50": 0.8622,
            "box_prec": 0.8775,
            "badge": "+19.4% Precision Breakthrough",
            "desc": "Dilengkapi Coordinate Attention @ P3 untuk fokus pada paruh spatula & menekan false-positive pantulan air/kuntul.",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "BeakAttn_L", "weights", "best.pt"),
            ]
        },
        "ablation_baseline_l": {
            "id": "ablation_baseline_l",
            "name": "YOLO11L Baseline (Ablation L)",
            "short_name": "Baseline-L",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "L",
            "params": "27.68M",
            "mask_map50": 0.8717,
            "box_prec": 0.6837,
            "badge": "High Recall (90.1%)",
            "desc": "Model Large standar tanpa atensi koordinat (pembanding efek overparameterization pada rawa).",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "Baseline_L", "weights", "best.pt"),
            ]
        },
        "ablation_baseline_n": {
            "id": "ablation_baseline_n",
            "name": "YOLO11N Baseline (Ablation N)",
            "short_name": "Baseline-N",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "N",
            "params": "2.84M",
            "mask_map50": 0.9450,
            "box_prec": 0.9124,
            "badge": "Best Edge Accuracy (94.5%)",
            "desc": "Performa segmentasi utuh terbaik pada skala kompak (mAP50 94.5%, mAP50-95 74.1%).",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "Baseline_N", "weights", "best.pt"),
            ]
        },
        "ablation_beakattn_n": {
            "id": "ablation_beakattn_n",
            "name": "YOLO11N + BeakAttention (CoordAtt @ P3)",
            "short_name": "BeakAttn-N (P3)",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "N",
            "params": "2.85M",
            "mask_map50": 0.9244,
            "box_prec": 0.8393,
            "badge": "Edge Beak-Focused",
            "desc": "Model Nano dengan modul Coordinate Attention pada pyramid P3.",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "BeakAttn_N", "weights", "best.pt"),
            ]
        },
        "ablation_baseline_m": {
            "id": "ablation_baseline_m",
            "name": "YOLO11M Baseline (Ablation M)",
            "short_name": "Baseline-M",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "M",
            "params": "22.42M",
            "mask_map50": 0.9513,
            "box_prec": 0.9126,
            "badge": "Peak Mask mAP50 (95.1%)",
            "desc": "Mencapai skor Mask mAP50 tertinggi (95.13%) pada seri ablasi.",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "Baseline_M", "weights", "best.pt"),
            ]
        },
        "ablation_beakattn_m": {
            "id": "ablation_beakattn_m",
            "name": "YOLO11M + BeakAttention (CoordAtt @ P3)",
            "short_name": "BeakAttn-M (P3)",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "M",
            "params": "22.45M",
            "mask_map50": 0.9142,
            "box_prec": 0.8321,
            "badge": "Medium Beak-Focused",
            "desc": "Model skala Medium dengan injeksi Coordinate Attention @ P3.",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "BeakAttn_M", "weights", "best.pt"),
            ]
        },
        "ablation_baseline_s": {
            "id": "ablation_baseline_s",
            "name": "YOLO11S Baseline (Ablation S)",
            "short_name": "Baseline-S",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "S",
            "params": "10.09M",
            "mask_map50": 0.9284,
            "box_prec": 0.8200,
            "badge": "100% Mask Recall",
            "desc": "Model skala Small dengan tingkat sensitivitas (Recall) sempurna 100% pada validasi.",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "Baseline_S", "weights", "best.pt"),
            ]
        },
        "ablation_beakattn_s": {
            "id": "ablation_beakattn_s",
            "name": "YOLO11S + BeakAttention (CoordAtt @ P3)",
            "short_name": "BeakAttn-S (P3)",
            "phase": "Phase 4: Multi-Scale Ablation",
            "backend": "torch",
            "scale": "S",
            "params": "10.11M",
            "mask_map50": 0.8942,
            "box_prec": 0.7956,
            "badge": "Small Beak-Focused",
            "desc": "Model skala Small dengan injeksi Coordinate Attention @ P3.",
            "candidates": [
                os.path.join(w, "12_Full", "runs", "segment", "Ablation_BeakAttention", "BeakAttn_S", "weights", "best.pt"),
            ]
        },
        "legacy_11n": {
            "id": "legacy_11n",
            "name": "Legacy YOLO11n (Phase 1 - 268 img)",
            "short_name": "Legacy 11N (V1)",
            "phase": "Phase 1: Legacy Baseline",
            "backend": "torch",
            "scale": "N",
            "params": "2.87M",
            "mask_map50": 0.9551,
            "box_prec": 0.9967,
            "badge": "Initial Prototype",
            "desc": "Model awal 50-epoch yang dilatih pada 268 citra lokal awal.",
            "candidates": [
                os.path.join(w, "PullGit", "BlackFacedSpoonbill", "Yolov11_InsSeg", "best.pt"),
            ]
        }
    }

    # Resolve paths and mark availability
    for k, item in catalog.items():
        resolved = None
        for p in item["candidates"]:
            if os.path.exists(p):
                resolved = p
                break
        item["path"] = resolved
        item["available"] = resolved is not None
        if resolved:
            item["size_mb"] = round(os.path.getsize(resolved) / (1024 * 1024), 1)
        else:
            item["size_mb"] = 0.0

    return catalog


# =====================================================================
# 3. UNIFIED HYBRID ENGINE (ONNX RUNTIME + PYTORCH ULTRALYTICS + SAHI)
# =====================================================================
class UnifiedSpoonbillEngine:
    def __init__(self):
        self.catalog = build_model_catalog()
        self.onnx_sessions: Dict[str, ort.InferenceSession] = {}
        self.torch_models: Dict[str, object] = {}
        self.hw_info = probe_system_hardware()

        # Always start in CPU Safe Mode by default unless user explicitly activates GPU
        self.active_mode = "cpu"
        self.device = "cpu"

        # Setup ONNX providers
        avail = ort.get_available_providers()
        self.onnx_providers = [
            p for p in ["CoreMLExecutionProvider", "DmlExecutionProvider", "CUDAExecutionProvider", "CPUExecutionProvider"]
            if p in avail
        ] or ["CPUExecutionProvider"]

        print(f"[UnifiedEngine] Initialized in {self.active_mode.upper()} Safe Mode | Hardware: {self.hw_info['status_label']} | ONNX Providers: {self.onnx_providers}")

    def switch_hardware_mode(self, requested_mode: str) -> Dict:
        """Dynamically switches between CPU Safe Mode and GPU Acceleration (NVIDIA CUDA / Apple Metal)."""
        req = requested_mode.lower()
        if req in ["cuda", "gpu", "nvidia"]:
            if not self.hw_info.get("cuda_available") and not self.hw_info.get("mps_available"):
                return {
                    "success": False,
                    "active_mode": self.active_mode,
                    "device": self.device,
                    "message": "Akselerasi NVIDIA CUDA tidak terdeteksi pada perangkat ini. Sistem tetap berjalan dalam CPU Safe Mode.",
                    "hw_info": self.hw_info
                }
            self.active_mode = "cuda" if self.hw_info.get("cuda_available") else "mps"
            self.device = "cuda" if self.hw_info.get("cuda_available") else "mps"
            for mid, m in self.torch_models.items():
                try:
                    if hasattr(m, "to"):
                        m.to(self.device)
                except Exception:
                    pass
            print(f"[UnifiedEngine] Hardware Mode Switched to GPU: {self.device.upper()}")
            return {
                "success": True,
                "active_mode": self.active_mode,
                "device": self.device,
                "message": f"Akselerasi {self.device.upper()} berhasil diaktifkan.",
                "hw_info": self.hw_info
            }
        else:
            self.active_mode = "cpu"
            self.device = "cpu"
            for mid, m in self.torch_models.items():
                try:
                    if hasattr(m, "to"):
                        m.to("cpu")
                except Exception:
                    pass
            print("[UnifiedEngine] Hardware Mode Switched to CPU Safe Mode")
            return {
                "success": True,
                "active_mode": self.active_mode,
                "device": self.device,
                "message": "Sistem beralih ke CPU Safe Mode.",
                "hw_info": self.hw_info
            }

    def get_available_models(self) -> List[Dict]:
        models = []
        is_cpu_mode = self.active_mode == "cpu"
        for m in self.catalog.values():
            if not m["available"]:
                continue
            scale = m.get("scale", "N")
            is_nano = scale == "N"
            is_large_or_med = scale in ["M", "L"]

            # Recommendation rules per user specification:
            # - CPU mode: disarankan untuk n model saja
            # - M dan L: diberi highlight/warning menyatakan tidak menyarankan jika di CPU
            recommended = is_nano if is_cpu_mode else True
            cpu_warning = is_cpu_mode and is_large_or_med
            warning_text = "⚠️ Disarankan NVIDIA GPU (Beban CPU Tinggi)" if cpu_warning else ""

            models.append({
                "id": m["id"],
                "name": m["name"],
                "short_name": m["short_name"],
                "phase": m["phase"],
                "backend": m["backend"].upper(),
                "scale": scale,
                "params": m["params"],
                "size_mb": m["size_mb"],
                "mask_map50": m["mask_map50"],
                "box_prec": m["box_prec"],
                "badge": m["badge"],
                "desc": m["desc"],
                "available": m["available"],
                "recommended": recommended,
                "cpu_warning": cpu_warning,
                "warning_text": warning_text
            })
        return models

    def _get_onnx_session(self, model_id: str) -> ort.InferenceSession:
        if model_id not in self.onnx_sessions:
            info = self.catalog.get(model_id)
            if not info or not info["available"]:
                raise FileNotFoundError(f"ONNX model '{model_id}' not found.")
            opts = ort.SessionOptions()
            opts.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
            opts.intra_op_num_threads = os.cpu_count() or 4
            print(f"[UnifiedEngine] Loading ONNX session: {info['name']}...")
            self.onnx_sessions[model_id] = ort.InferenceSession(
                info["path"], sess_options=opts, providers=self.onnx_providers
            )
        return self.onnx_sessions[model_id]

    def _get_torch_model(self, model_id: str):
        if not HAS_TORCH:
            raise RuntimeError(f"PyTorch is not installed in this environment. Please run ONNX models or install PyTorch to use '{model_id}'.")
        register_coord_att()
        if model_id not in self.torch_models:
            info = self.catalog.get(model_id)
            if not info or not info["available"]:
                raise FileNotFoundError(f"PyTorch model '{model_id}' not found.")
            from ultralytics import YOLO
            print(f"[UnifiedEngine] Loading PyTorch YOLO model: {info['name']}...")
            self.torch_models[model_id] = YOLO(info["path"])
        return self.torch_models[model_id]

    @staticmethod
    def _generate_slices(img_h: int, img_w: int, slice_size: int, overlap_ratio: float) -> List[Tuple[int, int, int, int]]:
        if img_h <= slice_size and img_w <= slice_size:
            return [(0, 0, img_w, img_h)]
        step_y = max(1, int(slice_size * (1.0 - overlap_ratio)))
        step_x = max(1, int(slice_size * (1.0 - overlap_ratio)))

        y_coords = list(range(0, img_h - slice_size + 1, step_y))
        if not y_coords or y_coords[-1] + slice_size < img_h:
            y_coords.append(max(0, img_h - slice_size))

        x_coords = list(range(0, img_w - slice_size + 1, step_x))
        if not x_coords or x_coords[-1] + slice_size < img_w:
            x_coords.append(max(0, img_w - slice_size))

        y_coords = sorted(list(set(y_coords)))
        x_coords = sorted(list(set(x_coords)))

        slices = []
        for y in y_coords:
            for x in x_coords:
                slices.append((x, y, min(img_w, x + slice_size), min(img_h, y + slice_size)))
        return slices

    @staticmethod
    def _nms_indices(boxes: np.ndarray, scores: np.ndarray, iou_thresh: float) -> List[int]:
        order = scores.argsort()[::-1]
        keep = []
        while order.size > 0:
            i = order[0]
            keep.append(i)
            if order.size == 1:
                break
            xx1 = np.maximum(boxes[i, 0], boxes[order[1:], 0])
            yy1 = np.maximum(boxes[i, 1], boxes[order[1:], 1])
            xx2 = np.minimum(boxes[i, 2], boxes[order[1:], 2])
            yy2 = np.minimum(boxes[i, 3], boxes[order[1:], 3])
            w = np.maximum(0.0, xx2 - xx1)
            h = np.maximum(0.0, yy2 - yy1)
            inter = w * h
            area_i = (boxes[i, 2] - boxes[i, 0]) * (boxes[i, 3] - boxes[i, 1])
            area_rem = (boxes[order[1:], 2] - boxes[order[1:], 0]) * (boxes[order[1:], 3] - boxes[order[1:], 1])
            union = area_i + area_rem - inter
            iou = inter / np.maximum(union, 1e-6)
            inds = np.where(iou <= iou_thresh)[0]
            order = order[inds + 1]
        return keep

    def _preprocess_onnx(self, img: np.ndarray, target_size: int = 640) -> Tuple[np.ndarray, float, Tuple[float, float]]:
        h, w = img.shape[:2]
        r = min(target_size / h, target_size / w)
        new_unpad = (int(round(w * r)), int(round(h * r)))
        dw, dh = (target_size - new_unpad[0]) / 2, (target_size - new_unpad[1]) / 2

        img_resized = cv2.resize(img, new_unpad, interpolation=cv2.INTER_LINEAR) if (w, h) != new_unpad else img
        top, bottom = int(round(dh - 0.1)), int(round(dh + 0.1))
        left, right = int(round(dw - 0.1)), int(round(dw + 0.1))
        img_padded = cv2.copyMakeBorder(img_resized, top, bottom, left, right, cv2.BORDER_CONSTANT, value=(114, 114, 114))

        blob = cv2.cvtColor(img_padded, cv2.COLOR_BGR2RGB).astype(np.float32) / 255.0
        blob = np.transpose(blob, (2, 0, 1))[np.newaxis, ...]
        return blob, r, (dw, dh)

    def _postprocess_onnx(
        self,
        output0: np.ndarray,
        output1: np.ndarray,
        orig_shape: Tuple[int, int],
        ratio: float,
        pad: Tuple[float, float],
        conf_thresh: float,
        iou_thresh: float
    ) -> List[Dict]:
        preds = np.transpose(output0[0], (1, 0))
        boxes_xywh = preds[:, :4]
        scores = preds[:, 4]
        mask_coeffs = preds[:, 5:]
        proto_masks = output1[0]

        mask_conf = scores >= conf_thresh
        if not np.any(mask_conf):
            return []

        boxes_xywh = boxes_xywh[mask_conf]
        scores = scores[mask_conf]
        mask_coeffs = mask_coeffs[mask_conf]

        x1 = boxes_xywh[:, 0] - boxes_xywh[:, 2] / 2
        y1 = boxes_xywh[:, 1] - boxes_xywh[:, 3] / 2
        x2 = boxes_xywh[:, 0] + boxes_xywh[:, 2] / 2
        y2 = boxes_xywh[:, 1] + boxes_xywh[:, 3] / 2

        dw, dh = pad
        orig_h, orig_w = orig_shape
        x1 = np.clip((x1 - dw) / ratio, 0, orig_w)
        y1 = np.clip((y1 - dh) / ratio, 0, orig_h)
        x2 = np.clip((x2 - dw) / ratio, 0, orig_w)
        y2 = np.clip((y2 - dh) / ratio, 0, orig_h)
        boxes_orig = np.column_stack([x1, y1, x2, y2])

        keep_indices = self._nms_indices(boxes_orig, scores, iou_thresh)
        if not keep_indices:
            return []

        boxes_orig = boxes_orig[keep_indices]
        scores = scores[keep_indices]
        mask_coeffs = mask_coeffs[keep_indices]

        c, mh, mw = proto_masks.shape
        proto_flat = proto_masks.reshape(c, -1)
        masks_mat = 1.0 / (1.0 + np.exp(-np.dot(mask_coeffs, proto_flat)))
        masks_mat = masks_mat.reshape(-1, mh, mw)

        detections = []
        for i in range(len(boxes_orig)):
            box = boxes_orig[i]
            score = float(scores[i])
            mask_scaled = cv2.resize(masks_mat[i], (orig_w, orig_h), interpolation=cv2.INTER_LINEAR)
            mask_binary = (mask_scaled > 0.50).astype(np.uint8)

            bx1, by1, bx2, by2 = int(box[0]), int(box[1]), int(box[2]), int(box[3])
            box_mask = np.zeros_like(mask_binary)
            box_mask[by1:by2, bx1:bx2] = 1
            mask_binary = cv2.bitwise_and(mask_binary, box_mask)

            contours, _ = cv2.findContours(mask_binary, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
            polygon = None
            if len(contours) > 0:
                largest_c = max(contours, key=cv2.contourArea)
                if len(largest_c) > 2 and cv2.contourArea(largest_c) > 10:
                    polygon = largest_c.squeeze()
                    if polygon.ndim == 1:
                        polygon = polygon[np.newaxis, :]

            detections.append({
                "bbox": box.astype(np.float32),
                "polygon": polygon.astype(np.int32) if polygon is not None else None,
                "class_id": 0,
                "class_name": "Black-faced spoonbill",
                "score": score,
                "area": float((box[2] - box[0]) * (box[3] - box[1]))
            })
        return detections

    def _predict_onnx(
        self,
        img: np.ndarray,
        model_id: str,
        conf: float,
        iou: float,
        use_sahi: bool,
        slice_size: int,
        overlap: float
    ) -> Tuple[List[Dict], int]:
        session = self._get_onnx_session(model_id)
        input_name = session.get_inputs()[0].name
        output_names = [o.name for o in session.get_outputs()]
        img_h, img_w = img.shape[:2]

        if not use_sahi:
            blob, ratio, pad = self._preprocess_onnx(img, target_size=640)
            outputs = session.run(output_names, {input_name: blob})
            dets = self._postprocess_onnx(outputs[0], outputs[1], (img_h, img_w), ratio, pad, conf, iou)
            return dets, 1

        slice_coords = self._generate_slices(img_h, img_w, slice_size, overlap)
        all_detections = []
        for (sx1, sy1, sx2, sy2) in slice_coords:
            crop = img[sy1:sy2, sx1:sx2]
            ch, cw = crop.shape[:2]
            blob, ratio, pad = self._preprocess_onnx(crop, target_size=640)
            outputs = session.run(output_names, {input_name: blob})
            local_dets = self._postprocess_onnx(outputs[0], outputs[1], (ch, cw), ratio, pad, conf, iou)
            for d in local_dets:
                box = d["bbox"]
                poly = d["polygon"]
                g_box = np.array([box[0] + sx1, box[1] + sy1, box[2] + sx1, box[3] + sy1], dtype=np.float32)
                g_poly = None
                if poly is not None:
                    gp = poly.copy()
                    gp[:, 0] += sx1
                    gp[:, 1] += sy1
                    g_poly = gp
                all_detections.append({
                    "bbox": g_box,
                    "polygon": g_poly,
                    "class_id": 0,
                    "class_name": "Black-faced spoonbill",
                    "score": d["score"],
                    "area": float((g_box[2] - g_box[0]) * (g_box[3] - g_box[1]))
                })

        if all_detections:
            boxes = np.array([d["bbox"] for d in all_detections])
            scores = np.array([d["score"] for d in all_detections])
            keep_idx = self._nms_indices(boxes, scores, iou)
            all_detections = [all_detections[k] for k in keep_idx]

        return all_detections, len(slice_coords)

    def _predict_torch(
        self,
        img: np.ndarray,
        model_id: str,
        conf: float,
        iou: float,
        use_sahi: bool,
        slice_size: int,
        overlap: float
    ) -> Tuple[List[Dict], int]:
        model = self._get_torch_model(model_id)
        img_h, img_w = img.shape[:2]

        if not use_sahi:
            results = model.predict(
                source=img,
                conf=conf,
                iou=iou,
                imgsz=640,
                device=self.device,
                verbose=False,
                retina_masks=True
            )
            dets = self._extract_ultralytics_dets(results[0] if results else None, 0, 0)
            return dets, 1

        slice_coords = self._generate_slices(img_h, img_w, slice_size, overlap)
        all_detections = []
        batch_size = 4

        for i in range(0, len(slice_coords), batch_size):
            batch_slices = slice_coords[i:i + batch_size]
            batch_imgs = []
            for (sx1, sy1, sx2, sy2) in batch_slices:
                crop = img[sy1:sy2, sx1:sx2]
                ch, cw = crop.shape[:2]
                if ch < slice_size or cw < slice_size:
                    pad = np.zeros((slice_size, slice_size, 3), dtype=np.uint8)
                    pad[:ch, :cw] = crop
                    batch_imgs.append(pad)
                else:
                    batch_imgs.append(crop)

            results = model.predict(
                source=batch_imgs,
                conf=conf,
                iou=iou,
                imgsz=640,
                device=self.device,
                verbose=False,
                retina_masks=True
            )
            for r_idx, res in enumerate(results):
                sx1, sy1, _, _ = batch_slices[r_idx]
                all_detections.extend(self._extract_ultralytics_dets(res, sx1, sy1))

        if all_detections:
            boxes = np.array([d["bbox"] for d in all_detections])
            scores = np.array([d["score"] for d in all_detections])
            keep_idx = self._nms_indices(boxes, scores, iou)
            all_detections = [all_detections[k] for k in keep_idx]

        return all_detections, len(slice_coords)

    @staticmethod
    def _extract_ultralytics_dets(res, offset_x: int = 0, offset_y: int = 0) -> List[Dict]:
        if res is None or res.boxes is None or len(res.boxes) == 0:
            return []
        boxes = res.boxes.xyxy.cpu().numpy()
        scores = res.boxes.conf.cpu().numpy()
        masks_xy = res.masks.xy if (res.masks is not None and res.masks.xy is not None) else None

        dets = []
        for idx in range(len(boxes)):
            b = boxes[idx]
            g_box = np.array([b[0] + offset_x, b[1] + offset_y, b[2] + offset_x, b[3] + offset_y], dtype=np.float32)
            g_poly = None
            if masks_xy is not None and len(masks_xy) > idx and len(masks_xy[idx]) > 2:
                gp = masks_xy[idx].copy()
                gp[:, 0] += offset_x
                gp[:, 1] += offset_y
                g_poly = gp.astype(np.int32)

            dets.append({
                "bbox": g_box,
                "polygon": g_poly,
                "class_id": 0,
                "class_name": "Black-faced spoonbill",
                "score": float(scores[idx]),
                "area": float((g_box[2] - g_box[0]) * (g_box[3] - g_box[1]))
            })
        return dets

    def predict(
        self,
        img: np.ndarray,
        model_id: str = "v3_11l_onnx",
        conf: float = 0.25,
        iou: float = 0.45,
        use_sahi: bool = True,
        slice_size: int = 640,
        overlap: float = 0.20
    ) -> Dict:
        if model_id not in self.catalog or not self.catalog[model_id]["available"]:
            model_id = "v3_11l_onnx"

        info = self.catalog[model_id]
        img_h, img_w = img.shape[:2]

        # Ensure model is loaded into memory before starting latency timer
        if info["backend"] == "onnx":
            self._get_onnx_session(model_id)
            start_t = time.time()
            dets, total_slices = self._predict_onnx(img, model_id, conf, iou, use_sahi, slice_size, overlap)
        else:
            self._get_torch_model(model_id)
            start_t = time.time()
            dets, total_slices = self._predict_torch(img, model_id, conf, iou, use_sahi, slice_size, overlap)

        elapsed = time.time() - start_t
        mean_conf = float(np.mean([d["score"] for d in dets])) if dets else 0.0

        # Build YOLO normalized polygon lines (compatible with SAM / YOLO training format)
        yolo_lines = []
        for d in dets:
            poly = d.get("polygon")
            if poly is not None and len(poly) > 2:
                norm_coords = []
                for pt in poly:
                    nx = np.clip(pt[0] / img_w, 0.0, 1.0)
                    ny = np.clip(pt[1] / img_h, 0.0, 1.0)
                    norm_coords.append(f"{nx:.6f} {ny:.6f}")
                yolo_lines.append("0 " + " ".join(norm_coords))
            else:
                # Fallback to normalized bounding box polygon
                bx1, by1, bx2, by2 = d["bbox"]
                nx1, ny1, nx2, ny2 = bx1 / img_w, by1 / img_h, bx2 / img_w, by2 / img_h
                yolo_lines.append(f"0 {nx1:.6f} {ny1:.6f} {nx2:.6f} {ny1:.6f} {nx2:.6f} {ny2:.6f} {nx1:.6f} {ny2:.6f}")

        return {
            "mode": "SAHI (TILED)" if use_sahi else "DIRECT (SINGLE-PASS)",
            "model_id": model_id,
            "model_name": info["short_name"],
            "model_full_name": info["name"],
            "backend": info["backend"].upper(),
            "image_shape": (img_h, img_w, 3),
            "total_slices": total_slices,
            "slice_size": slice_size,
            "total_count": len(dets),
            "mean_confidence": round(mean_conf, 4),
            "detections": dets,
            "yolo_labels_txt": "\n".join(yolo_lines),
            "inference_time": elapsed
        }

    def visualize(
        self,
        image: np.ndarray,
        result: Dict,
        mask_alpha: float = 0.45,
        draw_boxes: bool = True,
        draw_masks: bool = True,
        draw_labels: bool = True,
        draw_hud: bool = True
    ) -> np.ndarray:
        vis = image.copy()
        overlay = image.copy()

        colors = [
            (0, 255, 128), (0, 215, 255), (255, 144, 30),
            (180, 105, 255), (255, 191, 0), (50, 205, 50)
        ]

        dets = result.get("detections", [])

        if draw_masks:
            for idx, det in enumerate(dets):
                poly = det.get("polygon")
                color = colors[idx % len(colors)]
                if poly is not None and len(poly) > 2:
                    cv2.fillPoly(overlay, [poly], color)
            cv2.addWeighted(overlay, mask_alpha, vis, 1.0 - mask_alpha, 0, vis)

        for idx, det in enumerate(dets):
            color = colors[idx % len(colors)]
            box = det["bbox"].astype(int)
            score = det["score"]
            poly = det.get("polygon")

            if draw_masks and poly is not None and len(poly) > 2:
                cv2.polylines(vis, [poly], isClosed=True, color=color, thickness=2, lineType=cv2.LINE_AA)

            if draw_boxes:
                cv2.rectangle(vis, (box[0], box[1]), (box[2], box[3]), color, 2, cv2.LINE_AA)

            if draw_labels:
                tag_text = f"#{idx+1} {score*100:.0f}%"
                (tw, th), _ = cv2.getTextSize(tag_text, cv2.FONT_HERSHEY_SIMPLEX, 0.50, 1)
                tag_y1 = max(0, box[1] - th - 6)
                tag_y2 = box[1]
                tag_x1 = box[0]
                tag_x2 = min(vis.shape[1], box[0] + tw + 8)
                cv2.rectangle(vis, (tag_x1, tag_y1), (tag_x2, tag_y2), color, -1)
                cv2.putText(vis, tag_text, (tag_x1 + 4, tag_y2 - 4),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.48, (0, 0, 0), 1, cv2.LINE_AA)

        if draw_hud:
            h, w = vis.shape[:2]
            hud_w = min(460, w - 24)
            hud_h = 96
            hud = vis.copy()
            cv2.rectangle(hud, (12, 12), (12 + hud_w, 12 + hud_h), (250, 250, 252), -1)
            cv2.addWeighted(hud, 0.88, vis, 0.12, 0, vis)
            cv2.rectangle(vis, (12, 12), (12 + hud_w, 12 + hud_h), (210, 214, 220), 1, cv2.LINE_AA)
            # Left accent bar (Terracotta BGR: 31, 130, 246)
            cv2.rectangle(vis, (12, 12), (16, 12 + hud_h), (31, 130, 246), -1)

            mode_str = result.get("mode", "SAHI")
            m_name = result.get("model_name", "11L")
            count = result.get("total_count", 0)
            slices = result.get("total_slices", 1)
            latency = result.get("inference_time", 0.0) * 1000

            cv2.putText(vis, f"{m_name}  |  {mode_str}", (26, 36),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.50, (82, 82, 91), 1, cv2.LINE_AA)
            cv2.putText(vis, f"Detected: {count} Spoonbills", (26, 62),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.62, (24, 24, 27), 2, cv2.LINE_AA)
            cv2.putText(vis, f"Tiles: {slices}  |  {latency:.1f} ms  |  Conf: {result.get('mean_confidence', 0)*100:.1f}%  |  {w}x{h}",
                        (26, 86), cv2.FONT_HERSHEY_SIMPLEX, 0.42, (113, 113, 122), 1, cv2.LINE_AA)

        return vis

    # =====================================================================
    # 4. GRAD-CAM / FEATURE ATTRIBUTION HEATMAP GENERATOR
    # =====================================================================
    def generate_gradcam_overlay(self, img: np.ndarray, model_id: str) -> np.ndarray:
        """
        Extracts spatial feature map energy (from P3 / CoordAtt or neck head)
        and blends a Jet colormap heatmap onto the original image.
        """
        h, w = img.shape[:2]
        if not HAS_TORCH:
            out = img.copy()
            cv2.rectangle(out, (10, 10), (450, 52), (15, 20, 28), -1)
            cv2.rectangle(out, (10, 10), (450, 52), (0, 215, 255), 1, cv2.LINE_AA)
            cv2.putText(out, "Grad-CAM requires PyTorch installation", (20, 36), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 255, 255), 1, cv2.LINE_AA)
            return out

        info = self.catalog.get(model_id)

        # If user selected an ONNX model, map it to its corresponding PyTorch model for layer hooks
        torch_id = model_id
        if not info or info["backend"] != "torch":
            torch_id = "ablation_baseline_l" if "11l" in model_id else "ablation_baseline_n"

        model = self._get_torch_model(torch_id)
        layers = list(model.model.model.children())

        # Find CoordAtt layer if present, else pick P3 high-res neck feature layer (index 16 or -2)
        target_layer = layers[-2]
        for lyr in layers:
            if lyr.__class__.__name__ == "CoordAtt":
                target_layer = lyr
                break

        activations = []
        def hook_fn(module, inp, out):
            activations.append(out)

        hook = target_layer.register_forward_hook(hook_fn)
        try:
            _ = model.predict(source=img, imgsz=640, device=self.device, verbose=False)
        finally:
            hook.remove()

        if not activations:
            return img.copy()

        act = activations[0]
        if isinstance(act, (list, tuple)):
            act = act[0]

        act_np = act.detach().cpu().squeeze().numpy()
        heatmap = np.mean(act_np, axis=0) if act_np.ndim == 3 else act_np
        heatmap = np.maximum(heatmap, 0)
        if heatmap.max() > 0:
            heatmap = heatmap / heatmap.max()

        heatmap_resized = cv2.resize(heatmap.astype(np.float32), (w, h), interpolation=cv2.INTER_CUBIC)
        hm_color = cv2.applyColorMap(np.uint8(255 * heatmap_resized), cv2.COLORMAP_JET)
        blended = cv2.addWeighted(img, 0.58, hm_color, 0.42, 0)

        # Add subtle badge
        cv2.rectangle(blended, (10, 10), (430, 52), (15, 20, 28), -1)
        cv2.rectangle(blended, (10, 10), (430, 52), (0, 215, 255), 1, cv2.LINE_AA)
        layer_tag = "CoordAtt @ P3 Hook" if target_layer.__class__.__name__ == "CoordAtt" else "Neck Feature Map Hook"
        cv2.putText(blended, f"Grad-CAM Attribution: {self.catalog[torch_id]['short_name']} ({layer_tag})",
                    (20, 36), cv2.FONT_HERSHEY_SIMPLEX, 0.48, (0, 255, 255), 1, cv2.LINE_AA)
        return blended
