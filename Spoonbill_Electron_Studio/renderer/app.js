/**
 * Spoonbill Studio — Photoshop / Lightroom / Warehouse Edition
 * Ultra-Compact, Zero-Clutter Computer Vision Workspace
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
  let activeView = 'studio'; // 'studio', 'compare', 'benchmark'

  // Canvas Pan & Zoom
  const canvas = document.getElementById('studio-canvas');
  const ctx = canvas.getContext('2d');
  const viewport = document.getElementById('viewport-studio');
  let zoom = 1.0;
  let panX = 0;
  let panY = 0;
  let isDragging = false;
  let dragStartX = 0;
  let dragStartY = 0;

  // --- DOM REFERENCES ---
  const topFilename = document.getElementById('top-filename');
  const fileInput = document.getElementById('file-input');
  const btnToggleFilmstrip = document.getElementById('btn-toggle-filmstrip');
  const filmstrip = document.getElementById('ps-filmstrip');
  const filmstripTrack = document.getElementById('filmstrip-track');
  const fsCount = document.getElementById('fs-count');

  const viewportStudio = document.getElementById('viewport-studio');
  const viewportCompare = document.getElementById('viewport-compare');
  const viewportBenchmark = document.getElementById('viewport-benchmark');
  const toolstrip = document.getElementById('ps-toolstrip');
  const inspector = document.getElementById('ps-inspector');

  const toolPan = document.getElementById('tool-pan');
  const toolFit = document.getElementById('tool-fit');
  const tool100 = document.getElementById('tool-100');
  const toolMask = document.getElementById('tool-mask');
  const toolBox = document.getElementById('tool-box');

  const hudRes = document.getElementById('hud-res');
  const hudZoom = document.getElementById('hud-zoom');
  const hudTime = document.getElementById('hud-time');
  const psBusy = document.getElementById('ps-busy');
  const psBusyText = document.getElementById('ps-busy-text');

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

  const instanceList = document.getElementById('instance-list');
  const instancesCountTab = document.getElementById('instances-count-tab');
  const btnExportImg = document.getElementById('btn-export-img');
  const btnExportCsv = document.getElementById('btn-export-csv');
  const btnExportYolo = document.getElementById('btn-export-yolo');

  // --- 1. INITIALIZATION ---
  async function initStudio() {
    try {
      const [modelsRes, samplesRes, benchRes] = await Promise.all([
        fetch('/api/models').then(r => r.json()),
        fetch('/api/samples').then(r => r.json()),
        fetch('/api/benchmarks').then(r => r.json())
      ]);

      modelsCatalog = modelsRes.models || [];
      populateModelPicker();

      samplesCatalog = samplesRes.samples || [];
      buildFilmstrip(samplesCatalog);

      benchmarkRows = benchRes.benchmarks || [];
      renderBenchmarkTable('ALL');

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
        opt.textContent = `${m.short_name} (${m.params})`;
        og.appendChild(opt);
      });
      selectModel.appendChild(og);
    });

    if (modelsCatalog.some(m => m.id === 'v3_11l_onnx')) selectModel.value = 'v3_11l_onnx';
    updateModelBadges();
  }

  function updateModelBadges() {
    const m = modelsCatalog.find(x => x.id === selectModel.value) || modelsCatalog[0];
    if (!m) return;
    badgeTech.textContent = `${m.params} · ${m.backend} FP32`;
    badgeTarget.textContent = m.badge;
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
      runInference();
    };
    baseImg.src = `/api/sample_image/${encodeURIComponent(filename)}`;
  }

  // Toggle Filmstrip visibility for maximum canvas real estate
  btnToggleFilmstrip.addEventListener('click', () => {
    filmstrip.classList.toggle('hidden');
    setTimeout(resizeCanvas, 50);
  });

  // File Upload (Drag-drop & Picker)
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

    // Add to filmstrip as active item
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

    // Render photo with delicate dark border & shadow
    ctx.shadowColor = 'rgba(0, 0, 0, 0.45)';
    ctx.shadowBlur = 18 / zoom;
    ctx.shadowOffsetY = 4 / zoom;
    ctx.drawImage(img, 0, 0);
    ctx.shadowColor = 'transparent';

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
    if (e.button === 0) {
      isDragging = true;
      dragStartX = e.clientX - panX;
      dragStartY = e.clientY - panY;
    }
  });

  window.addEventListener('mousemove', (e) => {
    if (isDragging) {
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
    document.querySelectorAll('.tool-btn').forEach(b => b.classList.remove('active'));
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

  // --- 4. RUN INFERENCE ---
  btnRunDetect.addEventListener('click', runInference);

  // Shortcut: Enter key runs detection
  window.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && activeView === 'studio' && !e.target.matches('input, select')) {
      runInference();
    }
  });

  async function runInference() {
    if (!currentFile && !currentSampleName) return;

    const fd = new FormData();
    if (currentFile) fd.append('file', currentFile);
    if (currentSampleName) fd.append('sample_name', currentSampleName);
    fd.append('model_id', selectModel.value);
    fd.append('use_sahi', chkSahi.checked);
    fd.append('slice_size', parseInt(rngTile.value));
    fd.append('overlap', parseFloat(rngOverlap.value) / 100);
    fd.append('conf', parseFloat(rngConf.value) / 100);
    fd.append('iou', parseFloat(rngIou.value) / 100);
    fd.append('draw_masks', showMasks);
    fd.append('draw_boxes', showBoxes);
    fd.append('draw_labels', true);

    psBusy.classList.remove('hidden');
    psBusyText.textContent = `Detecting spoonbills...`;
    btnRunDetect.disabled = true;

    try {
      const resp = await fetch('/api/predict', { method: 'POST', body: fd });
      const data = await resp.json();
      if (!resp.ok) throw new Error(data.detail || 'Inference error');

      latestResult = data;
      highlightedBBox = null;

      annotatedImg = new Image();
      annotatedImg.onload = () => {
        drawCanvas();
      };
      annotatedImg.src = data.image_data;

      // Update hero count & HUD
      birdCountHero.textContent = `${data.total_count}`;
      hudTime.textContent = `${data.inference_time_ms} ms (${data.total_slices} tiles)`;

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

  // Exports
  btnExportImg.addEventListener('click', () => {
    if (!latestResult) return;
    const a = document.createElement('a');
    a.href = latestResult.image_data;
    a.download = `Spoonbill_${latestResult.model_id}_${latestResult.filename}`;
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
      const fmt = (v) => v ? `${(parseFloat(v)*100).toFixed(1)}%` : '—';
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
        <td style="color:${isBeakAttn && r['Scale']==='L' ? 'var(--ps-accent)' : 'inherit'};"><b>${fmt(r['Box Precision'])}</b></td>
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
