/**
 * Spoonbill Studio v4.1 (Experimental) — Photoshop / Lightroom / Warehouse Edition
 * Features Non-Destructive Customizable Info Label (HUD) & Dedicated Inspector Tab
 */

document.addEventListener('DOMContentLoaded', () => {
  // --- STATE ---
  let modelsCatalog = [];
  let samplesCatalog = [];
  let benchmarkRows = [];

  let currentFile = null;
  let currentSampleName = null;
  let baseImg = new Image();
  let annotatedImg = new Image();
  let latestResult = null;
  let highlightedBBox = null;

  let showMasks = true;
  let showBoxes = true;
  let showBirdTags = true;
  let activeView = 'studio'; // 'studio', 'compare', 'benchmark'

  // Non-Destructive Info HUD Configuration (v4.1 Experimental)
  const hudConfig = {
    visible: true,
    renderMode: 'floating', // 'floating' (DOM overlay) or 'canvas' (pinned to image space)
    position: 'top-left',   // 'top-left', 'top-right', 'bottom-left', 'bottom-right', 'custom'
    customX: null,
    customY: null,
    theme: 'dark',          // 'dark' (sleek studio glass default), 'light', 'minimal'
    opacity: 0.92,
    scale: 1.00,
    customNote: '',
    bakeOnExport: false,
    fields: {
      model: true,
      mode: true,
      count: true,
      tiles: true,
      latency: true,
      conf: true,
      res: true
    }
  };

  // Canvas Pan & Zoom
  const canvas = document.getElementById('studio-canvas');
  const ctx = canvas.getContext('2d');
  const viewportStudio = document.getElementById('viewport-studio');
  let zoom = 1.0;
  let panX = 0;
  let panY = 0;
  let isDragging = false;
  let dragStartX = 0;
  let dragStartY = 0;

  // Floating HUD Dragging State
  let isDraggingHud = false;
  let hudDragOffsetX = 0;
  let hudDragOffsetY = 0;

  // --- DOM REFERENCES ---
  const topFilename = document.getElementById('top-filename');
  const fileInput = document.getElementById('file-input');
  const btnToggleFilmstrip = document.getElementById('btn-toggle-filmstrip');
  const filmstrip = document.getElementById('ps-filmstrip');
  const filmstripTrack = document.getElementById('filmstrip-track');
  const fsCount = document.getElementById('fs-count');

  const viewportCompare = document.getElementById('viewport-compare');
  const viewportBenchmark = document.getElementById('viewport-benchmark');
  const toolstrip = document.getElementById('ps-toolstrip');
  const inspector = document.getElementById('ps-inspector');

  const toolPan = document.getElementById('tool-pan');
  const toolFit = document.getElementById('tool-fit');
  const tool100 = document.getElementById('tool-100');
  const toolMask = document.getElementById('tool-mask');
  const toolBox = document.getElementById('tool-box');
  const toolTag = document.getElementById('tool-tag');
  const toolHud = document.getElementById('tool-hud');

  const hudRes = document.getElementById('hud-res');
  const hudZoom = document.getElementById('hud-zoom');
  const hudTime = document.getElementById('hud-time');
  const psBusy = document.getElementById('ps-busy');
  const psBusyText = document.getElementById('ps-busy-text');

  // Floating Info HUD DOM Elements
  const floatingInfoHud = document.getElementById('floating-info-hud');
  const btnHudCustomize = document.getElementById('btn-hud-customize');
  const btnHudHide = document.getElementById('btn-hud-hide');
  const btnHudRestore = document.getElementById('btn-hud-restore');

  const hudRowTop = document.getElementById('hud-row-top');
  const infoHudModel = document.getElementById('info-hud-model');
  const infoHudPipe = document.getElementById('info-hud-pipe');
  const infoHudMode = document.getElementById('info-hud-mode');
  const hudRowCount = document.getElementById('hud-row-count');
  const infoHudCount = document.getElementById('info-hud-count');
  const hudRowMeta = document.getElementById('hud-row-meta');
  const infoHudTiles = document.getElementById('info-hud-tiles');
  const sepTiles = document.getElementById('sep-tiles');
  const infoHudLatency = document.getElementById('info-hud-latency');
  const sepLatency = document.getElementById('sep-latency');
  const infoHudConf = document.getElementById('info-hud-conf');
  const sepConf = document.getElementById('sep-conf');
  const infoHudRes = document.getElementById('info-hud-res');
  const hudRowNote = document.getElementById('hud-row-note');

  // Inspector Tabs & Controls
  const tabBtnDetect = document.getElementById('tab-btn-detect');
  const tabBtnHud = document.getElementById('tab-btn-hud');
  const itabDetect = document.getElementById('itab-detect');
  const itabHud = document.getElementById('itab-hud');

  const birdCountHero = document.getElementById('bird-count-hero');
  const btnRunDetect = document.getElementById('btn-run-detect');
  const selectModel = document.getElementById('select-model');
  const badgeTech = document.getElementById('badge-tech');
  const badgeTarget = document.getElementById('badge-target');

  const rngConf = document.getElementById('rng-conf');
  const confValText = document.getElementById('conf-val-text');
  const chkSahi = document.getElementById('chk-sahi');
  const rngTile = document.getElementById('rng-tile');
  const tileValText = document.getElementById('tile-val-text');
  const rngOverlap = document.getElementById('rng-overlap');
  const overlapValText = document.getElementById('overlap-val-text');
  const rngIou = document.getElementById('rng-iou');
  const iouValText = document.getElementById('iou-val-text');

  // Info Label Customizer Controls
  const chkHudVisible = document.getElementById('chk-hud-visible');
  const chkBirdTags = document.getElementById('chk-bird-tags');
  const selectHudRenderMode = document.getElementById('select-hud-render-mode');
  const btnHudResetPos = document.getElementById('btn-hud-reset-pos');
  const chkFieldModel = document.getElementById('chk-field-model');
  const chkFieldMode = document.getElementById('chk-field-mode');
  const chkFieldCount = document.getElementById('chk-field-count');
  const chkFieldTiles = document.getElementById('chk-field-tiles');
  const chkFieldLatency = document.getElementById('chk-field-latency');
  const chkFieldConf = document.getElementById('chk-field-conf');
  const chkFieldRes = document.getElementById('chk-field-res');
  const txtHudNote = document.getElementById('txt-hud-note');
  const selectHudTheme = document.getElementById('select-hud-theme');
  const rngHudOpacity = document.getElementById('rng-hud-opacity');
  const hudOpacityVal = document.getElementById('hud-opacity-val');
  const rngHudScale = document.getElementById('rng-hud-scale');
  const hudScaleVal = document.getElementById('hud-scale-val');
  const chkExportBakeHud = document.getElementById('chk-export-bake-hud');

  const instanceList = document.getElementById('instance-list');
  const instancesCountTab = document.getElementById('instances-count-tab');
  const btnExportImg = document.getElementById('btn-export-img');
  const btnExportCsv = document.getElementById('btn-export-csv');
  const btnExportYolo = document.getElementById('btn-export-yolo');

  // --- HARDWARE & ACCELERATION STATE ---
  let activeHardwareMode = 'cpu';
  let hardwareInfo = null;

  const btnOpenHwModal = document.getElementById('btn-open-hw-modal');
  const hwDot = document.getElementById('hw-dot');
  const hwLabel = document.getElementById('hw-label');
  const hwTag = document.getElementById('hw-tag');
  const hwModal = document.getElementById('hw-modal');
  const btnCloseHwModal = document.getElementById('btn-close-hw-modal');
  const hwStatusIcon = document.getElementById('hw-status-icon');
  const hwStatusTitle = document.getElementById('hw-status-title');
  const hwStatusDesc = document.getElementById('hw-status-desc');
  const hwValOs = document.getElementById('hw-val-os');
  const hwValCpu = document.getElementById('hw-val-cpu');
  const hwValGpus = document.getElementById('hw-val-gpus');
  const hwValCuda = document.getElementById('hw-val-cuda');
  const hwValMode = document.getElementById('hw-val-mode');
  const hwValRec = document.getElementById('hw-val-rec');
  const btnSetCpuMode = document.getElementById('btn-set-cpu-mode');
  const btnSetCudaMode = document.getElementById('btn-set-cuda-mode');

  async function fetchHardwareInfo() {
    try {
      const res = await fetch('/api/hardware/probe');
      const data = await res.json();
      activeHardwareMode = data.active_mode || 'cpu';
      hardwareInfo = data.hw_info || {};
      updateHardwarePill();
      return data;
    } catch (err) {
      console.warn('Hardware probe error:', err);
      return null;
    }
  }

  function updateHardwarePill() {
    if (!hwDot || !hwLabel || !hwTag) return;
    if (activeHardwareMode === 'cuda') {
      hwDot.className = 'hw-dot dot-cuda';
      hwLabel.textContent = 'NVIDIA CUDA Mode';
      hwTag.textContent = 'Active';
    } else if (activeHardwareMode === 'mps') {
      hwDot.className = 'hw-dot dot-cuda';
      hwLabel.textContent = 'Apple Metal Mode';
      hwTag.textContent = 'Active';
    } else {
      hwDot.className = 'hw-dot dot-cpu';
      hwLabel.textContent = 'CPU Safe Mode';
      hwTag.textContent = 'Inspect';
    }
  }

  function populateHardwareModal() {
    if (!hardwareInfo) return;
    const h = hardwareInfo;

    if (hwValOs) hwValOs.textContent = `${h.system_os || 'OS'} (${h.arch || 'x64'})`;
    if (hwValCpu) hwValCpu.textContent = `${h.cpu_name || 'Generic CPU'} · ${h.cpu_cores || 4} Cores`;

    if (hwValGpus) {
      if (h.physical_gpus && h.physical_gpus.length > 0) {
        hwValGpus.textContent = h.physical_gpus.join(', ');
      } else {
        hwValGpus.textContent = 'Integrated Graphics / CPU';
      }
    }

    if (hwValCuda) {
      if (h.cuda_available) {
        hwValCuda.textContent = `Ready (${h.cuda_device_name || 'NVIDIA GPU'}, ${h.cuda_vram_gb || 0} GB)`;
        hwValCuda.style.color = '#10B981';
      } else if (h.has_nvidia_hardware) {
        hwValCuda.textContent = 'NVIDIA Detected (PyTorch CUDA Missing)';
        hwValCuda.style.color = '#F59E0B';
      } else if (h.mps_available) {
        hwValCuda.textContent = 'Apple Silicon Metal (MPS) Supported';
        hwValCuda.style.color = '#3B82F6';
      } else {
        hwValCuda.textContent = 'No Discrete NVIDIA GPU / CUDA';
        hwValCuda.style.color = '#A1A1AA';
      }
    }

    if (hwValMode) {
      hwValMode.textContent = activeHardwareMode === 'cuda' ? 'NVIDIA GPU Acceleration' :
                              activeHardwareMode === 'mps' ? 'Apple Silicon Metal (MPS)' : 'CPU Safe Mode (ONNX)';
    }
    if (hwValRec) {
      hwValRec.textContent = activeHardwareMode === 'cpu' ? '11N Nano (<60ms CPU Fast)' : '11L Large / BeakAttn';
    }

    if (hwStatusTitle) hwStatusTitle.textContent = h.status_label || 'Status Akselerasi';
    if (hwStatusDesc) hwStatusDesc.textContent = h.description || 'Pemeriksaan status perangkat selesai.';
    if (hwStatusIcon) hwStatusIcon.textContent = h.cuda_available ? 'GPU' : h.has_nvidia_hardware ? 'NV' : 'CPU';

    if (btnSetCudaMode) {
      if (h.can_enable_gpu) {
        btnSetCudaMode.disabled = false;
        if (activeHardwareMode === 'cuda' || activeHardwareMode === 'mps') {
          btnSetCudaMode.textContent = 'Akselerasi GPU Sedang Aktif';
          btnSetCudaMode.disabled = true;
        } else {
          btnSetCudaMode.textContent = 'Aktifkan NVIDIA CUDA Mode';
        }
      } else {
        btnSetCudaMode.disabled = true;
        btnSetCudaMode.textContent = 'NVIDIA CUDA Tidak Tersedia (CPU Only)';
      }
    }
  }

  function openHardwareModal() {
    if (hwModal) hwModal.style.display = 'flex';
    fetchHardwareInfo().then(() => {
      populateHardwareModal();
    });
  }

  function closeHardwareModal() {
    if (hwModal) hwModal.style.display = 'none';
  }

  async function switchHardwareMode(targetMode) {
    try {
      if (targetMode === 'cuda' && btnSetCudaMode) {
        btnSetCudaMode.textContent = 'Memeriksa & Mengalihkan...';
        btnSetCudaMode.disabled = true;
      }

      const fd = new FormData();
      fd.append('mode', targetMode);
      const res = await fetch('/api/hardware/switch_device', { method: 'POST', body: fd });
      const data = await res.json();

      if (!data.success) {
        alert(data.message || 'Gagal mengubah mode akselerasi.');
        if (targetMode === 'cuda' && btnSetCudaMode) {
          btnSetCudaMode.textContent = 'Aktifkan NVIDIA CUDA Mode';
          btnSetCudaMode.disabled = false;
        }
        return;
      }

      activeHardwareMode = data.active_mode;
      if (data.hw_info) hardwareInfo = data.hw_info;
      updateHardwarePill();
      populateHardwareModal();

      const modelsRes = await fetch('/api/models').then(r => r.json());
      modelsCatalog = modelsRes.models || [];
      populateModelPicker();

      setTimeout(() => {
        closeHardwareModal();
      }, 600);
    } catch (e) {
      console.error('Error switching hardware mode:', e);
      alert('Terjadi kesalahan saat beralih mode akselerasi.');
    }
  }

  function initHardwareInspector() {
    if (btnOpenHwModal) btnOpenHwModal.addEventListener('click', openHardwareModal);
    if (btnCloseHwModal) btnCloseHwModal.addEventListener('click', closeHardwareModal);
    if (hwModal) {
      hwModal.addEventListener('click', (e) => {
        if (e.target === hwModal) closeHardwareModal();
      });
    }
    if (btnSetCpuMode) {
      btnSetCpuMode.addEventListener('click', () => switchHardwareMode('cpu'));
    }
    if (btnSetCudaMode) {
      btnSetCudaMode.addEventListener('click', () => switchHardwareMode('cuda'));
    }
  }

  // --- INSPECTOR TAB SWITCHING (MODEL & SAHI vs INFO LABEL) ---
  function triggerHudPulse() {
    if (!floatingInfoHud) return;
    floatingInfoHud.classList.remove('pulse-highlight');
    void floatingInfoHud.offsetWidth;
    floatingInfoHud.classList.add('pulse-highlight');
    setTimeout(() => {
      if (floatingInfoHud) floatingInfoHud.classList.remove('pulse-highlight');
    }, 1000);
  }

  function switchInspectorTab(tabName) {
    const isDetect = (tabName === 'detect');
    tabBtnDetect.classList.toggle('active', isDetect);
    tabBtnHud.classList.toggle('active', !isDetect);
    itabDetect.classList.toggle('hidden', !isDetect);
    itabHud.classList.toggle('hidden', isDetect);
    if (!isDetect && hudConfig.visible) {
      triggerHudPulse();
    }
  }

  tabBtnDetect.addEventListener('click', () => switchInspectorTab('detect'));
  tabBtnHud.addEventListener('click', () => switchInspectorTab('hud'));

  // --- NON-DESTRUCTIVE INFO HUD ENGINE (V4.1 EXPERIMENTAL) ---
  function getActiveTelemetryData() {
    if (latestResult) {
      const h = latestResult.image_shape ? latestResult.image_shape[0] : (baseImg.height || 0);
      const w = latestResult.image_shape ? latestResult.image_shape[1] : (baseImg.width || 0);
      return {
        modelName: latestResult.model_name || '11N V3 (ONNX)',
        modeStr: latestResult.mode || (chkSahi.checked ? 'SAHI (TILED)' : 'DIRECT (SINGLE-PASS)'),
        count: latestResult.total_count || 0,
        slices: latestResult.total_slices || 1,
        latencyMs: latestResult.inference_time_ms || 0.0,
        confPct: ((latestResult.mean_confidence || 0) * 100).toFixed(1),
        resStr: `${w}×${h}`
      };
    }
    const selModel = modelsCatalog.find(x => x.id === selectModel.value);
    return {
      modelName: selModel ? selModel.short_name : '11N V3 (ONNX)',
      modeStr: chkSahi.checked ? 'SAHI (TILED)' : 'DIRECT (SINGLE-PASS)',
      count: 0,
      slices: 1,
      latencyMs: 0.0,
      confPct: '0.0',
      resStr: baseImg.width ? `${baseImg.width}×${baseImg.height}` : '0×0'
    };
  }

  function syncInfoHudUI() {
    const t = getActiveTelemetryData();
    const f = hudConfig.fields;

    // Sync checkboxes & toolbar buttons
    chkHudVisible.checked = hudConfig.visible;
    toolHud.classList.toggle('active', hudConfig.visible);
    chkBirdTags.checked = showBirdTags;
    toolTag.classList.toggle('active', showBirdTags);

    // Populate floating DOM HUD text & field visibility
    infoHudModel.textContent = t.modelName;
    infoHudModel.classList.toggle('hidden', !f.model);

    infoHudMode.textContent = t.modeStr;
    infoHudMode.classList.toggle('hidden', !f.mode);

    infoHudPipe.classList.toggle('hidden', !(f.model && f.mode));
    hudRowTop.classList.toggle('hidden', !(f.model || f.mode));

    infoHudCount.textContent = `${t.count}`;
    hudRowCount.classList.toggle('hidden', !f.count);

    infoHudTiles.textContent = `Tiles: ${t.slices}`;
    infoHudTiles.classList.toggle('hidden', !f.tiles);

    infoHudLatency.textContent = `${t.latencyMs} ms`;
    infoHudLatency.classList.toggle('hidden', !f.latency);

    infoHudConf.textContent = `Conf: ${t.confPct}%`;
    infoHudConf.classList.toggle('hidden', !f.conf);

    infoHudRes.textContent = t.resStr;
    infoHudRes.classList.toggle('hidden', !f.res);

    // Dynamic separators in metadata row
    const visibleMeta = [
      { el: infoHudTiles, sep: sepTiles, on: f.tiles },
      { el: infoHudLatency, sep: sepLatency, on: f.latency },
      { el: infoHudConf, sep: sepConf, on: f.conf },
      { el: infoHudRes, sep: null, on: f.res }
    ];
    for (let i = 0; i < visibleMeta.length; i++) {
      if (visibleMeta[i].sep) {
        const hasFollowing = visibleMeta.slice(i + 1).some(m => m.on);
        visibleMeta[i].sep.classList.toggle('hidden', !(visibleMeta[i].on && hasFollowing));
      }
    }
    const anyMeta = visibleMeta.some(m => m.on);
    hudRowMeta.classList.toggle('hidden', !anyMeta);

    // Custom survey note
    const noteText = (hudConfig.customNote || '').trim();
    if (noteText.length > 0) {
      hudRowNote.textContent = noteText;
      hudRowNote.classList.remove('hidden');
    } else {
      hudRowNote.textContent = '';
      hudRowNote.classList.add('hidden');
    }

    // Theme & Opacity & Scale
    floatingInfoHud.classList.remove('theme-light', 'theme-dark', 'theme-minimal');
    floatingInfoHud.classList.add(`theme-${hudConfig.theme}`);

    const alpha = hudConfig.opacity;
    if (hudConfig.theme === 'light') {
      floatingInfoHud.style.backgroundColor = `rgba(252, 252, 250, ${alpha})`;
    } else if (hudConfig.theme === 'dark') {
      floatingInfoHud.style.backgroundColor = `rgba(20, 20, 24, ${alpha})`;
    } else {
      floatingInfoHud.style.backgroundColor = `rgba(16, 16, 18, ${alpha})`;
    }
    floatingInfoHud.style.transform = `scale(${hudConfig.scale})`;

    // Position classes or custom dragged coordinates
    floatingInfoHud.classList.remove('pos-top-left', 'pos-top-right', 'pos-bottom-left', 'pos-bottom-right');
    if (hudConfig.position === 'custom' && hudConfig.customX !== null && hudConfig.customY !== null) {
      floatingInfoHud.style.left = `${hudConfig.customX}px`;
      floatingInfoHud.style.top = `${hudConfig.customY}px`;
      floatingInfoHud.style.right = 'auto';
      floatingInfoHud.style.bottom = 'auto';
      floatingInfoHud.style.transformOrigin = 'top left';
    } else {
      floatingInfoHud.style.left = '';
      floatingInfoHud.style.top = '';
      floatingInfoHud.style.right = '';
      floatingInfoHud.style.bottom = '';
      floatingInfoHud.style.transformOrigin = '';
      const posClass = `pos-${hudConfig.position || 'top-left'}`;
      floatingInfoHud.classList.add(posClass);
    }

    // Sync active corner pill
    document.querySelectorAll('.pos-pill').forEach(p => {
      p.classList.toggle('active', p.getAttribute('data-pos') === hudConfig.position);
    });

    // Show/hide floating DOM vs Canvas-pinned mode vs Restore Pill
    const shouldShowFloating = hudConfig.visible && hudConfig.renderMode === 'floating';
    floatingInfoHud.classList.toggle('hidden', !shouldShowFloating);
    btnHudRestore.classList.toggle('hidden', hudConfig.visible);

    drawCanvas();
  }

  // Draw customizable Info Box onto any 2D Canvas Context (used for 'canvas' renderMode AND optional Image Export)
  function drawCustomHudOnCanvasContext(targetCtx, imgWidth, imgHeight) {
    const t = getActiveTelemetryData();
    const f = hudConfig.fields;

    const lines = [];
    if (f.model || f.mode) {
      const parts = [];
      if (f.model) parts.push(t.modelName);
      if (f.mode) parts.push(t.modeStr);
      lines.push({ text: parts.join('  |  '), type: 'header' });
    }
    if (f.count) {
      lines.push({ text: `Detected: ${t.count} Spoonbills`, type: 'hero' });
    }
    const metaParts = [];
    if (f.tiles) metaParts.push(`Tiles: ${t.slices}`);
    if (f.latency) metaParts.push(`${t.latencyMs} ms`);
    if (f.conf) metaParts.push(`Conf: ${t.confPct}%`);
    if (f.res) metaParts.push(t.resStr);
    if (metaParts.length > 0) {
      lines.push({ text: metaParts.join('  ·  '), type: 'meta' });
    }
    const noteText = (hudConfig.customNote || '').trim();
    if (noteText.length > 0) {
      lines.push({ text: noteText, type: 'note' });
    }

    if (lines.length === 0) return;

    const s = hudConfig.scale * Math.max(0.85, Math.min(1.6, imgWidth / 1200));
    const padX = Math.round(16 * s);
    const padY = Math.round(12 * s);
    const lineGap = Math.round(7 * s);

    targetCtx.save();
    let maxTextW = 0;
    let totalTextH = 0;

    lines.forEach((ln) => {
      if (ln.type === 'header') {
        targetCtx.font = `600 ${Math.round(13 * s)}px Inter, Segoe UI, sans-serif`;
        ln.height = Math.round(16 * s);
      } else if (ln.type === 'hero') {
        targetCtx.font = `800 ${Math.round(19 * s)}px Inter, Segoe UI, sans-serif`;
        ln.height = Math.round(23 * s);
      } else if (ln.type === 'meta') {
        targetCtx.font = `500 ${Math.round(12 * s)}px JetBrains Mono, Consolas, monospace`;
        ln.height = Math.round(15 * s);
      } else {
        targetCtx.font = `600 ${Math.round(12 * s)}px Inter, Segoe UI, sans-serif`;
        ln.height = Math.round(15 * s);
      }
      const m = targetCtx.measureText(ln.text);
      if (m.width > maxTextW) maxTextW = m.width;
      totalTextH += ln.height;
    });
    totalTextH += Math.max(0, lines.length - 1) * lineGap;

    const boxW = Math.min(imgWidth - 24, Math.round(maxTextW + padX * 2 + 8 * s));
    const boxH = Math.round(totalTextH + padY * 2);
    const margin = Math.round(14 * s);

    let bx = margin;
    let by = margin;
    if (hudConfig.position === 'top-right') {
      bx = Math.max(margin, imgWidth - boxW - margin);
      by = margin;
    } else if (hudConfig.position === 'bottom-left') {
      bx = margin;
      by = Math.max(margin, imgHeight - boxH - margin);
    } else if (hudConfig.position === 'bottom-right') {
      bx = Math.max(margin, imgWidth - boxW - margin);
      by = Math.max(margin, imgHeight - boxH - margin);
    }

    // Card Background
    const alpha = hudConfig.opacity;
    if (hudConfig.theme === 'light') {
      targetCtx.fillStyle = `rgba(252, 252, 250, ${alpha})`;
      targetCtx.strokeStyle = 'rgba(210, 214, 220, 0.95)';
    } else {
      targetCtx.fillStyle = `rgba(20, 20, 24, ${alpha})`;
      targetCtx.strokeStyle = 'rgba(63, 63, 70, 0.95)';
    }

    targetCtx.lineWidth = 1.5;
    targetCtx.fillRect(bx, by, boxW, boxH);
    targetCtx.strokeRect(bx, by, boxW, boxH);

    // Left Terracotta Accent Bar
    targetCtx.fillStyle = '#F6821F';
    targetCtx.fillRect(bx, by, Math.max(4, Math.round(5 * s)), boxH);

    // Render Text Lines
    let curY = by + padY;
    const textX = bx + padX + Math.round(4 * s);

    lines.forEach((ln) => {
      curY += ln.height * 0.82;
      if (ln.type === 'header') {
        targetCtx.font = `600 ${Math.round(13 * s)}px Inter, Segoe UI, sans-serif`;
        targetCtx.fillStyle = hudConfig.theme === 'light' ? '#52525B' : '#A1A1AA';
      } else if (ln.type === 'hero') {
        targetCtx.font = `800 ${Math.round(19 * s)}px Inter, Segoe UI, sans-serif`;
        targetCtx.fillStyle = hudConfig.theme === 'light' ? '#18181B' : '#FFFFFF';
      } else if (ln.type === 'meta') {
        targetCtx.font = `500 ${Math.round(12 * s)}px JetBrains Mono, Consolas, monospace`;
        targetCtx.fillStyle = hudConfig.theme === 'light' ? '#71717A' : '#D4D4D8';
      } else {
        targetCtx.font = `600 ${Math.round(12 * s)}px Inter, Segoe UI, sans-serif`;
        targetCtx.fillStyle = '#F6821F';
      }
      targetCtx.fillText(ln.text, textX, curY);
      curY += ln.height * 0.18 + lineGap;
    });

    targetCtx.restore();
  }

  function initHudCustomizerEvents() {
    // Toggle HUD visibility from Inspector, Toolstrip, Floating Close button, or Restore Pill
    chkHudVisible.addEventListener('change', () => {
      hudConfig.visible = chkHudVisible.checked;
      syncInfoHudUI();
      if (hudConfig.visible) triggerHudPulse();
    });

    toolHud.addEventListener('click', () => {
      hudConfig.visible = !hudConfig.visible;
      syncInfoHudUI();
      if (hudConfig.visible) triggerHudPulse();
    });

    btnHudHide.addEventListener('click', (e) => {
      e.stopPropagation();
      hudConfig.visible = false;
      syncInfoHudUI();
    });

    btnHudRestore.addEventListener('click', () => {
      hudConfig.visible = true;
      syncInfoHudUI();
      triggerHudPulse();
    });

    btnHudCustomize.addEventListener('click', (e) => {
      e.stopPropagation();
      switchInspectorTab('hud');
    });

    const topExpBadge = document.getElementById('top-exp-badge');
    if (topExpBadge) {
      topExpBadge.addEventListener('click', () => {
        switchInspectorTab('hud');
      });
    }

    // Per-Bird Confidence Tags Toggle
    chkBirdTags.addEventListener('change', () => {
      showBirdTags = chkBirdTags.checked;
      toolTag.classList.toggle('active', showBirdTags);
      if (latestResult) runInference();
    });

    toolTag.addEventListener('click', () => {
      showBirdTags = !showBirdTags;
      chkBirdTags.checked = showBirdTags;
      toolTag.classList.toggle('active', showBirdTags);
      if (latestResult) runInference();
    });

    // Render Mode (Floating Viewport vs Pinned on Canvas)
    selectHudRenderMode.addEventListener('change', () => {
      hudConfig.renderMode = selectHudRenderMode.value;
      syncInfoHudUI();
    });

    // Corner Anchor Pills & Reset Position
    document.querySelectorAll('.pos-pill').forEach(pill => {
      pill.addEventListener('click', () => {
        hudConfig.position = pill.getAttribute('data-pos');
        hudConfig.customX = null;
        hudConfig.customY = null;
        syncInfoHudUI();
        triggerHudPulse();
      });
    });

    btnHudResetPos.addEventListener('click', () => {
      hudConfig.position = 'top-left';
      hudConfig.customX = null;
      hudConfig.customY = null;
      syncInfoHudUI();
      triggerHudPulse();
    });

    // Field Checkboxes
    const fieldMap = [
      [chkFieldModel, 'model'],
      [chkFieldMode, 'mode'],
      [chkFieldCount, 'count'],
      [chkFieldTiles, 'tiles'],
      [chkFieldLatency, 'latency'],
      [chkFieldConf, 'conf'],
      [chkFieldRes, 'res']
    ];
    fieldMap.forEach(([chk, key]) => {
      if (!chk) return;
      chk.addEventListener('change', () => {
        hudConfig.fields[key] = chk.checked;
        syncInfoHudUI();
      });
    });

    // Custom Survey Note
    txtHudNote.addEventListener('input', () => {
      hudConfig.customNote = txtHudNote.value;
      syncInfoHudUI();
    });

    // Theme, Opacity & Scale
    selectHudTheme.addEventListener('change', () => {
      hudConfig.theme = selectHudTheme.value;
      syncInfoHudUI();
    });

    rngHudOpacity.addEventListener('input', () => {
      hudOpacityVal.textContent = `${rngHudOpacity.value}%`;
      hudConfig.opacity = parseInt(rngHudOpacity.value, 10) / 100.0;
      syncInfoHudUI();
    });

    rngHudScale.addEventListener('input', () => {
      hudScaleVal.textContent = `${rngHudScale.value}%`;
      hudConfig.scale = parseInt(rngHudScale.value, 10) / 100.0;
      syncInfoHudUI();
    });

    chkExportBakeHud.addEventListener('change', () => {
      hudConfig.bakeOnExport = chkExportBakeHud.checked;
    });

    // Dragging the Floating Info HUD on the Viewport
    floatingInfoHud.addEventListener('mousedown', (e) => {
      if (e.target.closest('.info-hud-actions')) return;
      e.stopPropagation();
      isDraggingHud = true;
      const vpRect = viewportStudio.getBoundingClientRect();
      const hudRect = floatingInfoHud.getBoundingClientRect();
      hudDragOffsetX = e.clientX - hudRect.left;
      hudDragOffsetY = e.clientY - hudRect.top;
      hudConfig.position = 'custom';
      hudConfig.customX = hudRect.left - vpRect.left;
      hudConfig.customY = hudRect.top - vpRect.top;
      syncInfoHudUI();
    });

    window.addEventListener('mousemove', (e) => {
      if (!isDraggingHud) return;
      const vpRect = viewportStudio.getBoundingClientRect();
      const maxX = Math.max(10, vpRect.width - floatingInfoHud.offsetWidth - 10);
      const maxY = Math.max(10, vpRect.height - floatingInfoHud.offsetHeight - 10);
      hudConfig.customX = Math.max(8, Math.min(maxX, e.clientX - vpRect.left - hudDragOffsetX));
      hudConfig.customY = Math.max(8, Math.min(maxY, e.clientY - vpRect.top - hudDragOffsetY));
      syncInfoHudUI();
    });

    window.addEventListener('mouseup', () => {
      isDraggingHud = false;
    });
  }

  // --- 1. INITIALIZATION ---
  async function initStudio() {
    try {
      initHardwareInspector();
      initHudCustomizerEvents();
      await fetchHardwareInfo();

      const [modelsRes, samplesRes, benchRes] = await Promise.all([
        fetch('/api/models').then(r => r.json()),
        fetch('/api/samples').then(r => r.json()),
        fetch('/api/benchmarks').then(r => r.json())
      ]);

      if (modelsRes.active_mode) {
        activeHardwareMode = modelsRes.active_mode;
        updateHardwarePill();
      }

      modelsCatalog = modelsRes.models || [];
      populateModelPicker();

      samplesCatalog = samplesRes.samples || [];
      buildFilmstrip(samplesCatalog);

      benchmarkRows = benchRes.benchmarks || [];
      renderBenchmarkTable('ALL');

      syncInfoHudUI();

      // Auto-load first sample
      if (samplesCatalog.length > 0) {
        selectFilmstripItem(samplesCatalog[0].filename);
      }
    } catch (err) {
      console.error('Init error:', err);
    }
  }

  function populateModelPicker() {
    selectModel.innerHTML = '';
    const groups = {};
    modelsCatalog.forEach(m => {
      const grp = m.phase.includes('Production') ? 'Production Models'
                : m.phase.includes('Ablation') ? 'Ablation Research Suite' : 'Legacy';
      if (!groups[grp]) groups[grp] = [];
      groups[grp].push(m);
    });

    Object.keys(groups).forEach(grpName => {
      const og = document.createElement('optgroup');
      og.label = grpName;
      groups[grpName].forEach(m => {
        const opt = document.createElement('option');
        opt.value = m.id;

        let label = m.short_name;
        if (activeHardwareMode === 'cpu') {
          if (m.scale === 'N' || m.recommended) {
            label = `[Fast CPU] ${m.short_name}`;
          } else if (m.cpu_warning || m.scale === 'L' || m.scale === 'M') {
            label = `[GPU Rec] ${m.short_name}`;
          }
        }
        opt.textContent = `${label} (${m.params})`;
        og.appendChild(opt);
      });
      selectModel.appendChild(og);
    });

    if (activeHardwareMode === 'cpu' && modelsCatalog.some(m => m.id === 'v3_11n_onnx')) {
      selectModel.value = 'v3_11n_onnx';
    } else if (modelsCatalog.some(m => m.id === 'v3_11l_onnx')) {
      selectModel.value = 'v3_11l_onnx';
    } else if (modelsCatalog.length > 0) {
      selectModel.value = modelsCatalog[0].id;
    }

    updateModelBadges();
  }

  function updateModelBadges() {
    const m = modelsCatalog.find(x => x.id === selectModel.value) || modelsCatalog[0];
    if (!m) return;
    badgeTech.textContent = `${m.params} · ${m.backend} FP32`;
    badgeTarget.textContent = m.badge;

    const warnContainer = document.getElementById('model-warning-container');
    const warnBadge = document.getElementById('model-warning-badge');
    if (warnContainer && warnBadge) {
      if (activeHardwareMode === 'cpu' && (m.cpu_warning || m.scale === 'L' || m.scale === 'M')) {
        warnContainer.style.display = 'block';
        warnBadge.className = 'model-badge-warn';
        warnBadge.textContent = 'Disarankan NVIDIA GPU (Beban CPU Tinggi)';
      } else if (activeHardwareMode === 'cpu' && (m.scale === 'N' || m.recommended)) {
        warnContainer.style.display = 'block';
        warnBadge.className = 'model-badge-rec';
        warnBadge.textContent = 'Rekomendasi CPU Aktif: Kecepatan Instan (<60ms)';
      } else {
        warnContainer.style.display = 'none';
      }
    }
  }

  selectModel.addEventListener('change', () => {
    updateModelBadges();
    runInference();
  });

  // --- 2. BOTTOM FILMSTRIP (LIGHTROOM PHOTO BIN) ---
  function buildFilmstrip(items) {
    filmstripTrack.innerHTML = '';
    fsCount.textContent = `${items.length} images`;

    items.forEach(item => {
      const box = document.createElement('div');
      box.className = 'fs-item';
      box.setAttribute('data-fname', item.filename);

      const img = document.createElement('img');
      img.src = item.url || `/api/sample_image/${encodeURIComponent(item.filename)}`;
      img.alt = item.filename;

      const lbl = document.createElement('span');
      lbl.className = 'fs-label';
      lbl.textContent = item.filename;

      box.appendChild(img);
      box.appendChild(lbl);

      box.addEventListener('click', () => {
        selectFilmstripItem(item.filename);
      });
      filmstripTrack.appendChild(box);
    });
  }

  function selectFilmstripItem(filename) {
    document.querySelectorAll('.fs-item').forEach(b => {
      b.classList.toggle('active', b.getAttribute('data-fname') === filename);
    });
    currentFile = null;
    currentSampleName = filename;
    topFilename.textContent = filename;

    baseImg = new Image();
    baseImg.onload = () => {
      annotatedImg.src = '';
      latestResult = null;
      highlightedBBox = null;
      fitToCanvas();
      drawCanvas();
      hudRes.textContent = `${baseImg.width}×${baseImg.height} px`;
      syncInfoHudUI();
      runInference();
    };
    baseImg.src = `/api/sample_image/${encodeURIComponent(filename)}`;
  }

  btnToggleFilmstrip.addEventListener('click', () => {
    filmstrip.classList.toggle('hidden');
    setTimeout(resizeCanvas, 50);
  });

  fileInput.addEventListener('change', (e) => {
    if (e.target.files.length > 0) {
      loadUserFile(e.target.files[0]);
    }
  });

  viewportStudio.addEventListener('dragover', (e) => {
    e.preventDefault();
    document.getElementById('drop-hint').classList.remove('hidden');
  });

  viewportStudio.addEventListener('dragleave', (e) => {
    e.preventDefault();
    document.getElementById('drop-hint').classList.add('hidden');
  });

  viewportStudio.addEventListener('drop', (e) => {
    e.preventDefault();
    document.getElementById('drop-hint').classList.add('hidden');
    if (e.dataTransfer.files.length > 0) {
      loadUserFile(e.dataTransfer.files[0]);
    }
  });

  function loadUserFile(file) {
    currentFile = file;
    currentSampleName = null;
    topFilename.textContent = file.name;

    const box = document.createElement('div');
    box.className = 'fs-item active';
    box.setAttribute('data-fname', file.name);
    const lbl = document.createElement('span');
    lbl.className = 'fs-label';
    lbl.textContent = file.name;
    box.appendChild(lbl);
    filmstripTrack.prepend(box);

    const reader = new FileReader();
    reader.onload = (ev) => {
      baseImg = new Image();
      baseImg.onload = () => {
        annotatedImg.src = '';
        latestResult = null;
        highlightedBBox = null;
        fitToCanvas();
        drawCanvas();
        hudRes.textContent = `${baseImg.width}×${baseImg.height} px`;
        syncInfoHudUI();
        runInference();
      };
      baseImg.src = ev.target.result;
    };
    reader.readAsDataURL(file);
  }

  // --- 3. INTERACTIVE CANVAS VIEWPORT ---
  function resizeCanvas() {
    canvas.width = viewportStudio.clientWidth;
    canvas.height = viewportStudio.clientHeight;
    drawCanvas();
  }
  window.addEventListener('resize', resizeCanvas);

  function fitToCanvas() {
    if (!baseImg.width) return;
    canvas.width = viewportStudio.clientWidth;
    canvas.height = viewportStudio.clientHeight;
    zoom = Math.min((canvas.width - 40) / baseImg.width, (canvas.height - 40) / baseImg.height, 1.0);
    panX = (canvas.width - baseImg.width * zoom) / 2;
    panY = (canvas.height - baseImg.height * zoom) / 2;
    hudZoom.textContent = `${Math.round(zoom * 100)}%`;
  }

  function drawCanvas() {
    if (!canvas.width || !canvas.height) return;
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const img = (annotatedImg.src && annotatedImg.complete && annotatedImg.width) ? annotatedImg : baseImg;
    if (!img.width) return;

    ctx.save();
    ctx.translate(panX, panY);
    ctx.scale(zoom, zoom);

    ctx.shadowColor = 'rgba(0, 0, 0, 0.45)';
    ctx.shadowBlur = 18 / zoom;
    ctx.shadowOffsetY = 4 / zoom;
    ctx.drawImage(img, 0, 0);
    ctx.shadowColor = 'transparent';

    // Non-destructive Canvas-pinned HUD mode (if selected by user)
    if (hudConfig.visible && hudConfig.renderMode === 'canvas') {
      drawCustomHudOnCanvasContext(ctx, img.width, img.height);
    }

    // Draw active bird focus ring
    if (highlightedBBox) {
      const [x1, y1, x2, y2] = highlightedBBox;
      ctx.strokeStyle = '#F6821F';
      ctx.lineWidth = Math.max(3, 4 / zoom);
      ctx.strokeRect(x1 - 4, y1 - 4, (x2 - x1) + 8, (y2 - y1) + 8);
    }
    ctx.restore();
  }

  // Mouse pan & zoom
  viewportStudio.addEventListener('wheel', (e) => {
    e.preventDefault();
    const factor = 1.14;
    const rect = viewportStudio.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    const newZoom = Math.max(0.08, Math.min(12.0, e.deltaY < 0 ? zoom * factor : zoom / factor));
    panX = mx - (mx - panX) * (newZoom / zoom);
    panY = my - (my - panY) * (newZoom / zoom);
    zoom = newZoom;
    hudZoom.textContent = `${Math.round(zoom * 100)}%`;
    drawCanvas();
  });

  viewportStudio.addEventListener('mousedown', (e) => {
    if (e.target.closest('#floating-info-hud') || e.target.closest('#btn-hud-restore')) return;
    if (e.button === 0) {
      isDragging = true;
      dragStartX = e.clientX - panX;
      dragStartY = e.clientY - panY;
    }
  });

  window.addEventListener('mousemove', (e) => {
    if (isDragging && !isDraggingHud) {
      panX = e.clientX - dragStartX;
      panY = e.clientY - dragStartY;
      drawCanvas();
    }
  });

  window.addEventListener('mouseup', () => {
    isDragging = false;
  });

  // Toolbar actions
  toolPan.addEventListener('click', () => {
    toolPan.classList.add('active');
  });
  toolFit.addEventListener('click', () => {
    highlightedBBox = null;
    fitToCanvas();
    drawCanvas();
  });
  tool100.addEventListener('click', () => {
    zoom = 1.0;
    if (baseImg.width) {
      panX = (canvas.width - baseImg.width) / 2;
      panY = (canvas.height - baseImg.height) / 2;
    }
    hudZoom.textContent = '100%';
    drawCanvas();
  });

  toolMask.addEventListener('click', () => {
    showMasks = !showMasks;
    toolMask.classList.toggle('active', showMasks);
    if (latestResult) runInference();
  });

  toolBox.addEventListener('click', () => {
    showBoxes = !showBoxes;
    toolBox.classList.toggle('active', showBoxes);
    if (latestResult) runInference();
  });

  // Keyboard Shortcuts: Enter = Detect, I = Toggle Info HUD, T = Toggle Bird Tags, M = Masks, B = Boxes, F = Fit
  window.addEventListener('keydown', (e) => {
    if (e.target.matches('input, select, textarea')) return;
    if (activeView !== 'studio') return;

    if (e.key === 'Enter') {
      runInference();
    } else if (e.key === 'i' || e.key === 'I') {
      hudConfig.visible = !hudConfig.visible;
      syncInfoHudUI();
    } else if (e.key === 't' || e.key === 'T') {
      showBirdTags = !showBirdTags;
      chkBirdTags.checked = showBirdTags;
      toolTag.classList.toggle('active', showBirdTags);
      if (latestResult) runInference();
    } else if (e.key === 'm' || e.key === 'M') {
      showMasks = !showMasks;
      toolMask.classList.toggle('active', showMasks);
      if (latestResult) runInference();
    } else if (e.key === 'b' || e.key === 'B') {
      showBoxes = !showBoxes;
      toolBox.classList.toggle('active', showBoxes);
      if (latestResult) runInference();
    } else if (e.key === 'f' || e.key === 'F') {
      highlightedBBox = null;
      fitToCanvas();
      drawCanvas();
    }
  });

  // --- 4. RUN INFERENCE ---
  btnRunDetect.addEventListener('click', runInference);

  async function runInference() {
    if (!currentFile && !currentSampleName) return;

    const fd = new FormData();
    if (currentFile) fd.append('file', currentFile);
    if (currentSampleName) fd.append('sample_name', currentSampleName);
    fd.append('model_id', selectModel.value);
    fd.append('use_sahi', chkSahi.checked);
    fd.append('slice_size', parseInt(rngTile.value, 10));
    fd.append('overlap', parseFloat(rngOverlap.value) / 100);
    fd.append('conf', parseFloat(rngConf.value) / 100);
    fd.append('iou', parseFloat(rngIou.value) / 100);
    fd.append('draw_masks', showMasks);
    fd.append('draw_boxes', showBoxes);
    fd.append('draw_labels', showBirdTags);
    fd.append('draw_hud', false); // Never bake HUD on backend so image stays 100% clean & non-destructive

    psBusy.classList.remove('hidden');
    psBusyText.textContent = 'Detecting spoonbills...';
    btnRunDetect.disabled = true;

    try {
      const resp = await fetch('/api/predict', { method: 'POST', body: fd });
      const data = await resp.json();
      if (!resp.ok) throw new Error(data.detail || 'Inference error');

      latestResult = data;
      highlightedBBox = null;

      annotatedImg = new Image();
      annotatedImg.onload = () => {
        syncInfoHudUI();
        drawCanvas();
      };
      annotatedImg.src = data.image_data;

      // Update hero count & status bar
      birdCountHero.textContent = `${data.total_count}`;
      hudTime.textContent = `${data.inference_time_ms} ms (${data.total_slices} tiles)`;
      syncInfoHudUI();

      // Enable exports
      btnExportImg.disabled = false;
      btnExportCsv.disabled = false;
      btnExportYolo.disabled = false;

      // Populate instance list
      renderInstancesList(data.detections);
    } catch (err) {
      alert(`Detection failed: ${err.message}`);
    } finally {
      psBusy.classList.add('hidden');
      btnRunDetect.disabled = false;
    }
  }

  function renderInstancesList(dets) {
    instanceList.innerHTML = '';
    instancesCountTab.textContent = dets.length;

    if (!dets || dets.length === 0) {
      instanceList.innerHTML = '<span class="list-empty">0 birds detected at current confidence.</span>';
      return;
    }

    dets.forEach(d => {
      const row = document.createElement('div');
      row.className = 'instance-row';
      row.innerHTML = `
        <span><b>#${d.id}</b> · Spoonbill</span>
        <span style="color:#10B981;">${(d.score * 100).toFixed(0)}%</span>
      `;
      row.addEventListener('click', () => {
        focusOnBBox(d.bbox);
      });
      instanceList.appendChild(row);
    });
  }

  function focusOnBBox(bbox) {
    const [x1, y1, x2, y2] = bbox;
    const bw = Math.max(50, x2 - x1);
    const bh = Math.max(50, y2 - y1);
    const cx = (x1 + x2) / 2;
    const cy = (y1 + y2) / 2;

    zoom = Math.min(4.0, Math.max(1.0, Math.min(canvas.width / (bw * 3.5), canvas.height / (bh * 3.5))));
    panX = canvas.width / 2 - cx * zoom;
    panY = canvas.height / 2 - cy * zoom;
    highlightedBBox = bbox;
    hudZoom.textContent = `${Math.round(zoom * 100)}%`;
    drawCanvas();
  }

  // Sliders input sync
  rngConf.addEventListener('input', () => {
    confValText.textContent = `${rngConf.value}%`;
  });
  rngConf.addEventListener('change', runInference);

  rngTile.addEventListener('input', () => {
    tileValText.textContent = `${rngTile.value} px`;
  });
  rngOverlap.addEventListener('input', () => {
    overlapValText.textContent = `${rngOverlap.value}%`;
  });
  rngIou.addEventListener('input', () => {
    iouValText.textContent = (rngIou.value / 100).toFixed(2);
  });

  [rngTile, rngOverlap, rngIou, chkSahi].forEach(ctrl => {
    ctrl.addEventListener('change', () => {
      syncInfoHudUI();
      if (latestResult) runInference();
    });
  });

  // Quick Preset Pills
  document.querySelectorAll('.preset-pill').forEach(pill => {
    pill.addEventListener('click', () => {
      document.querySelectorAll('.preset-pill').forEach(p => p.classList.remove('active'));
      pill.classList.add('active');
      const p = pill.getAttribute('data-p');

      if (p === 'panorama') {
        selectModel.value = 'v3_11l_onnx';
        chkSahi.checked = true;
        rngTile.value = 640; tileValText.textContent = '640 px';
        rngConf.value = 25; confValText.textContent = '25%';
      } else if (p === 'wetland') {
        if (modelsCatalog.some(m => m.id === 'ablation_beakattn_l')) {
          selectModel.value = 'ablation_beakattn_l';
        }
        chkSahi.checked = true;
        rngTile.value = 640; tileValText.textContent = '640 px';
        rngConf.value = 30; confValText.textContent = '30%';
      } else if (p === 'fast') {
        selectModel.value = modelsCatalog.some(m => m.id === 'v3_11n_onnx') ? 'v3_11n_onnx' : 'ablation_baseline_n';
        chkSahi.checked = false;
        rngConf.value = 25; confValText.textContent = '25%';
      }
      updateModelBadges();
      runInference();
    });
  });

  // --- EXPORTS (SUPPORTS CLEAN IMAGE OR OPTIONAL ATTACHED CUSTOM HUD) ---
  btnExportImg.addEventListener('click', () => {
    if (!latestResult) return;

    // If user unchecked 'Attach Info Box on Saved Image', export the clean segmented image directly
    if (!hudConfig.bakeOnExport || !annotatedImg.width) {
      const a = document.createElement('a');
      a.href = latestResult.image_data;
      a.download = `Spoonbill_${latestResult.model_id}_${latestResult.filename}`;
      a.click();
      return;
    }

    // Otherwise composite the user's customized HUD onto a full-resolution offscreen canvas
    const offCanvas = document.createElement('canvas');
    offCanvas.width = annotatedImg.width;
    offCanvas.height = annotatedImg.height;
    const offCtx = offCanvas.getContext('2d');
    offCtx.drawImage(annotatedImg, 0, 0);
    drawCustomHudOnCanvasContext(offCtx, offCanvas.width, offCanvas.height);

    const a = document.createElement('a');
    a.href = offCanvas.toDataURL('image/jpeg', 0.94);
    a.download = `Spoonbill_HUD_${latestResult.model_id}_${latestResult.filename}`;
    a.click();
  });

  btnExportCsv.addEventListener('click', () => {
    if (!latestResult) return;
    const rows = [['Bird_ID', 'Class', 'Confidence', 'X1', 'Y1', 'X2', 'Y2', 'Area_px2']];
    latestResult.detections.forEach(d => {
      rows.push([d.id, d.class_name, d.score, d.bbox[0], d.bbox[1], d.bbox[2], d.bbox[3], d.area]);
    });
    const csv = 'data:text/csv;charset=utf-8,' + rows.map(r => r.join(',')).join('\n');
    const a = document.createElement('a');
    a.href = encodeURI(csv);
    a.download = `Spoonbill_Census_${latestResult.filename}.csv`;
    a.click();
  });

  btnExportYolo.addEventListener('click', () => {
    if (!latestResult) return;
    const blob = new Blob([latestResult.yolo_labels_txt || ''], { type: 'text/plain;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    const bname = latestResult.filename.replace(/\.[^/.]+$/, '');
    a.download = `${bname}.txt`;
    a.click();
  });

  // --- 5. VIEW SWITCHING (STUDIO / COMPARE / BENCHMARK) ---
  document.querySelectorAll('.mode-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      activeView = btn.getAttribute('data-view');
      document.querySelectorAll('.mode-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      viewportStudio.classList.toggle('hidden', activeView !== 'studio');
      viewportCompare.classList.toggle('hidden', activeView !== 'compare');
      viewportBenchmark.classList.toggle('hidden', activeView !== 'benchmark');

      toolstrip.classList.toggle('hidden', activeView !== 'studio');
      inspector.classList.toggle('hidden', activeView === 'benchmark');

      if (activeView === 'studio') {
        setTimeout(resizeCanvas, 50);
      } else if (activeView === 'compare') {
        runSplitCompare();
      }
    });
  });

  // Quick Compare
  async function runSplitCompare() {
    const fd = new FormData();
    if (currentFile) fd.append('file', currentFile);
    if (currentSampleName) fd.append('sample_name', currentSampleName);
    fd.append('model_a', 'ablation_baseline_l');
    fd.append('model_b', 'ablation_beakattn_l');
    fd.append('compare_mode', 'segmentation');
    fd.append('use_sahi', true);
    fd.append('draw_hud', false);

    try {
      const resp = await fetch('/api/compare', { method: 'POST', body: fd });
      const data = await resp.json();
      if (!resp.ok) return;

      document.getElementById('cmp-label-a').textContent = data.model_a.name;
      document.getElementById('cmp-count-a').textContent = `${data.model_a.count} Birds (${data.model_a.latency_ms} ms)`;
      document.getElementById('cmp-img-a').src = data.model_a.image_data;

      document.getElementById('cmp-label-b').textContent = data.model_b.name;
      document.getElementById('cmp-count-b').textContent = `${data.model_b.count} Birds (${data.model_b.latency_ms} ms)`;
      document.getElementById('cmp-img-b').src = data.model_b.image_data;

      birdCountHero.textContent = `${data.model_b.count}`;
    } catch (e) {
      console.error(e);
    }
  }

  // Benchmark table
  function renderBenchmarkTable(filter) {
    const tbody = document.getElementById('bench-tbody');
    tbody.innerHTML = '';
    const filtered = benchmarkRows.filter(r => filter === 'ALL' || (r['Group'] || '').includes(filter));
    filtered.forEach(r => {
      const isBeakAttn = (r['Model Name'] || '').includes('BeakAttn');
      const tr = document.createElement('tr');
      const fmt = (v) => v ? `${(parseFloat(v) * 100).toFixed(1)}%` : '—';
      tr.innerHTML = `
        <td style="color:var(--ps-text-muted);">${r['Group']}</td>
        <td><strong style="color:${isBeakAttn ? 'var(--ps-accent)' : 'inherit'};">${r['Model Name']}</strong></td>
        <td>${r['Scale']}</td>
        <td>${r['Best Epoch']}</td>
        <td style="color:#10B981;"><b>${fmt(r['Mask mAP50'])}</b></td>
        <td>${fmt(r['Mask mAP50-95'])}</td>
        <td>${fmt(r['Mask Precision'])}</td>
        <td>${fmt(r['Mask Recall'])}</td>
        <td>${fmt(r['Box mAP50'])}</td>
        <td style="color:${isBeakAttn && r['Scale'] === 'L' ? 'var(--ps-accent)' : 'inherit'};"><b>${fmt(r['Box Precision'])}</b></td>
        <td>${parseFloat(r['Val Seg Loss'] || 0).toFixed(4)}</td>
      `;
      tbody.appendChild(tr);
    });
  }

  document.querySelectorAll('#bench-filters .b-pill').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('#bench-filters .b-pill').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      renderBenchmarkTable(btn.getAttribute('data-filter'));
    });
  });

  initStudio();
});
