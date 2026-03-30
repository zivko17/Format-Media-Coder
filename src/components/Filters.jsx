import { useState } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

const SCALE_PRESETS = [
  { label: 'Sin cambio', value: 'none' },
  { label: '1920×1080', value: '1920:1080' },
  { label: '3840×2160', value: '3840:2160' },
  { label: '1280×720',  value: '1280:720' },
  { label: 'Personalizado', value: 'custom' },
]

const ROTATIONS = [
  { label: '0°',    value: 'none', symbol: '↑' },
  { label: '90° ↻', value: 'cw',   symbol: '↻' },
  { label: '90° ↺', value: 'ccw',  symbol: '↺' },
  { label: '180°',  value: '180',  symbol: '↕' },
]

export default function Filters({ processing, setProcessing, showToast }) {
  const [input, setInput]     = useState('')

  // Scale
  const [scalePreset, setScalePreset] = useState('none')
  const [customW, setCustomW]         = useState('')
  const [customH, setCustomH]         = useState('')

  // Crop
  const [cropW, setCropW] = useState('')
  const [cropH, setCropH] = useState('')
  const [cropX, setCropX] = useState('')
  const [cropY, setCropY] = useState('')
  const [detecting, setDetecting] = useState(false)

  // Rotation
  const [rotation, setRotation] = useState('none')

  // ── Detect black bars ──────────────────────────────────────────────────────
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

  // ── Build & Run ────────────────────────────────────────────────────────────
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

  const hasCrop = cropW && cropH

  return (
    <div className="animate-in">
      <PageHeader
        title="Cirugía de Imagen"
        desc="Escala, recorta y rota sin tocar un solo parámetro raro."
        icon="✂️"
      />

      <FilePicker label="Archivo de entrada" value={input} onChange={setInput} accept="video" />

      <hr className="divider" />

      {/* ── SCALE ──────────────────────────────────────────────────────────── */}
      <div style={{ marginBottom: 20 }}>
        <label className="field-label" style={{ marginBottom: 10 }}>Escalador Inteligente</label>
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
          {SCALE_PRESETS.map(({ label, value }) => (
            <button
              key={value}
              onClick={() => setScalePreset(value)}
              style={{
                padding: '7px 14px',
                borderRadius: 6,
                cursor: 'pointer',
                fontSize: 12,
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
              <input
                type="number" min="1" placeholder="-1 = automático"
                value={customW} onChange={e => setCustomW(e.target.value)}
              />
            </div>
            <div>
              <label className="field-label">Alto (px)</label>
              <input
                type="number" min="1" placeholder="-1 = automático"
                value={customH} onChange={e => setCustomH(e.target.value)}
              />
            </div>
          </div>
        )}
      </div>

      <hr className="divider" />

      {/* ── CROP ───────────────────────────────────────────────────────────── */}
      <div style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
          <label className="field-label" style={{ marginBottom: 0 }}>Crop Tool</label>
          <button
            onClick={detectCrop}
            disabled={detecting || !input}
            style={{
              padding: '5px 12px',
              borderRadius: 6,
              border: '1px solid var(--border)',
              background: 'var(--bg-input)',
              color: detecting ? 'var(--text-3)' : 'var(--text-2)',
              cursor: detecting || !input ? 'not-allowed' : 'pointer',
              fontSize: 11,
              display: 'flex',
              alignItems: 'center',
              gap: 5,
              transition: 'all 0.12s'
            }}
          >
            <span>{detecting ? '⏳' : '🔍'}</span>
            {detecting ? 'Analizando...' : 'Quitar bandas negras'}
          </button>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
          {[
            ['Ancho', cropW, setCropW, 'w'],
            ['Alto',  cropH, setCropH, 'h'],
            ['X',     cropX, setCropX, 'x'],
            ['Y',     cropY, setCropY, 'y'],
          ].map(([lbl, val, set, key]) => (
            <div key={key}>
              <label className="field-label">{lbl}</label>
              <input
                type="number" min="0" placeholder="—"
                value={val} onChange={e => set(e.target.value)}
              />
            </div>
          ))}
        </div>

        {hasCrop && (
          <div style={{ marginTop: 8, fontSize: 11, color: 'var(--text-3)', fontFamily: 'JetBrains Mono, monospace' }}>
            crop={cropW}:{cropH}:{cropX || '0'}:{cropY || '0'}
          </div>
        )}
      </div>

      <hr className="divider" />

      {/* ── ROTATION ───────────────────────────────────────────────────────── */}
      <div style={{ marginBottom: 28 }}>
        <label className="field-label" style={{ marginBottom: 10 }}>Rotación rápida</label>
        <div style={{ display: 'flex', gap: 8 }}>
          {ROTATIONS.map(({ label, value, symbol }) => {
            const active = rotation === value
            return (
              <button
                key={value}
                onClick={() => setRotation(value)}
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  gap: 4,
                  padding: '12px 20px',
                  borderRadius: 8,
                  cursor: 'pointer',
                  border: `1px solid ${active ? 'var(--accent)' : 'var(--border)'}`,
                  background: active ? 'var(--accent-dim)' : 'var(--bg-input)',
                  transition: 'all 0.12s'
                }}
              >
                <span style={{ fontSize: 20, lineHeight: 1 }}>{symbol}</span>
                <span style={{
                  fontSize: 11,
                  fontFamily: 'JetBrains Mono, monospace',
                  color: active ? 'var(--accent)' : 'var(--text-2)',
                  fontWeight: active ? 600 : 400
                }}>
                  {label}
                </span>
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
