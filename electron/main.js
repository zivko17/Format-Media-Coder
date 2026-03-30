const { app, BrowserWindow, ipcMain, dialog, shell } = require('electron')
const path = require('path')
const fs = require('fs')
const { execFileSync } = require('child_process')

// Paths — bin/ bundled (full build con HAP). En producción usa extraResources.
const isDev = process.env.NODE_ENV === 'development'

let ffmpegPath  = isDev
  ? path.join(__dirname, '../bin/ffmpeg.exe')
  : path.join(process.resourcesPath, 'bin/ffmpeg.exe')

let ffprobePath = isDev
  ? path.join(__dirname, '../bin/ffprobe.exe')
  : path.join(process.resourcesPath, 'bin/ffprobe.exe')

// Fallback al ffmpeg-static del npm si no existe bin/ (primera instalación en dev)
if (!fs.existsSync(ffmpegPath)) {
  try {
    ffmpegPath  = require('ffmpeg-static')
    ffprobePath = require('ffprobe-static').path
  } catch (e) { console.error('ffmpeg not found:', e) }
}

const ffmpeg = require('fluent-ffmpeg')

// ── Settings (JSON en userData) ───────────────────────────────────────────────
let settingsFile
let availableEncoders = new Set()

function loadSettings() {
  try { return JSON.parse(fs.readFileSync(settingsFile, 'utf8')) } catch { return {} }
}
function saveSettings(data) {
  const cur = loadSettings()
  fs.writeFileSync(settingsFile, JSON.stringify({ ...cur, ...data }, null, 2))
}

function applyFfmpegPath(p) {
  ffmpegPath = p
  ffmpeg.setFfmpegPath(p)
}

// ── Encoder detection ─────────────────────────────────────────────────────────
function detectEncoders() {
  availableEncoders = new Set()
  try {
    const out = execFileSync(ffmpegPath, ['-encoders', '-hide_banner'], { encoding: 'utf8', timeout: 8000 })
    for (const line of out.split('\n')) {
      const m = line.match(/^\s+[VAS][.F][.S][.X][.B][.D]\s+(\S+)/)
      if (m) availableEncoders.add(m[1])
    }
  } catch (e) { console.error('detectEncoders failed:', e.message) }
}

let mainWindow

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 750,
    minWidth: 900,
    minHeight: 600,
    frame: false,
    titleBarStyle: process.platform === 'darwin' ? 'hiddenInset' : 'hidden',
    backgroundColor: '#0d1117',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  })

  if (isDev) {
    mainWindow.loadURL('http://localhost:5173')
    // mainWindow.webContents.openDevTools()
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'))
  }
}

app.whenReady().then(() => {
  settingsFile = path.join(app.getPath('userData'), 'format-media-coder-settings.json')

  // Aplica custom ffmpeg si el usuario configuró uno
  const saved = loadSettings().ffmpegPath
  if (saved && fs.existsSync(saved)) applyFfmpegPath(saved)
  else if (ffmpegPath) ffmpeg.setFfmpegPath(ffmpegPath)
  if (ffprobePath) ffmpeg.setFfprobePath(ffprobePath)

  detectEncoders()
  createWindow()
})
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})
app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) createWindow()
})

// ── Window controls ──────────────────────────────────────────────────────────
ipcMain.on('win-minimize', () => mainWindow?.minimize())
ipcMain.on('win-maximize', () => {
  if (mainWindow?.isMaximized()) mainWindow.unmaximize()
  else mainWindow?.maximize()
})
ipcMain.on('win-close', () => mainWindow?.close())

// ── Dialogs ──────────────────────────────────────────────────────────────────
ipcMain.handle('open-file-dialog', async (_, options) => {
  return dialog.showOpenDialog(mainWindow, options)
})

ipcMain.handle('save-file-dialog', async (_, options) => {
  return dialog.showSaveDialog(mainWindow, options)
})

ipcMain.handle('open-folder-dialog', async () => {
  return dialog.showOpenDialog(mainWindow, { properties: ['openDirectory'] })
})

