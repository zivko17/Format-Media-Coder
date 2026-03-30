import { useState } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

export default function TrimMerge({ processing, setProcessing, showToast }) {
  const [mode, setMode] = useState('trim')

  return (
    <div className="animate-in">
      <PageHeader title="Edición" desc="Recorta, une o aplica filtros de imagen a tus archivos." icon="✂️" />

      <div style={{ display: 'flex', gap: 8, marginBottom: 24, borderBottom: '1px solid var(--border)', paddingBottom: 16 }}>
        {[
          ['trim',    '✂️ Recortar'],
          ['merge',   '🔗 Unir archivos'],
          ['filters', '🔬 Cirugía'],
        ].map(([id, label]) => (
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

      {mode === 'trim'    && <TrimPanel    processing={processing} setProcessing={setProcessing} showToast={showToast} />}
      {mode === 'merge'   && <MergePanel   processing={processing} setProcessing={setProcessing} showToast={showToast} />}
      {mode === 'filters' && <FiltersPanel processing={processing} setProcessing={setProcessing} showToast={showToast} />}
    </div>
  )
}

// ── Trim ───────────────────────────────────────────────────────────────────────
function TrimPanel({ processing, setProcessing, showToast }) {
  const [input, setInput] = useState('')
  const [start, setStart] = useState('00:00:00')
  const [end, setEnd]     = useState('00:01:00')

  const run = async () => {
    if (!input) return showToast('error', 'Selecciona un archivo')
    if (!validateTime(start) || !validateTime(end)) return showToast('error', 'Formato de tiempo incorrecto (HH:MM:SS)')
    if (timeToSec(end) <= timeToSec(start)) return showToast('error', 'El tiempo final debe ser mayor que el inicial')

    const ext = input.split('.').pop()
    const result = await window.api.saveFile({
      defaultPath: input.replace(/(\.[^.]+)$/, `_trim.$1`),
      filters: [{ name: 'Video', extensions: [ext] }]
    })
    if (result.canceled) return

    setProcessing(true)
    try {
      await window.api.trim({ input, output: result.filePath, startTime: start, endTime: end })
    } catch (e) {}
  }

  return (
    <div>
      <FilePicker label="Archivo de entrada" value={input} onChange={setInput} accept="video" />
      <hr className="divider" />

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 20 }}>
        <div>
          <label className="field-label">Tiempo de inicio</label>
          <input type="text" value={start} onChange={e => setStart(e.target.value)} placeholder="00:00:00" />
          <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 4 }}>Formato: HH:MM:SS</div>
        </div>
        <div>
          <label className="field-label">Tiempo de fin</label>
          <input type="text" value={end} onChange={e => setEnd(e.target.value)} placeholder="00:01:00" />
          <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 4 }}>Formato: HH:MM:SS</div>
        </div>
      </div>

      {timeToSec(start) >= 0 && timeToSec(end) > timeToSec(start) && (
        <div style={{ padding: '10px 14px', background: 'var(--accent-dim)', border: '1px solid rgba(245,158,11,0.2)', borderRadius: 6, marginBottom: 16, fontSize: 12, color: 'var(--accent)' }}>
          Duración del fragmento: <strong>{formatDuration(timeToSec(end) - timeToSec(start))}</strong>
        </div>
      )}

      <button className="btn-primary" onClick={run} disabled={processing || !input}>
        {processing ? '⏳ Cortando...' : '▶ Recortar'}
      </button>
    </div>
  )
}

// ── Merge ──────────────────────────────────────────────────────────────────────
function MergePanel({ processing, setProcessing, showToast }) {
  const [files, setFiles] = useState([])

  const run = async () => {
    if (files.length < 2) return showToast('error', 'Añade al menos 2 archivos')

    const ext = files[0].split('.').pop()
    const result = await window.api.saveFile({
      defaultPath: `merged.${ext}`,
      filters: [{ name: 'Video', extensions: [ext, 'mp4', 'mkv'] }]
    })
    if (result.canceled) return

    setProcessing(true)
    try {
      await window.api.merge({ inputs: files, output: result.filePath })
    } catch (e) {}
  }

  return (
    <div>
      <FilePicker
        label={`Archivos a unir (${files.length} seleccionados)`}
        value={files}
        onChange={setFiles}
        accept="video"
        multiple
      />

      {files.length >= 2 && (
        <div style={{ marginTop: 16, padding: '10px 14px', background: 'var(--accent-dim)', border: '1px solid rgba(245,158,11,0.2)', borderRadius: 6, marginBottom: 16, fontSize: 12, color: 'var(--accent)' }}>
          ⚠️ Los archivos deben tener el mismo codec y resolución para unirse sin re-encodear.
        </div>
      )}

      <div style={{ marginTop: 20 }}>
        <button className="btn-primary" onClick={run} disabled={processing || files.length < 2}>
          {processing ? '⏳ Uniendo...' : `▶ Unir ${files.length} archivos`}
        </button>
      </div>
    </div>
  )
}

