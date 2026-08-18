/**
 * SPOONBILL AI STUDIO - UNIVERSAL V3 JAVASCRIPT
 * Interactive Canvas, Pan/Zoom, Dual Parameter Binding, ONNX API Integration,
 * and Batch Multi-Image Automated Census & Output Saving.
 */

document.addEventListener('DOMContentLoaded', () => {
  // DOM ELEMENTS - COMMON & SINGLE
  const canvas = document.getElementById('main-canvas');
  const ctx = canvas.getContext('2d');
  const canvasContainer = document.getElementById('canvas-container');
  const dragDropOverlay = document.getElementById('drag-drop-overlay');
  const loadingOverlay = document.getElementById('loading-overlay');
  const loadingText = document.getElementById('loading-text');

  const fileInput = document.getElementById('file-input');
  const fileNameDisplay = document.getElementById('file-name-display');
  const headerBirdCount = document.getElementById('header-bird-count');

  const btnRun = document.getElementById('btn-run-inference');
  const btnRunText = document.getElementById('btn-run-text');
  const btnDownloadImg = document.getElementById('btn-download-img');
  const btnExportCsv = document.getElementById('btn-export-csv');

  const btnZoomIn = document.getElementById('btn-zoom-in');
  const btnZoomOut = document.getElementById('btn-zoom-out');
  const btnFitScreen = document.getElementById('btn-fit-screen');
  const btnResetZoom = document.getElementById('btn-reset-zoom');

  // DOM ELEMENTS - BATCH MODE
  const tabSingle = document.getElementById('tab-single');
  const tabBatch = document.getElementById('tab-batch');
  const viewSingle = document.getElementById('view-single');
  const viewBatch = document.getElementById('view-batch');
  const layersSection = document.getElementById('layers-section');

  const batchFileInput = document.getElementById('batch-file-input');
  const btnStartBatch = document.getElementById('btn-start-batch');
  const batchSelectedCount = document.getElementById('batch-selected-count');
  const batchSummaryCards = document.getElementById('batch-summary-cards');
  const batchExportToolbar = document.getElementById('batch-export-toolbar');
  const batchTableBody = document.getElementById('batch-table-body');

  const batchCardTotalImgs = document.getElementById('batch-card-total-imgs');
  const batchCardTotalBirds = document.getElementById('batch-card-total-birds');
  const batchCardAvgTime = document.getElementById('batch-card-avg-time');
  const btnBatchDownloadCsv = document.getElementById('btn-batch-download-csv');
  const btnBatchDownloadZip = document.getElementById('btn-batch-download-zip');

  // HYPERPARAMETER CONTROLS
  const toggleSahi = document.getElementById('toggle-sahi');
  const toggleSahiText = document.getElementById('toggle-sahi-text');
  const rowTileSize = document.getElementById('row-tile-size');
  const rowOverlap = document.getElementById('row-overlap');

  const sliderTile = document.getElementById('slider-tile');
  const inputTile = document.getElementById('input-tile');
  const sliderOverlap = document.getElementById('slider-overlap');
  const inputOverlap = document.getElementById('input-overlap');
  const sliderConf = document.getElementById('slider-conf');
  const inputConf = document.getElementById('input-conf');
  const sliderIou = document.getElementById('slider-iou');
  const inputIou = document.getElementById('input-iou');

  const layerMasks = document.getElementById('layer-masks');
  const layerBoxes = document.getElementById('layer-boxes');
  const layerLabels = document.getElementById('layer-labels');

  const tableBody = document.getElementById('table-body');
  const statMode = document.getElementById('stat-mode');
  const statTime = document.getElementById('stat-time');
  const statSlices = document.getElementById('stat-slices');
  const statRes = document.getElementById('stat-res');

  // STATE
  let activeTab = 'single';
  let currentFile = null;
  let currentImage = new Image();
  let latestResult = null;
  let annotatedImage = new Image();

  let batchFiles = [];
  let latestBatchData = null;

  let zoomScale = 1.0;
  let panX = 0;
  let panY = 0;
  let isDragging = false;
  let startX = 0;
  let startY = 0;

  // --- TAB NAVIGATION ---
  tabSingle.addEventListener('click', () => switchTab('single'));
  tabBatch.addEventListener('click', () => switchTab('batch'));

  function switchTab(tab) {
    activeTab = tab;
    if (tab === 'single') {
      tabSingle.classList.add('active');
      tabBatch.classList.remove('active');
      viewSingle.classList.add('active');
      viewBatch.classList.remove('active');
      layersSection.style.display = 'block';
      btnRun.style.display = 'flex';
      setTimeout(resizeCanvas, 50);
    } else {
      tabBatch.classList.add('active');
      tabSingle.classList.remove('active');
      viewBatch.classList.add('active');
      viewSingle.classList.remove('active');
      layersSection.style.display = 'none';
      btnRun.style.display = 'none';
    }
  }

  // --- DUAL BINDING SLIDERS & NUMERIC INPUTS ---
  function setupDualBinding(slider, input, isFloat = false, multiplier = 1) {
    slider.addEventListener('input', () => {
      let val = slider.value;
      if (isFloat) {
        input.value = (val / multiplier).toFixed(2);
      } else {
        input.value = val;
      }
    });

    input.addEventListener('change', () => {
      let val = parseFloat(input.value);
      if (isNaN(val)) return;
      if (isFloat) {
        slider.value = Math.round(val * multiplier);
      } else {
        slider.value = val;
      }
    });
  }

  setupDualBinding(sliderTile, inputTile, false, 1);
  setupDualBinding(sliderOverlap, inputOverlap, true, 100);
  setupDualBinding(sliderConf, inputConf, true, 100);
  setupDualBinding(sliderIou, inputIou, true, 100);

  // --- MODEL SELECTION CARDS ---
  document.querySelectorAll('.model-card').forEach(card => {
    card.addEventListener('click', () => {
      document.querySelectorAll('.model-card').forEach(c => c.classList.remove('active'));
      card.classList.add('active');
      card.querySelector('input').checked = true;
    });
  });

  // --- SAHI TOGGLE ---
  toggleSahi.addEventListener('change', () => {
    const isSahi = toggleSahi.checked;
    if (isSahi) {
      toggleSahiText.textContent = "Enable SAHI Tiling (Sliced Window)";
      rowTileSize.classList.remove('disabled');
      rowOverlap.classList.remove('disabled');
      btnRunText.textContent = "Run SAHI V3 Analysis";
    } else {
      toggleSahiText.textContent = "Direct Single-Pass (Without SAHI)";
      rowTileSize.classList.add('disabled');
      rowOverlap.classList.add('disabled');
      btnRunText.textContent = "Run Direct YOLOv11";
    }
  });

  // --- TOOLTIP TOGGLE ---
  document.querySelectorAll('.help-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      const tooltipId = btn.getAttribute('data-tooltip');
      const tooltip = document.getElementById(tooltipId);
      document.querySelectorAll('.tooltip-box').forEach(t => {
        if (t !== tooltip) t.classList.remove('show');
      });
      if (tooltip) tooltip.classList.toggle('show');
    });
  });

  document.addEventListener('click', () => {
    document.querySelectorAll('.tooltip-box').forEach(t => t.classList.remove('show'));
  });

  // --- SINGLE FILE HANDLING & DRAG-DROP ---
  fileInput.addEventListener('change', (e) => {
    if (e.target.files.length > 0) {
      handleSingleFile(e.target.files[0]);
    }
  });

  ['dragenter', 'dragover'].forEach(name => {
    canvasContainer.addEventListener(name, (e) => {
      e.preventDefault();
      dragDropOverlay.classList.remove('hidden');
    });
  });

  ['dragleave', 'drop'].forEach(name => {
    canvasContainer.addEventListener(name, (e) => {
      e.preventDefault();
      dragDropOverlay.classList.add('hidden');
    });
  });

  canvasContainer.addEventListener('drop', (e) => {
    if (e.dataTransfer.files.length > 0) {
      if (e.dataTransfer.files.length === 1) {
        handleSingleFile(e.dataTransfer.files[0]);
      } else {
        switchTab('batch');
        handleBatchFiles(Array.from(e.dataTransfer.files));
      }
    }
  });

  function handleSingleFile(file) {
    if (!file.type.startsWith('image/')) {
      alert('Please upload an image file (JPG, PNG, WEBP, etc.)');
      return;
    }
    currentFile = file;
    fileNameDisplay.textContent = file.name;
    dragDropOverlay.classList.add('hidden');

    const reader = new FileReader();
    reader.onload = (e) => {
      currentImage.onload = () => {
        annotatedImage.src = "";
        latestResult = null;
        fitImageToCanvas();
        renderCanvas();
        statRes.textContent = `Resolution: ${currentImage.width}x${currentImage.height} px`;
        statMode.textContent = "Mode: Ready";
        statTime.textContent = "Latency: -";
        statSlices.textContent = "Slices: -";
        headerBirdCount.textContent = "Ready";
        btnDownloadImg.disabled = true;
        btnExportCsv.disabled = true;
        clearTable();
      };
      currentImage.src = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  // --- BATCH FILES HANDLING ---
  batchFileInput.addEventListener('change', (e) => {
    if (e.target.files.length > 0) {
      handleBatchFiles(Array.from(e.target.files));
    }
  });

  function handleBatchFiles(filesList) {
    batchFiles = filesList.filter(f => f.type.startsWith('image/') || f.name.match(/\.(jpg|jpeg|png|bmp|webp)$/i));
    batchSelectedCount.textContent = `📦 ${batchFiles.length} images queued for processing`;
    btnStartBatch.disabled = batchFiles.length === 0;

    // Render queued files into table
    batchTableBody.innerHTML = '';
    batchFiles.forEach((file, idx) => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${idx + 1}</td>
        <td><b>${file.name}</b></td>
        <td>-</td>
        <td>-</td>
        <td>-</td>
        <td>-</td>
        <td>-</td>
        <td><span style="color:#94a3b8;">⏳ In Queue</span></td>
      `;
      batchTableBody.appendChild(tr);
    });
  }

  // --- BATCH INFERENCE EXECUTION ---
  btnStartBatch.addEventListener('click', async () => {
    if (batchFiles.length === 0) return;

    const modelChoice = document.querySelector('input[name="model_choice"]:checked').value;
    const useSahi = toggleSahi.checked;
    const sliceSize = parseInt(inputTile.value);
    const overlap = parseFloat(inputOverlap.value);
    const conf = parseFloat(inputConf.value);
    const iou = parseFloat(inputIou.value);

    const formData = new FormData();
    batchFiles.forEach(file => {
      formData.append('files', file);
    });
    formData.append('model_type', modelChoice);
    formData.append('use_sahi', useSahi);
    formData.append('slice_size', sliceSize);
    formData.append('overlap', overlap);
    formData.append('conf', conf);
    formData.append('iou', iou);

    btnStartBatch.disabled = true;
    loadingOverlay.classList.add('active');
    loadingText.textContent = `Processing Batch of ${batchFiles.length} images with ${modelChoice.toUpperCase()}...`;

    try {
      const resp = await fetch('/api/predict_batch', {
        method: 'POST',
        body: formData
      });

      if (!resp.ok) {
        const err = await resp.json();
        throw new Error(err.detail || 'Batch inference failed');
      }

      const data = await resp.json();
      latestBatchData = data;

      // Update Summary Cards
      batchSummaryCards.style.display = 'grid';
      batchExportToolbar.style.display = 'flex';
      batchCardTotalImgs.textContent = data.total_images;
      batchCardTotalBirds.textContent = `${data.total_spoonbills} Birds`;
      
      let totalTime = data.summary.reduce((acc, curr) => acc + curr.latency_ms, 0);
      let avgTime = data.total_images > 0 ? (totalTime / data.total_images).toFixed(1) : 0;
      batchCardAvgTime.textContent = `${avgTime} ms`;
      headerBirdCount.textContent = `${data.total_spoonbills} Birds`;

      // Populate Batch Table
      batchTableBody.innerHTML = '';
      data.summary.forEach(item => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>#${item.id}</td>
          <td><b>${item.filename}</b></td>
          <td>${item.resolution}</td>
          <td><span style="color:#10b981; font-weight:800; font-size:0.92rem;">${item.bird_count} Birds</span></td>
          <td><span style="color:#06b6d4;">${item.mode}</span></td>
          <td>${item.slices}</td>
          <td>${item.latency_ms} ms</td>
          <td><span style="color:#10b981;">✅ Processed</span></td>
        `;
        batchTableBody.appendChild(tr);
      });

    } catch (err) {
      alert(`Error during batch processing: ${err.message}`);
    } finally {
      btnStartBatch.disabled = false;
      loadingOverlay.classList.remove('active');
    }
  });

  // BATCH EXPORTS
  btnBatchDownloadZip.addEventListener('click', () => {
    if (!latestBatchData || !latestBatchData.download_url) return;
    window.location.href = latestBatchData.download_url;
  });

  btnBatchDownloadCsv.addEventListener('click', () => {
    if (!latestBatchData || !latestBatchData.summary) return;
    const rows = [
      ['#', 'Image_Filename', 'Resolution', 'Total_Spoonbills_Detected', 'Inference_Mode', 'Latency_ms', 'Slices_Count']
    ];

    latestBatchData.summary.forEach(item => {
      rows.push([
        item.id,
        item.filename,
        item.resolution,
        item.bird_count,
        item.mode,
        item.latency_ms,
        item.slices
      ]);
    });

    rows.push([]);
    rows.push(['TOTAL', `${latestBatchData.total_images} Images`, '-', latestBatchData.total_spoonbills, '-', '-', '-']);

    const csvContent = "data:text/csv;charset=utf-8," + rows.map(e => e.join(",")).join("\n");
    const encodedUri = encodeURI(csvContent);
    const a = document.createElement('a');
    a.href = encodedUri;
    a.download = `Spoonbill_Census_Batch_${latestBatchData.batch_id}.csv`;
    a.click();
  });

  // --- CANVAS RENDERING WITH ZOOM & PAN ---
  function resizeCanvas() {
    canvas.width = canvasContainer.clientWidth;
    canvas.height = canvasContainer.clientHeight;
    renderCanvas();
  }
  window.addEventListener('resize', resizeCanvas);

  function fitImageToCanvas() {
    if (!currentImage.width) return;
    const cw = canvasContainer.clientWidth;
    const ch = canvasContainer.clientHeight;
    const iw = currentImage.width;
    const ih = currentImage.height;

    zoomScale = Math.min((cw - 40) / iw, (ch - 40) / ih, 1.0);
    panX = (cw - iw * zoomScale) / 2;
    panY = (ch - ih * zoomScale) / 2;
  }

  function renderCanvas() {
    if (!canvas.width || !canvas.height) {
      canvas.width = canvasContainer.clientWidth;
      canvas.height = canvasContainer.clientHeight;
    }

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const imgToDraw = annotatedImage.src && annotatedImage.complete ? annotatedImage : currentImage;
    if (imgToDraw.width) {
      ctx.save();
      ctx.translate(panX, panY);
      ctx.scale(zoomScale, zoomScale);
      ctx.drawImage(imgToDraw, 0, 0);
      ctx.restore();
    }
  }

  // --- MOUSE PAN & ZOOM HANDLERS ---
  canvasContainer.addEventListener('wheel', (e) => {
    e.preventDefault();
    const zoomFactor = 1.12;
    const mouseX = e.clientX - canvasContainer.getBoundingClientRect().left;
    const mouseY = e.clientY - canvasContainer.getBoundingClientRect().top;

    let newScale = e.deltaY < 0 ? zoomScale * zoomFactor : zoomScale / zoomFactor;
    newScale = Math.max(0.05, Math.min(10.0, newScale));

    panX = mouseX - (mouseX - panX) * (newScale / zoomScale);
    panY = mouseY - (mouseY - panY) * (newScale / zoomScale);
    zoomScale = newScale;

    renderCanvas();
  });

  canvasContainer.addEventListener('mousedown', (e) => {
    if (e.button === 0) { // Left click
      isDragging = true;
      startX = e.clientX - panX;
      startY = e.clientY - panY;
    }
  });

  window.addEventListener('mousemove', (e) => {
    if (isDragging) {
      panX = e.clientX - startX;
      panY = e.clientY - startY;
      renderCanvas();
    }
  });

  window.addEventListener('mouseup', () => {
    isDragging = false;
  });

  // ZOOM BUTTONS
  btnZoomIn.addEventListener('click', () => {
    zoomScale *= 1.25;
    renderCanvas();
  });
  btnZoomOut.addEventListener('click', () => {
    zoomScale /= 1.25;
    renderCanvas();
  });
  btnFitScreen.addEventListener('click', () => {
    fitImageToCanvas();
    renderCanvas();
  });
  btnResetZoom.addEventListener('click', () => {
    zoomScale = 1.0;
    if (currentImage.width) {
      panX = (canvasContainer.clientWidth - currentImage.width) / 2;
      panY = (canvasContainer.clientHeight - currentImage.height) / 2;
    }
    renderCanvas();
  });

  // --- SINGLE INFERENCE API CALL ---
  btnRun.addEventListener('click', async () => {
    if (!currentFile) {
      alert('Please select or drag an image first.');
      return;
    }

    const modelChoice = document.querySelector('input[name="model_choice"]:checked').value;
    const useSahi = toggleSahi.checked;
    const sliceSize = parseInt(inputTile.value);
    const overlap = parseFloat(inputOverlap.value);
    const conf = parseFloat(inputConf.value);
    const iou = parseFloat(inputIou.value);

    const formData = new FormData();
    formData.append('file', currentFile);
    formData.append('model_type', modelChoice);
    formData.append('use_sahi', useSahi);
    formData.append('slice_size', sliceSize);
    formData.append('overlap', overlap);
    formData.append('conf', conf);
    formData.append('iou', iou);
    formData.append('draw_boxes', layerBoxes.checked);
    formData.append('draw_masks', layerMasks.checked);
    formData.append('draw_labels', layerLabels.checked);

    btnRun.disabled = true;
    loadingOverlay.classList.add('active');
    loadingText.textContent = useSahi ? `Running SAHI V3 Tiled Slicing (${modelChoice.toUpperCase()})...` : `Running Direct Single-Pass (${modelChoice.toUpperCase()})...`;

    try {
      const resp = await fetch('/api/predict', {
        method: 'POST',
        body: formData
      });

      if (!resp.ok) {
        const err = await resp.json();
        throw new Error(err.detail || 'Inference failed');
      }

      const data = await resp.json();
      latestResult = data;

      // Load visualized image
      annotatedImage.onload = () => {
        renderCanvas();
      };
      annotatedImage.src = data.image_data;

      // Update UI Stats
      headerBirdCount.textContent = `${data.total_count} Birds`;
      statMode.textContent = `Mode: ${data.mode}`;
      statTime.textContent = `Latency: ${data.inference_time_ms} ms`;
      statSlices.textContent = `Slices: ${data.total_slices}`;
      statRes.textContent = `Resolution: ${data.image_shape[1]}x${data.image_shape[0]} px`;

      btnDownloadImg.disabled = false;
      btnExportCsv.disabled = false;

      // Populate Table
      populateTable(data.detections);

    } catch (err) {
      alert(`Error during analysis: ${err.message}`);
    } finally {
      btnRun.disabled = false;
      loadingOverlay.classList.remove('active');
    }
  });

  // LAYER TOGGLE RE-RENDERING
  [layerMasks, layerBoxes, layerLabels].forEach(cb => {
    cb.addEventListener('change', () => {
      if (currentFile && latestResult && activeTab === 'single') {
        btnRun.click();
      }
    });
  });

  // TABLE POPULATION
  function populateTable(detections) {
    tableBody.innerHTML = '';
    if (!detections || detections.length === 0) {
      tableBody.innerHTML = '<tr class="empty-row"><td colspan="5">No spoonbills detected with current confidence threshold.</td></tr>';
      return;
    }

    detections.forEach(d => {
      const tr = document.createElement('tr');
      const boxStr = `[${d.bbox.join(', ')}]`;
      tr.innerHTML = `
        <td>#${d.id}</td>
        <td><span style="color:#06b6d4; font-weight:700;">${d.class_name}</span></td>
        <td><span style="color:#10b981; font-weight:700;">${(d.score * 100).toFixed(1)}%</span></td>
        <td>${boxStr}</td>
        <td>${d.area}</td>
      `;
      tableBody.appendChild(tr);
    });
  }

  function clearTable() {
    tableBody.innerHTML = '<tr class="empty-row"><td colspan="5">Upload an image and run inference to see detected spoonbills.</td></tr>';
  }

  // SINGLE EXPORT ACTIONS
  btnDownloadImg.addEventListener('click', () => {
    if (!annotatedImage.src) return;
    const a = document.createElement('a');
    a.href = annotatedImage.src;
    a.download = `Spoonbill_V3_${fileNameDisplay.textContent || 'result'}.jpg`;
    a.click();
  });

  btnExportCsv.addEventListener('click', () => {
    if (!latestResult || !latestResult.detections) return;
    const rows = [
      ['ID', 'Class', 'Confidence', 'X1', 'Y1', 'X2', 'Y2', 'Area_px2']
    ];

    latestResult.detections.forEach(d => {
      rows.push([
        d.id,
        d.class_name,
        d.score,
        d.bbox[0],
        d.bbox[1],
        d.bbox[2],
        d.bbox[3],
        d.area
      ]);
    });

    const csvContent = "data:text/csv;charset=utf-8," + rows.map(e => e.join(",")).join("\n");
    const encodedUri = encodeURI(csvContent);
    const a = document.createElement('a');
    a.href = encodedUri;
    a.download = `Spoonbill_Census_${fileNameDisplay.textContent || 'data'}.csv`;
    a.click();
  });

  // Initialize Canvas dimensions
  setTimeout(resizeCanvas, 100);
});