ipcMain.handle('show-in-folder', async (_, filePath) => {
  shell.showItemInFolder(filePath)
})

// ── Helpers ──────────────────────────────────────────────────────────────────
function sendProgress(data) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send('ffmpeg:progress', data)
  }
}

function sendDone(output) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send('ffmpeg:done', { output })
  }
}

function sendError(msg) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send('ffmpeg:error', { message: msg })
  }
}

// Active command (for cancel)
let activeCmd = null

ipcMain.on('ffmpeg:cancel', () => {
  if (activeCmd) {
    try { activeCmd.kill('SIGKILL') } catch (e) {}
    activeCmd = null
  }
})

// ── Probe (get media info) ────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:probe', async (_, filePath) => {
  return new Promise((resolve, reject) => {
    ffmpeg.ffprobe(filePath, (err, meta) => {
      if (err) reject(err.message)
      else resolve(meta)
    })
  })
})

// ── Convert (PRO UPGRADE: GPU, Presets & EBU R128) ────────────────────────────
ipcMain.handle('ffmpeg:convert', async (_, { input, output, videoCodec, audioCodec, videoBitrate, audioBitrate, normalizeAudio }) => {
  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input)

    // 1. Lógica de Codecs de Vídeo (Aceleración y Formatos PRO)
    if (videoCodec) {
      switch (videoCodec) {
        case 'h264_nvenc': // Aceleración por Hardware (NVIDIA)
          cmd.videoCodec('h264_nvenc').outputOptions(['-preset p4', '-rc vbr', '-cq 23'])
          break
        case 'hevc_nvenc': // H.265 por GPU (4K ligero)
          cmd.videoCodec('hevc_nvenc').outputOptions(['-preset p4', '-rc vbr', '-cq 28'])
          break
        case 'prores': // Standard VMIX / Playback Pro / QLab
          cmd.videoCodec('prores_ks').outputOptions(['-profile:v 3', '-vendor apl0']) // Perfil 422 HQ
          break
        case 'hap': // Standard Watchout / Disguise
          // Usamos outputOptions en lugar de .videoCodec() para evitar que
          // fluent-ffmpeg rechace 'hap' por no estar en su lista interna de codecs
          cmd.outputOptions(['-c:v', 'hap', '-vf', 'format=rgba'])
          break
        case 'copy': // Passthrough sin re-renderizar
          cmd.videoCodec('copy')
          break
        default:
          cmd.videoCodec(videoCodec) // Cae a libx264 (CPU) por defecto
      }
    }

    // 2. Lógica de Audio y Normalización
    if (audioCodec && audioCodec !== 'copy') cmd.audioCodec(audioCodec)
    else if (audioCodec === 'copy') cmd.audioCodec('copy')

    if (normalizeAudio) {
      // Plancha el audio al estándar Broadcast EBU R128 (-23 LUFS)
      cmd.audioFilters('loudnorm=I=-23:LRA=7:TP=-2.0')
    }

    // Los formatos intra-frame (ProRes, HAP) asfixian FFmpeg si les fuerzas un bitrate, así que lo evitamos
    if (videoBitrate && !['prores', 'hap'].includes(videoCodec)) {
      cmd.videoBitrate(videoBitrate)
    }
    if (audioBitrate) cmd.audioBitrate(audioBitrate)

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark, fps: p.currentFps }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(output)
  })
})

// ── Compress ──────────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:compress', async (_, { input, output, crf, preset, resolution, audioBitrate, removeAudio }) => {
  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input)
      .videoCodec('libx264')
      .outputOptions([`-crf ${crf || 23}`, `-preset ${preset || 'medium'}`])

    if (resolution && resolution !== 'original') cmd.size(resolution)
    if (removeAudio) cmd.noAudio()
    else if (audioBitrate) cmd.audioBitrate(audioBitrate)

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark, fps: p.currentFps }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(output)
  })
})

