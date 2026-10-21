"""
Coordinate Attention (CA) Module for Fine-Grained Morphological Feature Extraction.
Reference: Hou et al., "Coordinate Attention for Efficient Mobile Network Design", CVPR 2021.
Optimized for directional, high-aspect-ratio spatial features (e.g., spoonbill spatulate beak).
Compatible with Ultralytics automatic model parsing across all scales (n, s, m, l).
"""

import torch
import torch.nn as nn

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
