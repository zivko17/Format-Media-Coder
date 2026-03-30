import { useState } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

const PRESETS = ['ultrafast', 'superfast', 'veryfast', 'faster', 'fast', 'medium', 'slow', 'slower', 'veryslow']
const RESOLUTIONS = ['original', '1920x1080', '1280x720', '854x480', '640x360']
const A_BITRATES = ['64k', '96k', '128k', '192k', '256k', '320k']

const CRF_LABEL = (v) => {
  if (v <= 18) return { text: 'Calidad visual perfecta', color: '#3fb950' }
  if (v <= 23) return { text: 'Alta calidad', color: '#58a6ff' }
  if (v <= 28) return { text: 'Calidad media', color: 'var(--accent)' }
  if (v <= 35) return { text: 'Compresión alta', color: '#f0883e' }
  return { text: 'Muy comprimido', color: 'var(--error)' }
}

export default function Compress({ processing, setProcessing, showToast }) {
  const [input, setInput] = useState('')
  const [crf, setCrf] = useState(23)
  const [preset, setPreset] = useState('medium')
  const [resolution, setResolution] = useState('original')
  const [audioBitrate, setAudioBitrate] = useState('128k')
  const [removeAudio, setRemoveAudio] = useState(false)

  const crfInfo = CRF_LABEL(crf)

  const run = async () => {
    if (!input) return showToast('error', 'Selecciona un archivo')

    const ext = input.split('.').pop()
    const result = await window.api.saveFile({
      defaultPath: input.replace(/(\.[^.]+)$/, `_compressed.$1`),
      filters: [{ name: 'Video', extensions: [ext, 'mp4', 'mkv'] }]
    })
    if (result.canceled) return

    setProcessing(true)
    try {
      await window.api.compress({
        input, output: result.filePath,
        crf, preset,
        resolution: resolution !== 'original' ? resolution : undefined,
        audioBitrate: removeAudio ? undefined : audioBitrate,
        removeAudio
      })
    } catch (e) {}
  }

  return (
    <div className="animate-in">
      <PageHeader title="Comprimir Video" desc="Reduce el tamaño del archivo controlando calidad y codificación." icon="📦" />

      <FilePicker label="Archivo de entrada" value={input} onChange={setInput} accept="video" />

      <hr className="divider" />

      {/* CRF Slider */}
      <div style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 10 }}>
          <label className="field-label" style={{ marginBottom: 0 }}>Calidad (CRF)</label>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ fontSize: 11, color: crfInfo.color }}>{crfInfo.text}</span>
            <span style={{
              fontFamily: 'JetBrains Mono, monospace',
              fontSize: 18,
              fontWeight: 700,
              color: crfInfo.color,
              minWidth: 30,
              textAlign: 'right'
            }}>{crf}</span>
          </div>
        </div>
        <input type="range" min={0} max={51} value={crf} onChange={e => setCrf(+e.target.value)}
          style={{ '--thumb-color': crfInfo.color }} />
        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
          <span style={{ fontSize: 10, color: 'var(--text-3)' }}>0 — Lossless</span>
          <span style={{ fontSize: 10, color: 'var(--text-3)' }}>51 — Mínima calidad</span>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 20 }}>
        {/* Preset */}
        <div>
          <label className="field-label">Preset de velocidad</label>
          <select value={preset} onChange={e => setPreset(e.target.value)}>
            {PRESETS.map(p => <option key={p} value={p}>{p}</option>)}
          </select>
          <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 4 }}>
            Más lento = mejor compresión
          </div>
        </div>

        {/* Resolution */}
        <div>
          <label className="field-label">Resolución de salida</label>
          <select value={resolution} onChange={e => setResolution(e.target.value)}>
            {RESOLUTIONS.map(r => <option key={r} value={r}>{r === 'original' ? 'Original' : r}</option>)}
          </select>
        </div>

        {/* Audio */}
        <div>
          <label className="field-label">Audio Bitrate</label>
          <select value={audioBitrate} onChange={e => setAudioBitrate(e.target.value)} disabled={removeAudio}>
            {A_BITRATES.map(b => <option key={b} value={b}>{b}</option>)}
          </select>
        </div>

        {/* No audio toggle */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, paddingTop: 20 }}>
          <button
            onClick={() => setRemoveAudio(!removeAudio)}
            style={{
              width: 36, height: 20,
              borderRadius: 10,
              background: removeAudio ? 'var(--accent)' : 'var(--border)',
              border: 'none',
              cursor: 'pointer',
              position: 'relative',
              transition: 'background 0.2s'
            }}
          >
            <span style={{
              position: 'absolute',
              top: 2, left: removeAudio ? 18 : 2,
              width: 16, height: 16,
              borderRadius: '50%',
              background: '#fff',
              transition: 'left 0.2s'
            }} />
          </button>
          <span style={{ fontSize: 13, color: 'var(--text-2)' }}>Sin audio</span>
        </div>
      </div>

      <button className="btn-primary" onClick={run} disabled={processing || !input}>
        {processing ? '⏳ Comprimiendo...' : '▶ Comprimir'}
      </button>
    </div>
  )
}