// ── Convert Audio ──────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:convert-audio', async (_, { input, output, codec, bitrate, sampleRate, mono, normalize }) => {
  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input).noVideo().audioCodec(codec)

    if (bitrate) cmd.audioBitrate(bitrate)
    if (sampleRate) cmd.audioFrequency(+sampleRate)
    if (mono) cmd.audioChannels(1)
    if (normalize) cmd.audioFilters('loudnorm=I=-23:LRA=7:TP=-2.0')

    activeCmd = cmd
    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark, fps: p.currentFps }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(output)
  })
})

// ── Trim ───────────────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:trim', async (_, { input, output, startTime, endTime }) => {
  return new Promise((resolve, reject) => {
    const duration = timeToSeconds(endTime) - timeToSeconds(startTime)

    let cmd = ffmpeg(input)
      .setStartTime(startTime)
      .setDuration(duration)
      .outputOptions(['-c copy'])

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(output)
  })
})

// ── Merge ──────────────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:merge', async (_, { inputs, output }) => {
  return new Promise((resolve, reject) => {
    const cmd = ffmpeg()
    inputs.forEach(f => cmd.input(f))

    const tmpDir = path.dirname(output)

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .mergeToFile(output, tmpDir)
  })
})

// ── Extract Audio ──────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:extract-audio', async (_, { input, output, format, bitrate }) => {
  const codecMap = { mp3: 'libmp3lame', aac: 'aac', flac: 'flac', wav: 'pcm_s16le', ogg: 'libvorbis' }

  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input)
      .noVideo()
      .audioCodec(codecMap[format] || 'libmp3lame')

    if (format !== 'wav' && format !== 'flac') cmd.audioBitrate(bitrate || '192k')

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(output)
  })
})

// ── Extract Frames ─────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:extract-frames', async (_, { input, outputDir, fps, quality, format }) => {
  const ext = format || 'jpg'
  const outPattern = path.join(outputDir, `frame_%05d.${ext}`)

  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input)
      .fps(parseFloat(fps) || 1)
      .outputOptions([`-q:v ${quality || 2}`])

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark }))
      .on('end', () => { activeCmd = null; sendDone(outputDir); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(outPattern)
  })
})

// ── Image Convert (JPG, PNG, WEBP, ICO) ──────────────────────────────────────
ipcMain.handle('ffmpeg:convert-image', async (_, { input, output, format }) => {
  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input)

    // Lógica específica para iconos (.ico)
    // Windows agradece que los iconos sean cuadrados y de 256px
    if (format === 'ico') {
      cmd.size('256x256').aspect('1:1')
    }

    activeCmd = cmd

    cmd
      .on('end', () => { 
        activeCmd = null; 
        resolve({ success: true }) 
      })
      .on('error', err => { 
        activeCmd = null; 
        reject(err.message) 
      })
      .save(output)
  })
})

// ── FFmpeg custom path & encoder detection ─────────────────────────────────────
ipcMain.handle('ffmpeg:available-encoders', () => [...availableEncoders])

// ── Thumbnail ──────────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:thumbnail', async (_, { input }) => {
  const os = require('os')
  const tmpFile = path.join(os.tmpdir(), `ffv_thumb_${Date.now()}.jpg`)
  return new Promise((resolve) => {
    ffmpeg(input)
      .inputOptions(['-ss', '00:00:03'])
      .outputOptions(['-vframes', '1', '-vf', 'scale=320:-1', '-q:v', '3'])
      .on('end', () => {
        try {
          const buf = fs.readFileSync(tmpFile)
          try { fs.unlinkSync(tmpFile) } catch {}
          resolve('data:image/jpeg;base64,' + buf.toString('base64'))
        } catch { resolve(null) }
      })
      .on('error', () => resolve(null))
      .save(tmpFile)
  })
})

ipcMain.handle('ffmpeg:get-custom-path', () => loadSettings().ffmpegPath || null)

