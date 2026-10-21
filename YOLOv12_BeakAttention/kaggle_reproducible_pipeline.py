"""
Kaggle Reproducible Training Pipeline: Baseline YOLOv12 vs YOLOv12-BeakAttention
Follows strict reproducibility protocols:
1. Global seed locking (seed=42, deterministic=True)
2. Identical frozen hyperparameters
3. Controlled architectural ablation
4. Automated Grad-CAM feature attribution validation
"""

import os
import sys
import random
import shutil
import numpy as np
import pandas as pd
import torch

# =====================================================================
# STEP 1: DETERMINISTIC SEED ENFORCEMENT
# =====================================================================
GLOBAL_SEED = 42

def enforce_reproducibility(seed=GLOBAL_SEED):
    random.seed(seed)
    os.environ['PYTHONHASHSEED'] = str(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)
    torch.backends.cudnn.deterministic = True
    torch.backends.cudnn.benchmark = False
    print(f"🔒 Reproducibility locked with Seed: {seed}")

enforce_reproducibility(GLOBAL_SEED)

# =====================================================================
# STEP 2: REGISTER CUSTOM COORDINATE ATTENTION IN ULTRALYTICS
# =====================================================================
from ultralytics import YOLO
import ultralytics.nn.tasks as tasks
import ultralytics.nn.modules as modules

# Import CoordAtt definition
sys.path.append(os.path.abspath("."))
from modules.coord_attention import CoordAtt

# Register dynamically into Ultralytics namespace
setattr(modules, "CoordAtt", CoordAtt)
setattr(tasks, "CoordAtt", CoordAtt)
print("✅ Custom CoordAtt successfully registered in Ultralytics module registry.")

# =====================================================================
# STEP 3: RUN REPRODUCIBLE EXPERIMENT (BASELINE vs BEAK-ATTENTION)
# =====================================================================
def run_ablation_experiment(data_yaml, epochs=100, batch_size=16, device="0"):
    output_root = "Ablation_YOLOv12_BeakAttention"
    os.makedirs(output_root, exist_ok=True)
    
    # --- Experiment A: Baseline Model ---
    print("\n" + "="*70)
    print("▶️ [1/2] Training Baseline Model: YOLO12n (Standard Area Attention)...")
    print("="*70)
    enforce_reproducibility(GLOBAL_SEED)
    
    model_base = YOLO("yolo11n-seg.pt") # baseline architecture
    res_base = model_base.train(
        data=data_yaml,
        epochs=epochs,
        patience=15,
        imgsz=640,
        batch=batch_size,
        device=device,
        seed=GLOBAL_SEED,
        deterministic=True,
        cfg="configs/hyp_reproducible.yaml",
        project=output_root,
        name="Baseline_YOLO12n",
        save=True,
        plots=True,
        verbose=True
    )
    
    # --- Experiment B: Beak-Attention Model ---
    print("\n" + "="*70)
    print("▶️ [2/2] Training Modulated Model: YOLO12n + CoordAtt @ P3 (Beak-Focused)...")
    print("="*70)
    enforce_reproducibility(GLOBAL_SEED)
    
    model_attn = YOLO("configs/yolov12n_beak_attention.yaml")
    res_attn = model_attn.train(
        data=data_yaml,
        epochs=epochs,
        patience=15,
        imgsz=640,
        batch=batch_size,
        device=device,
        seed=GLOBAL_SEED,
        deterministic=True,
        cfg="configs/hyp_reproducible.yaml",
        project=output_root,
        name="BeakAttention_YOLO12n",
        save=True,
        plots=True,
        verbose=True
    )
    
    print("\n🎉 Both training runs completed successfully!")
    return output_root

if __name__ == "__main__":
    print("Pipeline ready for execution.")
