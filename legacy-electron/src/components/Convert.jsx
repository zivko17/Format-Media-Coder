import { useState, useEffect, useRef } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

// ── Constants ──────────────────────────────────────────────────────────────────
const FORMAT_PRESETS = {
  mp4:  { ext: 'mp4',  video: 'libx264',   audio: 'aac',        label: 'MP4 (H.264)' },
  mov:  { ext: 'mov',  video: 'prores',     audio: 'pcm_s16le',  label: 'MOV (ProRes)' },
  hap:  { ext: 'mov',  video: 'hap',        audio: 'pcm_s16le',  label: 'MOV (HAP)' },
  mkv:  { ext: 'mkv',  video: 'copy',       audio: 'copy',       label: 'MKV (stream copy)' },
  webm: { ext: 'webm', video: 'libvpx-vp9', audio: 'libopus',    label: 'WebM (VP9)' },
  mp3:  { ext: 'mp3',  video: null,          audio: 'libmp3lame', label: 'MP3 (audio)' },
  aac:  { ext: 'aac',  video: null,          audio: 'aac',        label: 'AAC (audio)' },
  wav:  { ext: 'wav',  video: null,          audio: 'pcm_s16le',  label: 'WAV (audio)' },
}

const CONTAINER_CODECS = {
  mp4:  { video: ['libx264', 'h264_nvenc', 'hevc_nvenc', 'libx265', 'mpeg4', 'copy'],      audio: ['aac', 'libmp3lame', 'copy'] },
  mov:  { video: ['prores', 'libx264', 'h264_nvenc', 'hevc_nvenc', 'libx265', 'copy'],     audio: ['pcm_s16le', 'aac', 'libmp3lame', 'copy'] },
  hap:  { video: ['hap'],                                                                   audio: ['pcm_s16le', 'aac', 'copy'] },
  mkv:  { video: ['libx264', 'h264_nvenc', 'hevc_nvenc', 'libx265', 'libvpx-vp9', 'libxvid', 'copy'], audio: ['aac', 'libmp3lame', 'libopus', 'libvorbis', 'flac', 'pcm_s16le', 'copy'] },
  webm: { video: ['libvpx-vp9', 'copy'],                                                   audio: ['libopus', 'libvorbis'] },
  mp3:  { video: [], audio: ['libmp3lame'] },
  aac:  { video: [], audio: ['aac'] },
  wav:  { video: [], audio: ['pcm_s16le'] },
}

const VIDEO_CODEC_LABELS = {
  'copy': 'copy — sin re-encodear', 'libx264': 'libx264 — H.264 (CPU)',
  'h264_nvenc': 'h264_nvenc — H.264 (NVIDIA)', 'hevc_nvenc': 'hevc_nvenc — H.265 (NVIDIA)',
  'prores': 'prores — ProRes HQ', 'hap': 'hap — HAP', 'libx265': 'libx265 — H.265 (CPU)',
  'libvpx-vp9': 'libvpx-vp9 — VP9', 'libxvid': 'libxvid — XviD', 'mpeg4': 'mpeg4 — MPEG-4',
}
const AUDIO_CODEC_LABELS = {
  'copy': 'copy — sin re-encodear', 'aac': 'aac — AAC', 'libmp3lame': 'libmp3lame — MP3',
  'libopus': 'libopus — Opus', 'libvorbis': 'libvorbis — Vorbis',
  'flac': 'flac — FLAC lossless', 'pcm_s16le': 'pcm_s16le — PCM 16-bit',
}

const V_BITRATES = ['500k', '1000k', '2000k', '4000k', '8000k', '15000k', '25000k']
const A_BITRATES = ['64k', '128k', '192k', '256k', '320k']
const NO_BITRATE_CODECS = ['prores', 'hap', 'copy', 'flac', 'pcm_s16le']

// ── Helpers ────────────────────────────────────────────────────────────────────
const parseFps = (str) => {
  if (!str || str === '0/0') return '—'
  const [n, d] = str.split('/').map(Number)
  const v = d ? n / d : n
  return v % 1 === 0 ? String(v) : v.toFixed(2).replace(/\.?0+$/, '')
}
const formatDuration = (s) => {
  const h = Math.floor(s / 3600), m = Math.floor((s % 3600) / 60), sec = Math.floor(s % 60)
  return [h, m, sec].map(v => String(v).padStart(2, '0')).join(':')
}
const formatSize = (b) => {
  if (b > 1e9) return (b / 1e9).toFixed(2) + ' GB'
  if (b > 1e6) return (b / 1e6).toFixed(1) + ' MB'
  return (b / 1e3).toFixed(0) + ' KB'
}
const formatBitrate = (b) => b > 1e6 ? (b / 1e6).toFixed(1) + ' Mb/s' : (b / 1e3).toFixed(0) + ' kb/s'

