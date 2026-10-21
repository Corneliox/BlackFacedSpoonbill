const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  isElectron: true,
  openExportFolder: (folderPath) => ipcRenderer.invoke('electron:open-export-folder', folderPath),
  getAppInfo: () => ipcRenderer.invoke('electron:get-app-info'),
  setTitleBarColors: (bgColor, symbolColor) => ipcRenderer.invoke('electron:set-titlebar-colors', { bgColor, symbolColor })
});
