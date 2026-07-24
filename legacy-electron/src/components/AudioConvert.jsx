import { useState, useEffect } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

// ── Formats ────────────────────────────────────────────────────────────────────
const FORMATS = [
  { id: 'mp3',  label: 'MP3',  codec: 'libmp3lame', lossy: true,  desc: 'Universal' },
  { id: 'aac',  label: 'AAC',  codec: 'aac',        lossy: true,  desc: 'iOS / streaming' },
  { id: 'm4a',  label: 'M4A',  codec: 'aac',        lossy: true,  desc: 'iTunes / Apple' },
  { id: 'opus', label: 'OPUS', codec: 'libopus',    lossy: true,  desc: 'VoIP / Discord' },
  { id: 'ogg',  label: 'OGG',  codec: 'libvorbis',  lossy: true,  desc: 'Open source' },
  { id: 'flac', label: 'FLAC', codec: 'flac',       lossy: false, desc: 'Lossless' },
  { id: 'wav',  label: 'WAV',  codec: 'pcm_s16le',  lossy: false, desc: 'Sin comprimir' },
  { id: 'aiff', label: 'AIFF', codec: 'pcm_s16be',  lossy: false, desc: 'Mac lossless' },
]

const BITRATES  = ['64k', '96k', '128k', '160k', '192k', '256k', '320k']
const SAMPLERATES = [
  { label: '44.1 kHz', value: '44100' },
  { label: '48 kHz',   value: '48000' },
  { label: '96 kHz',   value: '96000' },
  { label: 'Original', value: '' },
]

// ── Helpers ────────────────────────────────────────────────────────────────────
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

function autoOut(inputPath, ext) {
  const lastDot = inputPath.lastIndexOf('.')
  const base = lastDot > 0 ? inputPath.substring(0, lastDot) : inputPath
  return `${base}_out.${ext}`
}

// ── AudioPreview ───────────────────────────────────────────────────────────────
function InfoChip({ label, value }) {
  return (
    <div style={{ background: 'var(--bg-app)', borderRadius: 5, padding: '4px 8px' }}>
      <div style={{ fontSize: 9, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>{label}</div>
      <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--text-1)', fontFamily: 'JetBrains Mono, monospace', marginTop: 1 }}>{value || '—'}</div>
    </div>
  )
}

function AudioPreview({ filePath }) {
  const [meta, setMeta] = useState(null)

  useEffect(() => {
    if (!filePath) { setMeta(null); return }
    window.api.probe(filePath).then(setMeta).catch(() => setMeta(null))
  }, [filePath])

  if (!filePath) return null

  const aStream = meta?.streams?.find(s => s.codec_type === 'audio')
  const fmt     = meta?.format

  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 14, padding: 12,
      background: 'var(--bg-card)', border: '1px solid var(--border)',
      borderRadius: 8, marginBottom: 16
    }}>
      <div style={{
        width: 48, height: 48, borderRadius: 8, background: 'var(--bg-app)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        fontSize: 24, flexShrink: 0
      }}>🎵</div>

      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-1)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', marginBottom: 8 }}>
          {filePath.split(/[\\/]/).pop()}
        </div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 5 }}>
          {aStream && <>
            <InfoChip label="Codec" value={aStream.codec_name?.toUpperCase()} />
            <InfoChip label="Sample Rate" value={aStream.sample_rate ? `${(+aStream.sample_rate / 1000).toFixed(1)} kHz` : null} />
            <InfoChip label="Canales" value={aStream.channels === 1 ? 'Mono' : aStream.channels === 2 ? 'Stereo' : aStream.channels ? `${aStream.channels}ch` : null} />
            {aStream.bit_rate && <InfoChip label="Bitrate" value={formatBitrate(+aStream.bit_rate)} />}
          </>}
          {fmt?.duration && <InfoChip label="Duración" value={formatDuration(+fmt.duration)} />}
          {fmt?.size     && <InfoChip label="Tamaño"   value={formatSize(+fmt.size)} />}
        </div>
      </div>
    </div>
  )
}