function autoOutputPath(inputPath, ext) {
  const lastDot = inputPath.lastIndexOf('.')
  const base = lastDot > 0 ? inputPath.substring(0, lastDot) : inputPath
  return `${base}_out.${ext}`
}

// ── FilePreview ────────────────────────────────────────────────────────────────
function InfoChip({ label, value }) {
  return (
    <div style={{ background: 'var(--bg-app)', borderRadius: 5, padding: '4px 8px' }}>
      <div style={{ fontSize: 9, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>{label}</div>
      <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--text-1)', fontFamily: 'JetBrains Mono, monospace', marginTop: 1 }}>{value || '—'}</div>
    </div>
  )
}

function FilePreview({ filePath }) {
  const [thumb, setThumb] = useState(null)
  const [meta, setMeta]   = useState(null)
  const [busy, setBusy]   = useState(false)

  useEffect(() => {
    if (!filePath) { setThumb(null); setMeta(null); return }
    setBusy(true)
    Promise.all([
      window.api.probe(filePath).catch(() => null),
      window.api.thumbnail({ input: filePath }).catch(() => null),
    ]).then(([m, t]) => { setMeta(m); setThumb(t); setBusy(false) })
  }, [filePath])

  if (!filePath) return null

  const vStream = meta?.streams?.find(s => s.codec_type === 'video')
  const aStream = meta?.streams?.find(s => s.codec_type === 'audio')
  const fmt = meta?.format

  return (
    <div style={{
      display: 'flex', gap: 14, padding: 12,
      background: 'var(--bg-card)', border: '1px solid var(--border)',
      borderRadius: 8, marginBottom: 16
    }}>
      {/* Thumbnail */}
      <div style={{
        width: 148, height: 84, borderRadius: 6, overflow: 'hidden',
        background: 'var(--bg-app)', flexShrink: 0,
        display: 'flex', alignItems: 'center', justifyContent: 'center'
      }}>
        {busy
          ? <div style={{ fontSize: 10, color: 'var(--text-3)' }}>Cargando...</div>
          : thumb
            ? <img src={thumb} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
            : <span style={{ fontSize: 28 }}>{vStream ? '🎬' : '🎵'}</span>
        }
      </div>

      {/* Metadata */}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-1)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 8 }}>
          {filePath.split(/[\\/]/).pop()}
        </div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 5 }}>
          {vStream && <>
            <InfoChip label="Resolución" value={vStream.width ? `${vStream.width}×${vStream.height}` : null} />
            <InfoChip label="Codec V" value={vStream.codec_name?.toUpperCase()} />
            <InfoChip label="FPS" value={parseFps(vStream.r_frame_rate)} />
          </>}
          {aStream && <InfoChip label="Codec A" value={aStream.codec_name?.toUpperCase()} />}
          {fmt?.duration && <InfoChip label="Duración" value={formatDuration(+fmt.duration)} />}
          {fmt?.size && <InfoChip label="Tamaño" value={formatSize(+fmt.size)} />}
          {fmt?.bit_rate && <InfoChip label="Bitrate" value={formatBitrate(+fmt.bit_rate)} />}
        </div>
      </div>
    </div>
  )
}

// ── QueueItem ──────────────────────────────────────────────────────────────────
const STATUS_STYLE = {
  pending:    { color: 'var(--text-3)', icon: '○' },
  processing: { color: 'var(--accent)', icon: '●' },
  done:       { color: '#3fb950',       icon: '✓' },
  error:      { color: 'var(--error)',  icon: '✕' },
}

