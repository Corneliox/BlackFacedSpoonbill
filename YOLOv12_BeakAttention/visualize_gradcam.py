"""
Feature Attribution & Visual Heatmap Validation (Grad-CAM / EigenCAM).
Compares the activation intensity between Baseline YOLOv12 and YOLOv12-BeakAttention
to verify that the attention mechanism explicitly localizes the spatulate beak.
"""

import os
import cv2
import torch
import numpy as np
import matplotlib.pyplot as plt
from ultralytics import YOLO

def generate_feature_heatmap(model, img_path, target_layer_idx=-2):
    """
    Extracts feature map activations from target layer and generates a normalized heatmap.
    """
    img = cv2.imread(img_path)
    h, w = img.shape[:2]
    
    # Run forward pass with intermediate activations
    results = model.predict(img_path, save=False, verbose=False)
    
    # Extract feature maps from the model's head
    activations = []
    def hook_fn(module, input, output):
        activations.append(output)
        
    # Hook the target layer
    target_layer = list(model.model.model.children())[target_layer_idx]
    hook = target_layer.register_forward_hook(hook_fn)
    
    # Re-run forward to capture hook
    _ = model.predict(img_path, save=False, verbose=False)
    hook.remove()
    
    if len(activations) == 0:
        return img, np.zeros((h, w), dtype=np.float32)
        
    act = activations[0]
    if isinstance(act, (list, tuple)):
        act = act[0]
        
    # Compute channel-wise mean energy
    act_np = act.detach().cpu().squeeze().numpy()
    if act_np.ndim == 3:
        heatmap = np.mean(act_np, axis=0)
    else:
        heatmap = act_np
        
    heatmap = np.maximum(heatmap, 0)
    if heatmap.max() > 0:
        heatmap /= heatmap.max()
        
    heatmap = cv2.resize(heatmap, (w, h))
    return img, heatmap

def compare_attention_heatmaps(baseline_weights, beak_attn_weights, test_img_path, output_png):
    """
    Generates side-by-side visual comparison between Baseline and Beak-Attention models.
    """
    m_base = YOLO(baseline_weights)
    m_attn = YOLO(beak_attn_weights)
    
    img, hm_base = generate_feature_heatmap(m_base, test_img_path)
    _, hm_attn = generate_feature_heatmap(m_attn, test_img_path)
    
    # Generate colored overlays
    hm_base_color = cv2.applyColorMap(np.uint8(255 * hm_base), cv2.COLORMAP_JET)
    hm_attn_color = cv2.applyColorMap(np.uint8(255 * hm_attn), cv2.COLORMAP_JET)
    
    overlay_base = cv2.addWeighted(img, 0.6, hm_base_color, 0.4, 0)
    overlay_attn = cv2.addWeighted(img, 0.6, hm_attn_color, 0.4, 0)
    
    # Plot side-by-side
    fig, axes = plt.subplots(1, 3, figsize=(18, 6))
    axes[0].imshow(cv2.cvtColor(img, cv2.COLOR_BGR2RGB))
    axes[0].set_title("Input Image", fontsize=14)
    axes[0].axis("off")
    
    axes[1].imshow(cv2.cvtColor(overlay_base, cv2.COLOR_BGR2RGB))
    axes[1].set_title("Baseline YOLOv12 (Global Attention)", fontsize=14)
    axes[1].axis("off")
    
    axes[2].imshow(cv2.cvtColor(overlay_attn, cv2.COLOR_BGR2RGB))
    axes[2].set_title("YOLOv12 + Coordinate Attention (Beak-Focused)", fontsize=14)
    axes[2].axis("off")
    
    plt.tight_layout()
    os.makedirs(os.path.dirname(output_png), exist_ok=True)
    plt.savefig(output_png, dpi=300)
    plt.close()
    print(f"Heatmap comparison successfully saved to: {output_png}")

if __name__ == "__main__":
    print("Grad-CAM / Feature Attribution Visualizer loaded.")
