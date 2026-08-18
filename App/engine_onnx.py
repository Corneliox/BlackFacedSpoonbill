"""
Pure ONNX Runtime Sliced Instance Segmentation Engine for Black-faced Spoonbill.
Runs without PyTorch / Ultralytics dependencies.
Cross-platform compatible across Windows, Mac Intel (x86_64), and Mac Apple Silicon (ARM64).
"""

import os
import sys
import time
import math
from typing import List, Dict, Tuple, Optional, Union
import cv2
import numpy as np
import onnxruntime as ort


def get_base_dir() -> str:
    if getattr(sys, "frozen", False):
        return getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
    return os.path.dirname(os.path.abspath(__file__))


class SpoonbillOnnxEngine:
    def __init__(
        self,
        model_type: str = "11l",
        slice_size: int = 640,
        overlap_ratio: float = 0.20,
        conf_threshold: float = 0.25,
        iou_threshold: float = 0.45,
        models_dir: Optional[str] = None
    ):
        self.slice_height = slice_size
        self.slice_width = slice_size
        self.overlap_ratio = overlap_ratio
        self.conf_threshold = conf_threshold
        self.iou_threshold = iou_threshold

        self.base_dir = get_base_dir()
        self.models_dir = models_dir if models_dir else os.path.join(self.base_dir, "models")
        self.sessions: Dict[str, ort.InferenceSession] = {}
        self.active_model_type = model_type

        # Available execution providers (auto-selects CoreML on Mac, DirectML/CUDA on Windows, or CPU)
        available_providers = ort.get_available_providers()
        self.providers = []
        for p in ["CoreMLExecutionProvider", "DmlExecutionProvider", "CUDAExecutionProvider", "CPUExecutionProvider"]:
            if p in available_providers:
                self.providers.append(p)
        if not self.providers:
            self.providers = ["CPUExecutionProvider"]

        print(f"[ONNX Engine] Initialized with providers: {self.providers}")
        self.load_model(model_type)

    def _resolve_model_path(self, model_type: str) -> str:
        filename_map = {
            "11l": "best_spoonbill_11l.onnx",
            "11n": "best_spoonbill_11n.onnx"
        }
        fname = filename_map.get(model_type, "best_spoonbill_11l.onnx")

        # 1. Auto-reconstruct from multipart chunks (.part1, .part2, ...)
        for d in [self.models_dir, os.path.join(self.base_dir, "models"), self.base_dir]:
            if not os.path.isdir(d):
                continue
            target_p = os.path.join(d, fname)
            part1_p = os.path.join(d, f"{fname}.part1")
            if not os.path.exists(target_p) and os.path.exists(part1_p):
                print(f"[ONNX Engine] Reconstructing {fname} from binary parts...")
                parts = sorted(
                    [os.path.join(d, f) for f in os.listdir(d) if f.startswith(f"{fname}.part")],
                    key=lambda x: int(x.split(".part")[-1]) if x.split(".part")[-1].isdigit() else 0
                )
                with open(target_p, "wb") as outfile:
                    for part_f in parts:
                        with open(part_f, "rb") as infile:
                            outfile.write(infile.read())
                if os.path.exists(target_p):
                    print(f"[ONNX Engine] {fname} successfully reconstructed ({os.path.getsize(target_p):,} bytes).")
                    return target_p

        # 2. Auto-extract from .zip if needed
        for d in [self.models_dir, os.path.join(self.base_dir, "models"), self.base_dir]:
            if not os.path.isdir(d):
                continue
            target_p = os.path.join(d, fname)
            zip_p = target_p + ".zip"
            if not os.path.exists(target_p) and os.path.exists(zip_p):
                print(f"[ONNX Engine] Extracting {fname} from {zip_p}...")
                import zipfile
                with zipfile.ZipFile(zip_p, 'r') as zf:
                    zf.extractall(d)
                if os.path.exists(target_p):
                    return target_p

        candidates = [
            os.path.join(self.models_dir, fname),
            os.path.join(self.base_dir, "models", fname),
            os.path.join(self.base_dir, fname),
            rf"D:\~Ideas n Innovation\~~Taiwan\AU\Gilang\Segmentasi 26-8-17\NewModels Reinforced\11l\{fname}",
            rf"D:\~Ideas n Innovation\~~Taiwan\AU\Gilang\Segmentasi 26-8-17\NewModels Reinforced\11n\{fname}",
            rf"D:\~Ideas n Innovation\~~Taiwan\AU\Gilang\Segmentasi 26-8-17\NewModels Reinforced\11l\best_spoonbill_11l.onnx",
            rf"D:\~Ideas n Innovation\~~Taiwan\AU\Gilang\Segmentasi 26-8-17\NewModels Reinforced\11n\best.onnx"
        ]

        for c in candidates:
            if os.path.exists(c):
                return c

        raise FileNotFoundError(f"ONNX Model for {model_type} ({fname}) not found in search paths:\n" + "\n".join(candidates))

    def load_model(self, model_type: str):
        path = self._resolve_model_path(model_type)
        if model_type not in self.sessions:
            print(f"[ONNX Engine] Loading {model_type.upper()} model from {path}...")
            opts = ort.SessionOptions()
            opts.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
            opts.intra_op_num_threads = os.cpu_count() or 4
            self.sessions[model_type] = ort.InferenceSession(path, sess_options=opts, providers=self.providers)

        self.session = self.sessions[model_type]
        self.active_model_type = model_type
        self.input_name = self.session.get_inputs()[0].name
        self.output_names = [o.name for o in self.session.get_outputs()]

    def _preprocess(self, img: np.ndarray, target_size: int = 640) -> Tuple[np.ndarray, float, Tuple[int, int]]:
        """Preprocesses image with letterbox resize, normalization to [0, 1] RGB NCHW."""
        h, w = img.shape[:2]
        r = min(target_size / h, target_size / w)
        new_unpad = (int(round(w * r)), int(round(h * r)))
        dw, dh = target_size - new_unpad[0], target_size - new_unpad[1]
        dw, dh = dw / 2, dh / 2

        if (w, h) != new_unpad:
            img_resized = cv2.resize(img, new_unpad, interpolation=cv2.INTER_LINEAR)
        else:
            img_resized = img

        top, bottom = int(round(dh - 0.1)), int(round(dh + 0.1))
        left, right = int(round(dw - 0.1)), int(round(dw + 0.1))
        img_padded = cv2.copyMakeBorder(img_resized, top, bottom, left, right, cv2.BORDER_CONSTANT, value=(114, 114, 114))

        # Convert BGR to RGB, Normalize 0-1, Transpose to CHW
        blob = cv2.cvtColor(img_padded, cv2.COLOR_BGR2RGB).astype(np.float32) / 255.0
        blob = np.transpose(blob, (2, 0, 1))[np.newaxis, ...]  # (1, 3, 640, 640)
        return blob, r, (dw, dh)

    def _postprocess_raw(
        self,
        output0: np.ndarray,
        output1: np.ndarray,
        orig_shape: Tuple[int, int],
        ratio: float,
        pad: Tuple[float, float],
        conf_thresh: float,
        iou_thresh: float
    ) -> List[Dict]:
        """Vectorized postprocessing of YOLOv11-seg ONNX tensor outputs."""
        # output0: (1, 37, 8400) -> transpose to (8400, 37)
        preds = np.transpose(output0[0], (1, 0))  # (8400, 37)
        boxes_xywh = preds[:, :4]                 # (8400, 4)
        scores = preds[:, 4]                      # (8400,)
        mask_coeffs = preds[:, 5:]                # (8400, 32)
        proto_masks = output1[0]                  # (32, 160, 160)

        # Filter by confidence
        mask_conf = scores >= conf_thresh
        if not np.any(mask_conf):
            return []

        boxes_xywh = boxes_xywh[mask_conf]
        scores = scores[mask_conf]
        mask_coeffs = mask_coeffs[mask_conf]

        # Convert xywh to xyxy in 640x640 space
        x1 = boxes_xywh[:, 0] - boxes_xywh[:, 2] / 2
        y1 = boxes_xywh[:, 1] - boxes_xywh[:, 3] / 2
        x2 = boxes_xywh[:, 0] + boxes_xywh[:, 2] / 2
        y2 = boxes_xywh[:, 1] + boxes_xywh[:, 3] / 2

        # Transform back to original image space
        dw, dh = pad
        orig_h, orig_w = orig_shape
        x1 = np.clip((x1 - dw) / ratio, 0, orig_w)
        y1 = np.clip((y1 - dh) / ratio, 0, orig_h)
        x2 = np.clip((x2 - dw) / ratio, 0, orig_w)
        y2 = np.clip((y2 - dh) / ratio, 0, orig_h)
        boxes_orig = np.column_stack([x1, y1, x2, y2])

        # Apply Fast NMS
        keep_indices = self._nms_indices(boxes_orig, scores, iou_thresh)
        if len(keep_indices) == 0:
            return []

        boxes_orig = boxes_orig[keep_indices]
        scores = scores[keep_indices]
        mask_coeffs = mask_coeffs[keep_indices]

        # Decode masks: MatMul (N, 32) x (32, 160x160) -> (N, 160, 160)
        c, mh, mw = proto_masks.shape
        proto_flat = proto_masks.reshape(c, -1)
        masks_mat = np.dot(mask_coeffs, proto_flat)
        masks_mat = 1.0 / (1.0 + np.exp(-masks_mat))  # Sigmoid
        masks_mat = masks_mat.reshape(-1, mh, mw)

        detections = []
        for i in range(len(boxes_orig)):
            box = boxes_orig[i]
            score = float(scores[i])

            # Resample mask to original crop
            mask_raw = masks_mat[i]
            # Convert 160x160 back to 640 space coordinate bounds
            gx1_640 = (box[0] * ratio + dw) / 4.0
            gy1_640 = (box[1] * ratio + dh) / 4.0
            gx2_640 = (box[2] * ratio + dw) / 4.0
            gy2_640 = (box[3] * ratio + dh) / 4.0

            # Scale mask to original image size
            mask_scaled = cv2.resize(mask_raw, (orig_w, orig_h), interpolation=cv2.INTER_LINEAR)
            mask_binary = (mask_scaled > 0.50).astype(np.uint8)

            # Crop mask to box
            bx1, by1, bx2, by2 = int(box[0]), int(box[1]), int(box[2]), int(box[3])
            box_mask = np.zeros_like(mask_binary)
            box_mask[by1:by2, bx1:bx2] = 1
            mask_binary = cv2.bitwise_and(mask_binary, box_mask)

            # Extract contours
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

    def _generate_slices(self, img_h: int, img_w: int) -> List[Tuple[int, int, int, int]]:
        if img_h <= self.slice_height and img_w <= self.slice_width:
            return [(0, 0, img_w, img_h)]
        step_y = max(1, int(self.slice_height * (1.0 - self.overlap_ratio)))
        step_x = max(1, int(self.slice_width * (1.0 - self.overlap_ratio)))

        y_coords = list(range(0, img_h - self.slice_height + 1, step_y))
        if not y_coords or y_coords[-1] + self.slice_height < img_h:
            y_coords.append(max(0, img_h - self.slice_height))

        x_coords = list(range(0, img_w - self.slice_width + 1, step_x))
        if not x_coords or x_coords[-1] + self.slice_width < img_w:
            x_coords.append(max(0, img_w - self.slice_width))

        y_coords = sorted(list(set(y_coords)))
        x_coords = sorted(list(set(x_coords)))

        slices = []
        for y in y_coords:
            for x in x_coords:
                slices.append((x, y, min(img_w, x + self.slice_width), min(img_h, y + self.slice_height)))
        return slices

    def predict(
        self,
        image_input: Union[str, np.ndarray],
        conf: Optional[float] = None,
        iou: Optional[float] = None,
        use_sahi: bool = True
    ) -> Dict:
        start_time = time.time()
        conf = conf if conf is not None else self.conf_threshold
        iou = iou if iou is not None else self.iou_threshold

        if isinstance(image_input, str):
            img = cv2.imread(image_input)
            if img is None:
                raise ValueError(f"Failed to load image: {image_input}")
        elif isinstance(image_input, np.ndarray):
            img = image_input.copy()
        else:
            raise TypeError("image_input must be path string or numpy ndarray")

        img_h, img_w = img.shape[:2]

        if not use_sahi:
            # DIRECT SINGLE-PASS FULL IMAGE
            blob, ratio, pad = self._preprocess(img, target_size=640)
            outputs = self.session.run(self.output_names, {self.input_name: blob})
            detections = self._postprocess_raw(outputs[0], outputs[1], (img_h, img_w), ratio, pad, conf, iou)
            elapsed = time.time() - start_time
            return {
                "mode": "DIRECT (NO SAHI)",
                "model_type": self.active_model_type.upper(),
                "image_shape": (img_h, img_w, 3),
                "total_slices": 1,
                "total_count": len(detections),
                "detections": detections,
                "inference_time": elapsed
            }

        # SAHI TILED SLICING
        slice_coords = self._generate_slices(img_h, img_w)
        all_detections = []

        for (sx1, sy1, sx2, sy2) in slice_coords:
            crop = img[sy1:sy2, sx1:sx2]
            ch, cw = crop.shape[:2]
            blob, ratio, pad = self._preprocess(crop, target_size=self.slice_height)
            outputs = self.session.run(self.output_names, {self.input_name: blob})
            local_dets = self._postprocess_raw(outputs[0], outputs[1], (ch, cw), ratio, pad, conf, iou)

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

        # Global NMS Deduplication
        if all_detections:
            boxes = np.array([d["bbox"] for d in all_detections])
            scores = np.array([d["score"] for d in all_detections])
            keep_idx = self._nms_indices(boxes, scores, iou)
            dedup_detections = [all_detections[k] for k in keep_idx]
        else:
            dedup_detections = []

        elapsed = time.time() - start_time
        return {
            "mode": "SAHI (TILED)",
            "model_type": self.active_model_type.upper(),
            "image_shape": (img_h, img_w, 3),
            "total_slices": len(slice_coords),
            "total_count": len(dedup_detections),
            "detections": dedup_detections,
            "inference_time": elapsed
        }

    def visualize(
        self,
        image: np.ndarray,
        result: Dict,
        mask_alpha: float = 0.45,
        draw_boxes: bool = True,
        draw_masks: bool = True,
        draw_labels: bool = True
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
            cls_name = det["class_name"]
            poly = det.get("polygon")

            if draw_masks and poly is not None and len(poly) > 2:
                cv2.polylines(vis, [poly], isClosed=True, color=color, thickness=2, lineType=cv2.LINE_AA)

            if draw_boxes:
                cv2.rectangle(vis, (box[0], box[1]), (box[2], box[3]), color, 2, cv2.LINE_AA)

            if draw_labels:
                tag_text = f"#{idx+1} {cls_name} {score:.2f}"
                (tw, th), _ = cv2.getTextSize(tag_text, cv2.FONT_HERSHEY_SIMPLEX, 0.50, 1)
                tag_y1 = max(0, box[1] - th - 6)
                tag_y2 = box[1]
                tag_x1 = box[0]
                tag_x2 = min(vis.shape[1], box[0] + tw + 8)
                cv2.rectangle(vis, (tag_x1, tag_y1), (tag_x2, tag_y2), color, -1)
                cv2.putText(vis, tag_text, (tag_x1 + 4, tag_y2 - 4),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.50, (0, 0, 0), 1, cv2.LINE_AA)

        # HUD Analytics Badge
        h, w = vis.shape[:2]
        hud_w = min(460, w - 20)
        hud_h = 108
        hud = vis.copy()
        cv2.rectangle(hud, (10, 10), (10 + hud_w, 10 + hud_h), (20, 20, 20), -1)
        cv2.addWeighted(hud, 0.80, vis, 0.20, 0, vis)
        cv2.rectangle(vis, (10, 10), (10 + hud_w, 10 + hud_h), (0, 215, 255), 2, cv2.LINE_AA)

        mode_str = result.get("mode", "SAHI (TILED)")
        m_type = result.get("model_type", "11L")
        count = result.get("total_count", 0)
        slices = result.get("total_slices", 1)
        latency = result.get("inference_time", 0.0) * 1000

        cv2.putText(vis, f"ONNX RUNTIME [{m_type}] - {mode_str}", (20, 32),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.52, (0, 255, 255), 2, cv2.LINE_AA)
        cv2.putText(vis, f"Total Spoonbills Detected : {count} Birds", (20, 58),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.60, (0, 255, 128), 2, cv2.LINE_AA)
        if "DIRECT" in mode_str:
            cv2.putText(vis, f"Direct Full Frame (1 Pass) | Latency: {latency:.1f} ms", (20, 82),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.45, (220, 220, 220), 1, cv2.LINE_AA)
        else:
            cv2.putText(vis, f"Slices: {slices} ({self.slice_width}x{self.slice_height}) | Latency: {latency:.1f} ms", (20, 82),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.45, (220, 220, 220), 1, cv2.LINE_AA)

        cv2.putText(vis, f"Resolution: {w}x{h} px | Multi-Platform Lean Engine", (20, 100),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.40, (180, 180, 180), 1, cv2.LINE_AA)

        return vis
