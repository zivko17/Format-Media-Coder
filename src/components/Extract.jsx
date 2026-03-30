import { useState } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

export default function Extract({ processing, setProcessing, showToast }) {
  const [mode, setMode] = useState('audio')

  return (
    <div className="animate-in">
      <PageHeader title="Extraer Audio" desc="Extrae el audio o captura frames del video." icon="🎯" />

      <div style={{ display: 'flex', gap: 8, marginBottom: 24, borderBottom: '1px solid var(--border)', paddingBottom: 16 }}>
        {[['audio', '🎵 Extraer Audio'], ['frames', '🖼️ Extraer Frames']].map(([id, label]) => (
          <button
            key={id}
            onClick={() => setMode(id)}
            style={{
              padding: '8px 18px',
              borderRadius: 6,
              border: `1px solid ${mode === id ? 'var(--accent)' : 'var(--border)'}`,
              background: mode === id ? 'var(--accent-dim)' : 'transparent',
              color: mode === id ? 'var(--accent)' : 'var(--text-2)',
              cursor: 'pointer',
              fontSize: 13,
              fontWeight: mode === id ? 600 : 400,
              transition: 'all 0.12s'
            }}
          >{label}</button>
        ))}
      </div>

      {mode === 'audio'
        ? <AudioPanel processing={processing} setProcessing={setProcessing} showToast={showToast} />
        : <FramesPanel processing={processing} setProcessing={setProcessing} showToast={showToast} />}
    </div>
  )
}

function AudioPanel({ processing, setProcessing, showToast }) {
  const [input, setInput] = useState('')
  const [format, setFormat] = useState('mp3')
  const [bitrate, setBitrate] = useState('192k')

  const FORMATS = [
    { id: 'mp3',  label: 'MP3',  desc: 'Compatible universal' },
    { id: 'aac',  label: 'AAC',  desc: 'iOS / YouTube' },
    { id: 'wav',  label: 'WAV',  desc: 'Sin pérdida (grande)' },
    { id: 'flac', label: 'FLAC', desc: 'Sin pérdida (comprimido)' },
    { id: 'ogg',  label: 'OGG',  desc: 'Open source' },
  ]

  const run = async () => {
    if (!input) return showToast('error', 'Selecciona un archivo')

    const result = await window.api.saveFile({
      defaultPath: input.replace(/\.[^.]+$/, '') + '.' + format,
      filters: [{ name: 'Audio', extensions: [format] }]
    })
    if (result.canceled) return

    setProcessing(true)
    try {
      await window.api.extractAudio({ input, output: result.filePath, format, bitrate })
    } catch (e) {}
  }

  const isLossless = format === 'wav' || format === 'flac'

  return (
    <div>
      <FilePicker label="Archivo de entrada (video)" value={input} onChange={setInput} accept="video" />
      <hr className="divider" />

      <div style={{ marginBottom: 16 }}>
        <label className="field-label">Formato de audio</label>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 6 }}>
          {FORMATS.map(f => (
            <button
              key={f.id}
              onClick={() => setFormat(f.id)}
              style={{
                padding: '10px 8px',
                borderRadius: 6,
                border: `1px solid ${format === f.id ? 'var(--accent)' : 'var(--border)'}`,
                background: format === f.id ? 'var(--accent-dim)' : 'var(--bg-input)',
                color: format === f.id ? 'var(--accent)' : 'var(--text-2)',
                cursor: 'pointer',
                fontSize: 11,
                fontWeight: format === f.id ? 700 : 400,
                fontFamily: 'JetBrains Mono, monospace',
                textAlign: 'center',
                transition: 'all 0.12s'
              }}
            >
              <div style={{ fontSize: 13, marginBottom: 3 }}>{f.label}</div>
              <div style={{ fontSize: 9, opacity: 0.7, fontFamily: 'DM Sans, sans-serif' }}>{f.desc}</div>
            </button>
          ))}
        </div>
      </div>

      {!isLossless && (
        <div style={{ marginBottom: 20, maxWidth: 260 }}>
          <label className="field-label">Bitrate</label>
          <select value={bitrate} onChange={e => setBitrate(e.target.value)}>
            {['64k', '96k', '128k', '192k', '256k', '320k'].map(b => <option key={b} value={b}>{b}</option>)}
          </select>
        </div>
      )}

      {isLossless && (
        <div style={{ padding: '10px 14px', background: 'rgba(63,185,80,0.08)', border: '1px solid rgba(63,185,80,0.2)', borderRadius: 6, marginBottom: 16, fontSize: 12, color: 'var(--success)' }}>
          ✓ Formato sin pérdida — se ignorará el bitrate
        </div>
      )}

      <button className="btn-primary" onClick={run} disabled={processing || !input}>
        {processing ? '⏳ Extrayendo...' : '▶ Extraer Audio'}
      </button>
    </div>
  )
}

