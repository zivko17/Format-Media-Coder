const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('api', {
  // Window controls
  minimize: () => ipcRenderer.send('win-minimize'),
  maximize: () => ipcRenderer.send('win-maximize'),
  close: () => ipcRenderer.send('win-close'),

  // Generic Invoke (ESTA ES LA QUE FALTABA)
  invoke: (channel, data) => ipcRenderer.invoke(channel, data),

  // Dialogs
  openFile: (opts) => ipcRenderer.invoke('open-file-dialog', opts),
  saveFile: (opts) => ipcRenderer.invoke('save-file-dialog', opts),
  openFolder: () => ipcRenderer.invoke('open-folder-dialog'),
  showInFolder: (p) => ipcRenderer.invoke('show-in-folder', p),

  // FFmpeg operations
  probe: (filePath) => ipcRenderer.invoke('ffmpeg:probe', filePath),
  convert: (params) => ipcRenderer.invoke('ffmpeg:convert', params),
  compress: (params) => ipcRenderer.invoke('ffmpeg:compress', params),
  trim: (params) => ipcRenderer.invoke('ffmpeg:trim', params),
  merge: (params) => ipcRenderer.invoke('ffmpeg:merge', params),
  extractAudio: (params) => ipcRenderer.invoke('ffmpeg:extract-audio', params),
  convertAudio: (params) => ipcRenderer.invoke('ffmpeg:convert-audio', params),
  extractFrames: (params) => ipcRenderer.invoke('ffmpeg:extract-frames', params),
  getAvailableEncoders: ()     => ipcRenderer.invoke('ffmpeg:available-encoders'),
  getCustomFfmpegPath:  ()     => ipcRenderer.invoke('ffmpeg:get-custom-path'),
  setCustomFfmpegPath:  (p)    => ipcRenderer.invoke('ffmpeg:set-custom-path', p),
  resetCustomFfmpegPath: ()    => ipcRenderer.invoke('ffmpeg:reset-custom-path'),
  thumbnail: (params) => ipcRenderer.invoke('ffmpeg:thumbnail', params),
  countFrames: (params) => ipcRenderer.invoke('ffmpeg:count-frames', params),
  cropDetect: (params) => ipcRenderer.invoke('ffmpeg:cropdetect', params),
  applyFilters: (params) => ipcRenderer.invoke('ffmpeg:filters', params),
  cancel: () => ipcRenderer.send('ffmpeg:cancel'),

  // Events
  onProgress: (cb) => {
    const handler = (_, data) => cb(data)
    ipcRenderer.on('ffmpeg:progress', handler)
    return () => ipcRenderer.removeListener('ffmpeg:progress', handler)
  },
  onDone: (cb) => {
    const handler = (_, data) => cb(data)
    ipcRenderer.on('ffmpeg:done', handler)
    return () => ipcRenderer.removeListener('ffmpeg:done', handler)
  },
  onError: (cb) => {
    const handler = (_, data) => cb(data)
    ipcRenderer.on('ffmpeg:error', handler)
    return () => ipcRenderer.removeListener('ffmpeg:error', handler)
  }
})