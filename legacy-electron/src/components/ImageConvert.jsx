import { useState } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

const IMG_FORMATS = ['png', 'jpg', 'webp', 'ico', 'bmp']

export default function ImageConvert({ processing, setProcessing, showToast }) {
  const [inputs, setInputs] = useState([])
  const [format, setFormat] = useState('png')

  const run = async () => {
    if (inputs.length === 0) return showToast('error', 'Selecciona imágenes primero')
    
    setProcessing(true)
    try {
      for (const input of inputs) {
        // Genera el nombre de salida automáticamente
        const output = input.replace(/\.[^/.]+$/, '') + `_converted.${format}`
        
        await window.api.invoke('ffmpeg:convert-image', { input, output, format })
      }
      showToast('success', `¡Completado! ${inputs.length} imágenes procesadas.`)
      setInputs([]) // Limpiamos la cola
    } catch (e) {
      showToast('error', `Error: ${e}`)
    } finally {
      setProcessing(false)
    }
  }

  return (
    <div className="animate-in">
      <PageHeader 
        title="Convertir Imágenes"
        desc="Cambia formatos de imagen y crea iconos (.ico) rápidamente." 
        icon="🖼️" 
      />

      <div style={{ display: 'grid', gap: 20, marginBottom: 20 }}>
        <FilePicker 
          label="Archivos de imagen" 
          value={inputs} 
          onChange={setInputs} 
          multiple={true} 
          accept="image/*" 
          placeholder="Suelta tus fotos aquí..."
        />

        <div>
          <label className="field-label">Formato de destino</label>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 8 }}>
            {IMG_FORMATS.map(f => (
              <button
                key={f}
                onClick={() => setFormat(f)}
                style={{
                  padding: '12px 6px',
                  borderRadius: 6,
                  border: `1px solid ${format === f ? 'var(--accent)' : 'var(--border)'}`,
                  background: format === f ? 'var(--accent-dim)' : 'var(--bg-input)',
                  color: format === f ? 'var(--accent)' : 'var(--text-2)',
                  cursor: 'pointer',
                  fontSize: 12,
                  fontWeight: 'bold',
                  textTransform: 'uppercase',
                  transition: 'all 0.1s'
                }}
              >
                .{f}
              </button>
            ))}
          </div>
        </div>
      </div>

      <button className="btn-primary" onClick={run} disabled={processing || inputs.length === 0}>
        {processing ? '⏳ Procesando lote...' : `▶ Convertir ${inputs.length > 0 ? inputs.length : ''} imágenes`}
      </button>
    </div>
  )
}