function QueueItem({ item, active, onClick, onRemove }) {
  const st = STATUS_STYLE[item.status] || STATUS_STYLE.pending
  return (
    <div
      onClick={onClick}
      style={{
        display: 'flex', alignItems: 'center', gap: 8,
        padding: '7px 10px', borderRadius: 6, cursor: 'pointer',
        background: active ? 'var(--accent-dim)' : 'transparent',
        border: `1px solid ${active ? 'var(--accent)' : 'transparent'}`,
        transition: 'all 0.1s'
      }}
    >
      <span style={{ fontSize: 14, color: st.color, flexShrink: 0, animation: item.status === 'processing' ? 'pulse 1.2s ease-in-out infinite' : 'none' }}>
        {st.icon}
      </span>
      <span style={{ flex: 1, fontSize: 12, color: 'var(--text-2)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {item.path.split(/[\\/]/).pop()}
      </span>
      {item.status === 'error' && (
        <span style={{ fontSize: 10, color: 'var(--error)', maxWidth: 120, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={item.error}>
          {item.error}
        </span>
      )}
      {item.status !== 'processing' && (
        <button
          onClick={e => { e.stopPropagation(); onRemove(item.id) }}
          style={{ background: 'none', border: 'none', color: 'var(--text-3)', cursor: 'pointer', fontSize: 14, lineHeight: 1, padding: '0 2px', flexShrink: 0 }}
        >×</button>
      )}
    </div>
  )
}

// ── Main Component ─────────────────────────────────────────────────────────────
export default function Convert({ processing, setProcessing, showToast, setCurrentFile, setQueueInfo, setBatchMode }) {
  const [queue, setQueue]           = useState([])
  const [previewPath, setPreviewPath] = useState(null)
  const batchRunning                = useRef(false)

  // Settings
  const [format, setFormat]             = useState('mp4')
  const [videoCodec, setVideoCodec]     = useState('libx264')
  const [audioCodec, setAudioCodec]     = useState('aac')
  const [videoBitrate, setVideoBitrate] = useState('')
  const [audioBitrate, setAudioBitrate] = useState('')
  const [normalizeAudio, setNormalizeAudio] = useState(false)

  // Encoder awareness
  const [encoders, setEncoders]   = useState(null)
  const [customPath, setCustomPath] = useState(null)

  useEffect(() => {
    window.api.getAvailableEncoders().then(list => setEncoders(new Set(list)))
    window.api.getCustomFfmpegPath().then(p => setCustomPath(p))
  }, [])

  // ── Queue management ─────────────────────────────────────────────────────────
  const addFiles = (paths) => {
    const arr = Array.isArray(paths) ? paths : [paths]
    setQueue(prev => {
      const existing = new Set(prev.map(i => i.path))
      const fresh = arr.filter(p => !existing.has(p)).map(p => ({
        id: `${Date.now()}_${Math.random()}`, path: p, status: 'pending', error: null
      }))
      if (!previewPath && fresh.length) setPreviewPath(fresh[0].path)
      return [...prev, ...fresh]
    })
  }

  const removeFromQueue = (id) => {
    setQueue(prev => {
      const next = prev.filter(i => i.id !== id)
      if (previewPath && !next.find(i => i.path === previewPath)) {
        setPreviewPath(next[0]?.path ?? null)
      }
      return next
    })
  }

  const handleFormatChange = (f) => {
    setFormat(f)
    const preset = FORMAT_PRESETS[f]
    const allowed = CONTAINER_CODECS[f]
    if (preset.video) setVideoCodec(prev => allowed.video.includes(prev) ? prev : preset.video)
    if (preset.audio) setAudioCodec(prev => allowed.audio.includes(prev) ? prev : preset.audio)
    setVideoBitrate(''); setAudioBitrate('')
  }

  // ── Batch run ────────────────────────────────────────────────────────────────
  const runBatch = async () => {
    const pending = queue.filter(q => q.status === 'pending')
    if (!pending.length) return showToast('error', 'No hay archivos pendientes')

    batchRunning.current = true
    setBatchMode?.(true)
    setProcessing(true)

    let doneCount = 0
    let errCount  = 0

    for (let i = 0; i < pending.length; i++) {
      const item = pending[i]
      const ext  = FORMAT_PRESETS[format].ext
      const out  = autoOutputPath(item.path, ext)

      setQueue(prev => prev.map(q => q.id === item.id ? { ...q, status: 'processing' } : q))
      setCurrentFile?.(item.path)
      setQueueInfo?.(pending.length > 1 ? `Archivo ${i + 1} de ${pending.length}` : '')

      try {
        await window.api.convert({
          input:        item.path,
          output:       out,
          videoCodec:   FORMAT_PRESETS[format]?.video ? videoCodec : undefined,
          audioCodec,
          videoBitrate: videoBitrate || undefined,
          audioBitrate: audioBitrate || undefined,
          normalizeAudio,
        })
        setQueue(prev => prev.map(q => q.id === item.id ? { ...q, status: 'done' } : q))
        doneCount++
      } catch (e) {
        const errMsg = String(e.message || e)
        setQueue(prev => prev.map(q => q.id === item.id ? { ...q, status: 'error', error: errMsg } : q))
        errCount++
      }
    }

    batchRunning.current = false
    setBatchMode?.(false)
    setProcessing(false)
    setCurrentFile?.('')
    setQueueInfo?.('')

    if (errCount > 0) showToast('error', `Lote: ${doneCount} ok, ${errCount} error(es)`)
    else showToast('success', `Lote completado — ${doneCount} archivo(s)`)
  }

  const pickCustomFfmpeg = async () => {
    const result = await window.api.openFile({ filters: [{ name: 'FFmpeg', extensions: ['exe'] }, { name: 'All', extensions: ['*'] }] })
    if (result.canceled || !result.filePaths.length) return
    const newEncoders = await window.api.setCustomFfmpegPath(result.filePaths[0])
    setEncoders(new Set(newEncoders)); setCustomPath(result.filePaths[0])
    showToast('success', 'FFmpeg personalizado configurado')
  }

  const resetFfmpeg = async () => {
    const newEncoders = await window.api.resetCustomFfmpegPath()
    setEncoders(new Set(newEncoders)); setCustomPath(null)
    showToast('success', 'Restaurado al FFmpeg incluido')
  }

  // ── Derived ──────────────────────────────────────────────────────────────────
  const isAudioOnly       = !FORMAT_PRESETS[format]?.video
  const allowedVideo      = CONTAINER_CODECS[format]?.video ?? []
  const allowedAudio      = CONTAINER_CODECS[format]?.audio ?? []
  const disableVBit       = NO_BITRATE_CODECS.includes(videoCodec)
  const disableABit       = NO_BITRATE_CODECS.includes(audioCodec)
  const videoAvailable    = isAudioOnly || !encoders || videoCodec === 'copy' || encoders.has(videoCodec)
  const showFfmpegBanner  = !isAudioOnly && encoders !== null && !videoAvailable
  const pendingCount      = queue.filter(q => q.status === 'pending').length
  const isRunning         = processing || batchRunning.current

  return (
    <div className="animate-in">
      <PageHeader title="Convertir Formato" desc="Cambia el contenedor o los codecs de tu archivo multimedia." icon="🔄" />

      {/* ── Preview ─────────────────────────────────────────────────────────── */}
      <FilePreview filePath={previewPath} />

      {/* ── File picker ─────────────────────────────────────────────────────── */}
      <FilePicker
        label={queue.length ? `Cola: ${queue.length} archivo(s)` : 'Archivos de entrada'}
        value={queue.map(q => q.path)}
        onChange={addFiles}
        accept="media"
        multiple
      />

      {/* ── Queue list ──────────────────────────────────────────────────────── */}
      {queue.length > 0 && (
        <div style={{ marginTop: 8, background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 8, padding: '6px', maxHeight: 180, overflowY: 'auto', marginBottom: 16 }}>
          {queue.map(item => (
            <QueueItem
              key={item.id}
              item={item}
              active={previewPath === item.path}
              onClick={() => setPreviewPath(item.path)}
              onRemove={removeFromQueue}
            />
          ))}
        </div>
      )}

      <hr className="divider" />

      {/* ── Format selector ─────────────────────────────────────────────────── */}
      <div style={{ marginBottom: 16 }}>
        <label className="field-label">Formato de salida</label>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 6 }}>
          {Object.entries(FORMAT_PRESETS).map(([key, { label }]) => (
            <button key={key} onClick={() => handleFormatChange(key)} title={label} style={{
              padding: '8px 4px', borderRadius: 6,
              border: `1px solid ${format === key ? 'var(--accent)' : 'var(--border)'}`,
              background: format === key ? 'var(--accent-dim)' : 'var(--bg-input)',
              color: format === key ? 'var(--accent)' : 'var(--text-2)',
              cursor: 'pointer', fontSize: 11,
              fontWeight: format === key ? 600 : 400,
              fontFamily: 'JetBrains Mono, monospace',
              textAlign: 'center', transition: 'all 0.12s'
            }}>.{key}</button>
          ))}
        </div>
        <div style={{ marginTop: 6, fontSize: 11, color: 'var(--text-3)' }}>{FORMAT_PRESETS[format].label}</div>
      </div>

      {/* ── Codec + bitrate grid ─────────────────────────────────────────────── */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 16 }}>
        {!isAudioOnly && (
          <div>
            <label className="field-label">Video Codec</label>
            <select value={videoCodec} onChange={e => setVideoCodec(e.target.value)}>
              {allowedVideo.map(c => <option key={c} value={c}>{VIDEO_CODEC_LABELS[c] ?? c}</option>)}
            </select>
          </div>
        )}
        <div>
          <label className="field-label">Audio Codec</label>
          <select value={audioCodec} onChange={e => setAudioCodec(e.target.value)}>
            {allowedAudio.map(c => <option key={c} value={c}>{AUDIO_CODEC_LABELS[c] ?? c}</option>)}
          </select>
        </div>
        {!isAudioOnly && (
          <div>
            <label className="field-label">Video Bitrate <span style={{ color: 'var(--text-3)', fontWeight: 400 }}>(opcional)</span></label>
            <select value={videoBitrate} onChange={e => setVideoBitrate(e.target.value)} disabled={disableVBit}>
              <option value="">Auto / Codec default</option>
              {V_BITRATES.map(b => <option key={b} value={b}>{b}</option>)}
            </select>
            {disableVBit && <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 3 }}>{videoCodec} gestiona la calidad internamente</div>}
          </div>
        )}
        <div>
          <label className="field-label">Audio Bitrate <span style={{ color: 'var(--text-3)', fontWeight: 400 }}>(opcional)</span></label>
          <select value={audioBitrate} onChange={e => setAudioBitrate(e.target.value)} disabled={disableABit}>
            <option value="">Auto / Codec default</option>
            {A_BITRATES.map(b => <option key={b} value={b}>{b}</option>)}
          </select>
          {disableABit && <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 3 }}>{audioCodec} es lossless o stream copy</div>}
        </div>
      </div>

      {/* ── Normalize ──────────────────────────────────────────────────────────── */}
      <div style={{ marginBottom: 16 }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', color: 'var(--text-2)' }}>
          <input type="checkbox" checked={normalizeAudio} onChange={e => setNormalizeAudio(e.target.checked)} style={{ width: 16, height: 16, accentColor: 'var(--accent)' }} />
          <span style={{ fontSize: 13, fontWeight: 500 }}>Normalizar Audio (EBU R128 / -23 LUFS)</span>
        </label>
        <p style={{ fontSize: 11, color: 'var(--text-3)', marginLeft: 24, marginTop: 4 }}>Útil para igualar el volumen de vídeos dispares en un evento.</p>
      </div>

      {/* ── FFmpeg banner ──────────────────────────────────────────────────────── */}
      {showFfmpegBanner && (
        <div style={{ padding: '12px 16px', borderRadius: 8, border: '1px solid #f0883e55', background: '#f0883e11', marginBottom: 16 }}>
          <div style={{ fontSize: 13, color: '#f0883e', fontWeight: 600, marginBottom: 6 }}>
            ⚠️ El codec <code style={{ fontFamily: 'JetBrains Mono, monospace', background: '#f0883e22', padding: '1px 5px', borderRadius: 4 }}>{videoCodec}</code> no está disponible en el FFmpeg incluido
          </div>
          <div style={{ fontSize: 11, color: 'var(--text-3)', marginBottom: 10, lineHeight: 1.5 }}>
            Necesitas el <strong style={{ color: 'var(--text-2)' }}>full_build</strong> de gyan.dev/ffmpeg. Selecciónalo abajo.
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button onClick={pickCustomFfmpeg} style={{ padding: '6px 14px', borderRadius: 6, cursor: 'pointer', fontSize: 12, border: '1px solid #f0883e88', background: '#f0883e22', color: '#f0883e', fontWeight: 600 }}>
              📁 Seleccionar ffmpeg.exe personalizado
            </button>
            {customPath && <button onClick={resetFfmpeg} style={{ padding: '6px 12px', borderRadius: 6, cursor: 'pointer', fontSize: 11, border: '1px solid var(--border)', background: 'transparent', color: 'var(--text-3)' }}>Restaurar incluido</button>}
          </div>
          {customPath && <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 6, fontFamily: 'JetBrains Mono, monospace' }}>Activo: {customPath}</div>}
        </div>
      )}

      {/* ── Action button ─────────────────────────────────────────────────────── */}
      <button
        className="btn-primary"
        onClick={runBatch}
        disabled={isRunning || queue.length === 0 || pendingCount === 0}
      >
        {isRunning
          ? '⏳ Procesando...'
          : pendingCount === 1
            ? `▶ Convertir a .${FORMAT_PRESETS[format].ext}`
            : `▶ Convertir ${pendingCount} archivo(s) a .${FORMAT_PRESETS[format].ext}`
        }
      </button>
    </div>
  )
}