function FramesPanel({ processing, setProcessing, showToast }) {
  const [input, setInput] = useState('')
  const [fps, setFps] = useState('1')
  const [quality, setQuality] = useState(2)
  const [format, setFormat] = useState('jpg')

  const run = async () => {
    if (!input) return showToast('error', 'Selecciona un archivo')

    const result = await window.api.openFolder()
    if (result.canceled) return
    const outputDir = result.filePaths[0]

    setProcessing(true)
    try {
      await window.api.extractFrames({ input, outputDir, fps, quality, format })
    } catch (e) {}
  }

  const FPS_PRESETS = [
    { label: '1/s', value: '1', desc: '1 frame/segundo' },
    { label: '5/s', value: '5', desc: '5 frames/seg' },
    { label: '10/s', value: '10', desc: '10 frames/seg' },
    { label: 'All', value: '0', desc: 'Todos los frames' },
    { label: 'Custom', value: 'custom', desc: 'Personalizado' },
  ]

  const [customFps, setCustomFps] = useState(false)
  const [fpsPreset, setFpsPreset] = useState('1')

  const selectPreset = (v) => {
    if (v === 'custom') { setCustomFps(true); return }
    setCustomFps(false)
    setFpsPreset(v)
    setFps(v)
  }

  return (
    <div>
      <FilePicker label="Archivo de entrada (video)" value={input} onChange={setInput} accept="video" />
      <hr className="divider" />

      <div style={{ marginBottom: 16 }}>
        <label className="field-label">Frecuencia de extracción</label>
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: 8 }}>
          {FPS_PRESETS.map(p => (
            <button
              key={p.value}
              onClick={() => selectPreset(p.value)}
              style={{
                padding: '7px 14px',
                borderRadius: 6,
                border: `1px solid ${(fpsPreset === p.value && !customFps) || (p.value === 'custom' && customFps) ? 'var(--accent)' : 'var(--border)'}`,
                background: (fpsPreset === p.value && !customFps) || (p.value === 'custom' && customFps) ? 'var(--accent-dim)' : 'var(--bg-input)',
                color: (fpsPreset === p.value && !customFps) || (p.value === 'custom' && customFps) ? 'var(--accent)' : 'var(--text-2)',
                cursor: 'pointer',
                fontSize: 12,
                fontWeight: 500,
                transition: 'all 0.12s'
              }}
            >{p.label}</button>
          ))}
        </div>
        {customFps && (
          <input type="number" value={fps} onChange={e => setFps(e.target.value)}
            placeholder="ej: 2.5" min="0.1" max="60" step="0.1" style={{ maxWidth: 160 }} />
        )}
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 20 }}>
        <div>
          <label className="field-label">Formato de imagen</label>
          <div style={{ display: 'flex', gap: 6 }}>
            {['jpg', 'png'].map(f => (
              <button key={f} onClick={() => setFormat(f)} style={{
                padding: '8px 18px', borderRadius: 6,
                border: `1px solid ${format === f ? 'var(--accent)' : 'var(--border)'}`,
                background: format === f ? 'var(--accent-dim)' : 'var(--bg-input)',
                color: format === f ? 'var(--accent)' : 'var(--text-2)',
                cursor: 'pointer', fontSize: 12, fontFamily: 'JetBrains Mono, monospace',
                transition: 'all 0.12s'
              }}>.{f}</button>
            ))}
          </div>
        </div>

        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 8 }}>
            <label className="field-label" style={{ marginBottom: 0 }}>Calidad ({format === 'jpg' ? 'JPEG' : 'PNG'})</label>
            <span style={{ fontSize: 12, fontFamily: 'JetBrains Mono, monospace', color: 'var(--accent)' }}>{quality}</span>
          </div>
          <input type="range" min={1} max={format === 'jpg' ? 31 : 9} value={quality} onChange={e => setQuality(+e.target.value)} />
          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 2 }}>
            <span style={{ fontSize: 10, color: 'var(--text-3)' }}>Alta calidad</span>
            <span style={{ fontSize: 10, color: 'var(--text-3)' }}>Baja calidad</span>
          </div>
        </div>
      </div>

      <div style={{ padding: '10px 14px', background: 'rgba(88,166,255,0.08)', border: '1px solid rgba(88,166,255,0.15)', borderRadius: 6, marginBottom: 16, fontSize: 12, color: '#58a6ff' }}>
        💡 Se te pedirá seleccionar una carpeta donde guardar los frames (frame_00001.{format}, frame_00002.{format}...)
      </div>

      <button className="btn-primary" onClick={run} disabled={processing || !input}>
        {processing ? '⏳ Extrayendo frames...' : '▶ Extraer Frames'}
      </button>
    </div>
  )
}