// ── QueueItem ──────────────────────────────────────────────────────────────────
const STATUS = {
  pending:    { color: 'var(--text-3)', icon: '○' },
  processing: { color: 'var(--accent)', icon: '●' },
  done:       { color: '#3fb950',       icon: '✓' },
  error:      { color: 'var(--error)',  icon: '✕' },
}

function QueueItem({ item, active, onClick, onRemove }) {
  const st = STATUS[item.status] || STATUS.pending
  return (
    <div onClick={onClick} style={{
      display: 'flex', alignItems: 'center', gap: 8,
      padding: '7px 10px', borderRadius: 6, cursor: 'pointer',
      background: active ? 'var(--accent-dim)' : 'transparent',
      border: `1px solid ${active ? 'var(--accent)' : 'transparent'}`,
      transition: 'all 0.1s'
    }}>
      <span style={{ fontSize: 14, color: st.color, flexShrink: 0 }}>{st.icon}</span>
      <span style={{ flex: 1, fontSize: 12, color: 'var(--text-2)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {item.path.split(/[\\/]/).pop()}
      </span>
      {item.error && (
        <span style={{ fontSize: 10, color: 'var(--error)', maxWidth: 120, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={item.error}>
          {item.error}
        </span>
      )}
      {item.status !== 'processing' && (
        <button onClick={e => { e.stopPropagation(); onRemove(item.id) }}
          style={{ background: 'none', border: 'none', color: 'var(--text-3)', cursor: 'pointer', fontSize: 14, lineHeight: 1, padding: '0 2px', flexShrink: 0 }}>×</button>
      )}
    </div>
  )
}

// ── Main ───────────────────────────────────────────────────────────────────────
export default function AudioConvert({ processing, setProcessing, showToast, setCurrentFile, setQueueInfo, setBatchMode }) {
  const [queue, setQueue]           = useState([])
  const [previewPath, setPreviewPath] = useState(null)

  const [format, setFormat]           = useState('mp3')
  const [bitrate, setBitrate]         = useState('192k')
  const [sampleRate, setSampleRate]   = useState('')
  const [mono, setMono]               = useState(false)
  const [normalize, setNormalize]     = useState(false)

  const currentFmt = FORMATS.find(f => f.id === format) || FORMATS[0]

  // ── Queue ──────────────────────────────────────────────────────────────────
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

  // ── Batch run ─────────────────────────────────────────────────────────────
  const run = async () => {
    const pending = queue.filter(q => q.status === 'pending')
    if (!pending.length) return showToast('error', 'No hay archivos pendientes')

    setBatchMode?.(true)
    setProcessing(true)

    let doneCount = 0, errCount = 0

    for (let i = 0; i < pending.length; i++) {
      const item = pending[i]
      const out  = autoOut(item.path, format)

      setQueue(prev => prev.map(q => q.id === item.id ? { ...q, status: 'processing' } : q))
      setCurrentFile?.(item.path)
      setQueueInfo?.(pending.length > 1 ? `Archivo ${i + 1} de ${pending.length}` : '')

      try {
        await window.api.convertAudio({
          input:      item.path,
          output:     out,
          codec:      currentFmt.codec,
          bitrate:    currentFmt.lossy ? bitrate : undefined,
          sampleRate: sampleRate || undefined,
          mono,
          normalize,
        })
        setQueue(prev => prev.map(q => q.id === item.id ? { ...q, status: 'done' } : q))
        doneCount++
      } catch (e) {
        const errMsg = String(e.message || e)
        setQueue(prev => prev.map(q => q.id === item.id ? { ...q, status: 'error', error: errMsg } : q))
        errCount++
      }
    }

    setBatchMode?.(false)
    setProcessing(false)
    setCurrentFile?.('')
    setQueueInfo?.('')

    if (errCount > 0) showToast('error', `Lote: ${doneCount} ok, ${errCount} error(es)`)
    else showToast('success', `Lote completado — ${doneCount} archivo(s)`)
  }

  const pendingCount = queue.filter(q => q.status === 'pending').length

  return (
    <div className="animate-in">
      <PageHeader title="Convertir Audio" desc="Convierte entre formatos de audio con control total sobre calidad y codificación." icon="🎵" />

      {/* Preview */}
      <AudioPreview filePath={previewPath} />

      {/* File picker */}
      <FilePicker
        label={queue.length ? `Cola: ${queue.length} archivo(s)` : 'Archivos de entrada'}
        value={queue.map(q => q.path)}
        onChange={addFiles}
        accept="media"
        multiple
      />

      {/* Queue list */}
      {queue.length > 0 && (
        <div style={{ marginTop: 8, background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 8, padding: 6, maxHeight: 160, overflowY: 'auto', marginBottom: 16 }}>
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

      {/* Format selector */}
      <div style={{ marginBottom: 20 }}>
        <label className="field-label">Formato de salida</label>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 6 }}>
          {FORMATS.map(f => {
            const active = format === f.id
            return (
              <button key={f.id} onClick={() => setFormat(f.id)} style={{
                padding: '10px 8px', borderRadius: 6, cursor: 'pointer', textAlign: 'center',
                border: `1px solid ${active ? 'var(--accent)' : 'var(--border)'}`,
                background: active ? 'var(--accent-dim)' : 'var(--bg-input)',
                transition: 'all 0.12s'
              }}>
                <div style={{ fontSize: 13, fontWeight: active ? 700 : 500, fontFamily: 'JetBrains Mono, monospace', color: active ? 'var(--accent)' : 'var(--text-1)', marginBottom: 2 }}>
                  {f.label}
                </div>
                <div style={{ fontSize: 9, color: active ? 'var(--accent)' : 'var(--text-3)' }}>{f.desc}</div>
                {!f.lossy && (
                  <div style={{ fontSize: 9, color: '#3fb950', marginTop: 2 }}>lossless</div>
                )}
              </button>
            )
          })}
        </div>
      </div>

      {/* Options grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 20 }}>

        {/* Bitrate — solo lossy */}
        {currentFmt.lossy ? (
          <div>
            <label className="field-label">Bitrate</label>
            <select value={bitrate} onChange={e => setBitrate(e.target.value)}>
              {BITRATES.map(b => <option key={b} value={b}>{b}</option>)}
            </select>
          </div>
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', padding: '8px 12px', background: 'rgba(63,185,80,0.08)', border: '1px solid rgba(63,185,80,0.2)', borderRadius: 6 }}>
            <span style={{ fontSize: 12, color: '#3fb950' }}>✓ Formato sin pérdida — bitrate no aplica</span>
          </div>
        )}

        {/* Sample rate */}
        <div>
          <label className="field-label">Sample Rate</label>
          <select value={sampleRate} onChange={e => setSampleRate(e.target.value)}>
            {SAMPLERATES.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
          </select>
        </div>
      </div>

      {/* Toggles */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 24 }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', color: 'var(--text-2)' }}>
          <input type="checkbox" checked={mono} onChange={e => setMono(e.target.checked)}
            style={{ width: 16, height: 16, accentColor: 'var(--accent)' }} />
          <div>
            <span style={{ fontSize: 13, fontWeight: 500 }}>Mezclar a Mono</span>
            <p style={{ fontSize: 11, color: 'var(--text-3)', margin: '2px 0 0' }}>Útil para podcasts, voz en off o locuciones.</p>
          </div>
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', color: 'var(--text-2)' }}>
          <input type="checkbox" checked={normalize} onChange={e => setNormalize(e.target.checked)}
            style={{ width: 16, height: 16, accentColor: 'var(--accent)' }} />
          <div>
            <span style={{ fontSize: 13, fontWeight: 500 }}>Normalizar (EBU R128 / -23 LUFS)</span>
            <p style={{ fontSize: 11, color: 'var(--text-3)', margin: '2px 0 0' }}>Iguala el volumen al estándar broadcast.</p>
          </div>
        </label>
      </div>

      <button className="btn-primary" onClick={run} disabled={processing || pendingCount === 0}>
        {processing
          ? '⏳ Convirtiendo...'
          : pendingCount === 1
            ? `▶ Convertir a ${format.toUpperCase()}`
            : `▶ Convertir ${pendingCount} archivo(s) a ${format.toUpperCase()}`
        }
      </button>
    </div>
  )
}