// ── Filters (Cirugía de Imagen) ────────────────────────────────────────────────
const SCALE_PRESETS = [
  { label: 'Sin cambio',    value: 'none' },
  { label: '1920×1080',     value: '1920:1080' },
  { label: '3840×2160',     value: '3840:2160' },
  { label: '1280×720',      value: '1280:720' },
  { label: 'Personalizado', value: 'custom' },
]

const ROTATIONS = [
  { label: '0°',    value: 'none', symbol: '↑' },
  { label: '90° ↻', value: 'cw',   symbol: '↻' },
  { label: '90° ↺', value: 'ccw',  symbol: '↺' },
  { label: '180°',  value: '180',  symbol: '↕' },
]

function FiltersPanel({ processing, setProcessing, showToast }) {
  const [input, setInput] = useState('')

  const [scalePreset, setScalePreset] = useState('none')
  const [customW, setCustomW]         = useState('')
  const [customH, setCustomH]         = useState('')

  const [cropW, setCropW]         = useState('')
  const [cropH, setCropH]         = useState('')
  const [cropX, setCropX]         = useState('')
  const [cropY, setCropY]         = useState('')
  const [detecting, setDetecting] = useState(false)

  const [rotation, setRotation] = useState('none')

  const detectCrop = async () => {
    if (!input) return showToast('error', 'Selecciona un archivo primero')
    setDetecting(true)
    try {
      const result = await window.api.cropDetect({ input })
      if (result) {
        setCropW(String(result.w))
        setCropH(String(result.h))
        setCropX(String(result.x))
        setCropY(String(result.y))
        showToast('success', `Bandas detectadas: ${result.w}×${result.h} @ (${result.x}, ${result.y})`)
      } else {
        showToast('error', 'No se detectaron bandas negras')
      }
    } catch (e) {
      showToast('error', `Error en detección: ${e.message || e}`)
    } finally {
      setDetecting(false)
    }
  }

  const run = async () => {
    if (!input) return showToast('error', 'Selecciona un archivo de entrada')

    let scale = null
    if (scalePreset === 'custom') {
      if (!customW && !customH) return showToast('error', 'Introduce al menos un valor de escala personalizado')
      scale = { w: customW || '-1', h: customH || '-1' }
    } else if (scalePreset !== 'none') {
      const [w, h] = scalePreset.split(':')
      scale = { w, h }
    }

    const crop = (cropW && cropH)
      ? { w: cropW, h: cropH, x: cropX || '0', y: cropY || '0' }
      : null

    if (!scale && !crop && rotation === 'none') {
      return showToast('error', 'Configura al menos un filtro antes de procesar')
    }

    const ext = input.split('.').pop()
    const result = await window.api.saveFile({
      defaultPath: input.replace(/(\.[^.]+)$/, '_filtered.$1'),
      filters: [{ name: 'Video', extensions: [ext, 'mp4', 'mov', 'mkv'] }]
    })
    if (result.canceled) return

    setProcessing(true)
    try {
      await window.api.applyFilters({ input, output: result.filePath, scale, crop, rotation })
    } catch (e) {
      setProcessing(false)
      showToast('error', `Error al aplicar filtros: ${e.message || e}`)
    }
  }

  return (
    <div>
      <FilePicker label="Archivo de entrada" value={input} onChange={setInput} accept="video" />

      <hr className="divider" />

      {/* Scale */}
      <div style={{ marginBottom: 20 }}>
        <label className="field-label" style={{ marginBottom: 10 }}>Escalador Inteligente</label>
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
          {SCALE_PRESETS.map(({ label, value }) => (
            <button
              key={value}
              onClick={() => setScalePreset(value)}
              style={{
                padding: '7px 14px', borderRadius: 6, cursor: 'pointer', fontSize: 12,
                fontFamily: value === 'none' || value === 'custom' ? 'inherit' : 'JetBrains Mono, monospace',
                border: `1px solid ${scalePreset === value ? 'var(--accent)' : 'var(--border)'}`,
                background: scalePreset === value ? 'var(--accent-dim)' : 'var(--bg-input)',
                color: scalePreset === value ? 'var(--accent)' : 'var(--text-2)',
                fontWeight: scalePreset === value ? 600 : 400,
                transition: 'all 0.12s'
              }}
            >
              {label}
            </button>
          ))}
        </div>
        {scalePreset === 'custom' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginTop: 12 }}>
            <div>
              <label className="field-label">Ancho (px)</label>
              <input type="number" min="1" placeholder="-1 = automático" value={customW} onChange={e => setCustomW(e.target.value)} />
            </div>
            <div>
              <label className="field-label">Alto (px)</label>
              <input type="number" min="1" placeholder="-1 = automático" value={customH} onChange={e => setCustomH(e.target.value)} />
            </div>
          </div>
        )}
      </div>

      <hr className="divider" />

      {/* Crop */}
      <div style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
          <label className="field-label" style={{ marginBottom: 0 }}>Crop Tool</label>
          <button
            onClick={detectCrop}
            disabled={detecting || !input}
            style={{
              padding: '5px 12px', borderRadius: 6,
              border: '1px solid var(--border)',
              background: 'var(--bg-input)',
              color: detecting ? 'var(--text-3)' : 'var(--text-2)',
              cursor: detecting || !input ? 'not-allowed' : 'pointer',
              fontSize: 11, display: 'flex', alignItems: 'center', gap: 5,
              transition: 'all 0.12s'
            }}
          >
            <span>{detecting ? '⏳' : '🔍'}</span>
            {detecting ? 'Analizando...' : 'Quitar bandas negras'}
          </button>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          {[['Ancho', cropW, setCropW], ['Alto', cropH, setCropH], ['X', cropX, setCropX], ['Y', cropY, setCropY]].map(([lbl, val, set]) => (
            <div key={lbl}>
              <label className="field-label">{lbl}</label>
              <input type="number" min="0" placeholder="—" value={val} onChange={e => set(e.target.value)} />
            </div>
          ))}
        </div>
        {cropW && cropH && (
          <div style={{ marginTop: 8, fontSize: 11, color: 'var(--text-3)', fontFamily: 'JetBrains Mono, monospace' }}>
            crop={cropW}:{cropH}:{cropX || '0'}:{cropY || '0'}
          </div>
        )}
      </div>

      <hr className="divider" />

      {/* Rotation */}
      <div style={{ marginBottom: 24 }}>
        <label className="field-label" style={{ marginBottom: 10 }}>Rotación rápida</label>
        <div style={{ display: 'flex', gap: 8 }}>
          {ROTATIONS.map(({ label, value, symbol }) => {
            const active = rotation === value
            return (
              <button
                key={value}
                onClick={() => setRotation(value)}
                style={{
                  display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4,
                  padding: '12px 20px', borderRadius: 8, cursor: 'pointer',
                  border: `1px solid ${active ? 'var(--accent)' : 'var(--border)'}`,
                  background: active ? 'var(--accent-dim)' : 'var(--bg-input)',
                  transition: 'all 0.12s'
                }}
              >
                <span style={{ fontSize: 20, lineHeight: 1 }}>{symbol}</span>
                <span style={{
                  fontSize: 11, fontFamily: 'JetBrains Mono, monospace',
                  color: active ? 'var(--accent)' : 'var(--text-2)',
                  fontWeight: active ? 600 : 400
                }}>{label}</span>
              </button>
            )
          })}
        </div>
      </div>

      <button className="btn-primary" onClick={run} disabled={processing || !input}>
        {processing ? '⏳ Procesando...' : '▶ Aplicar filtros'}
      </button>
    </div>
  )
}

// ── Helpers ────────────────────────────────────────────────────────────────────
function validateTime(str) {
  return /^\d{1,2}:\d{2}:\d{2}$/.test(str) || /^\d{1,2}:\d{2}$/.test(str)
}

function timeToSec(str) {
  if (!str) return 0
  const p = str.split(':').map(Number)
  if (p.length === 3) return p[0] * 3600 + p[1] * 60 + p[2]
  if (p.length === 2) return p[0] * 60 + p[1]
  return p[0] || 0
}

function formatDuration(sec) {
  const h = Math.floor(sec / 3600)
  const m = Math.floor((sec % 3600) / 60)
  const s = sec % 60
  return [h, m, s].map(v => String(v).padStart(2, '0')).join(':')
}