ipcMain.handle('ffmpeg:set-custom-path', async (_, ffmpegExePath) => {
  if (!fs.existsSync(ffmpegExePath)) throw new Error('El archivo no existe')
  saveSettings({ ffmpegPath: ffmpegExePath })
  applyFfmpegPath(ffmpegExePath)
  detectEncoders()
  return [...availableEncoders]
})

ipcMain.handle('ffmpeg:reset-custom-path', () => {
  saveSettings({ ffmpegPath: null })
  try { ffmpegPath = require('ffmpeg-static') } catch {}
  if (ffmpegPath) ffmpeg.setFfmpegPath(ffmpegPath)
  detectEncoders()
  return [...availableEncoders]
})

// ── Count Frames (via ffprobe -count_packets, fast) ────────────────────────────
ipcMain.handle('ffmpeg:count-frames', async (_, { input }) => {
  return new Promise((resolve, reject) => {
    const { spawn } = require('child_process')
    const args = [
      '-v', 'quiet',
      '-select_streams', 'v:0',
      '-count_packets',
      '-show_entries', 'stream=nb_read_packets',
      '-of', 'csv=p=0',
      input
    ]
    const proc = spawn(ffprobePath, args)
    let stdout = ''
    proc.stdout.on('data', chunk => { stdout += chunk.toString() })
    proc.on('close', () => {
      const n = parseInt(stdout.trim(), 10)
      resolve(isNaN(n) ? null : n)
    })
    proc.on('error', err => reject(err.message))
  })
})

// ── Crop Detect ────────────────────────────────────────────────────────────────
ipcMain.handle('ffmpeg:cropdetect', async (_, { input }) => {
  return new Promise((resolve, reject) => {
    const { spawn } = require('child_process')
    const args = [
      '-hide_banner', '-i', input,
      '-vf', 'cropdetect=24:16:0',
      '-f', 'null', '-t', '30', '-'
    ]

    const proc = spawn(ffmpegPath, args)
    let stderr = ''
    proc.stderr.on('data', chunk => { stderr += chunk.toString() })
    proc.on('close', () => {
      const matches = [...stderr.matchAll(/crop=(\d+):(\d+):(\d+):(\d+)/g)]
      if (!matches.length) return resolve(null)
      const last = matches[matches.length - 1]
      resolve({ w: +last[1], h: +last[2], x: +last[3], y: +last[4] })
    })
    proc.on('error', err => reject(err.message))
  })
})

// ── Apply Filters (scale, crop, rotate) ────────────────────────────────────────
ipcMain.handle('ffmpeg:filters', async (_, { input, output, scale, crop, rotation }) => {
  return new Promise((resolve, reject) => {
    let cmd = ffmpeg(input)

    const filters = []

    if (scale) {
      filters.push(`scale=${scale.w}:${scale.h}`)
    }

    if (crop) {
      filters.push(`crop=${crop.w}:${crop.h}:${crop.x}:${crop.y}`)
    }

    if (rotation === 'cw')  filters.push('transpose=1')
    if (rotation === 'ccw') filters.push('transpose=2')
    if (rotation === '180') { filters.push('hflip'); filters.push('vflip') }

    if (filters.length) cmd.videoFilters(filters)
    cmd.audioCodec('copy')

    activeCmd = cmd

    cmd
      .on('progress', p => sendProgress({ percent: Math.min(p.percent || 0, 100), timemark: p.timemark, fps: p.currentFps }))
      .on('end', () => { activeCmd = null; sendDone(output); resolve({ success: true }) })
      .on('error', err => { activeCmd = null; sendError(err.message); reject(err.message) })
      .save(output)
  })
})

// ── Helpers ────────────────────────────────────────────────────────────────────
function timeToSeconds(timeStr) {
  if (!timeStr) return 0
  const parts = timeStr.split(':').map(Number)
  if (parts.length === 3) return parts[0] * 3600 + parts[1] * 60 + parts[2]
  if (parts.length === 2) return parts[0] * 60 + parts[1]
  return parts[0] || 0
}
