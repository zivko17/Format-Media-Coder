import { useState } from 'react'

export default function FilePicker({ label, value, onChange, accept, multiple = false, placeholder = 'Seleccionar archivo...' }) {
  const [hover, setHover] = useState(false)
  const [dragging, setDragging] = useState(false)

  const handleClick = async () => {
    const filters = buildFilters(accept)
    const result = await window.api.openFile({
      properties: multiple ? ['openFile', 'multiSelections'] : ['openFile'],
      filters
    })
    if (!result.canceled && result.filePaths.length > 0) {
      onChange(multiple ? result.filePaths : result.filePaths[0])
    }
  }

  const handleDragOver = (e) => {
    e.preventDefault()
    e.stopPropagation()
    setDragging(true)
  }

  const handleDragLeave = (e) => {
    e.preventDefault()
    setDragging(false)
  }

  const handleDrop = (e) => {
    e.preventDefault()
    e.stopPropagation()
    setDragging(false)
    const dropped = Array.from(e.dataTransfer.files).map(f => f.path).filter(Boolean)
    if (!dropped.length) return
    if (multiple) {
      const existing = Array.isArray(value) ? value : []
      const merged = [...existing, ...dropped.filter(p => !existing.includes(p))]
      onChange(merged)
    } else {
      onChange(dropped[0])
    }
  }

  const displayValue = multiple
    ? (Array.isArray(value) && value.length > 0 ? `${value.length} archivos seleccionados` : null)
    : value

  const fileName = displayValue ? displayValue.split(/[\\/]/).pop() : null
  const isActive = hover || dragging

  return (
    <div>
      {label && <label className="field-label">{label}</label>}
      <button
        type="button"
        onClick={handleClick}
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        onDragOver={handleDragOver}
        onDragEnter={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        style={{
          width: '100%',
          padding: '10px 14px',
          border: `1.5px dashed ${dragging ? 'var(--accent)' : isActive ? 'var(--accent)' : 'var(--border-mid)'}`,
          borderRadius: 7,
          background: dragging ? 'var(--accent-dim)' : isActive ? 'var(--accent-dim)' : 'var(--bg-input)',
          cursor: 'pointer',
          textAlign: 'left',
          display: 'flex',
          alignItems: 'center',
          gap: 10,
          transition: 'all 0.15s',
          color: fileName ? 'var(--text-1)' : 'var(--text-3)'
        }}
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0, color: fileName ? 'var(--accent)' : dragging ? 'var(--accent)' : 'var(--text-3)' }}>
          <path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9l-7-7z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/>
          <path d="M13 2v7h7" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/>
        </svg>
        <span style={{ fontSize: 13, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1 }}>
          {dragging
            ? (multiple ? 'Suelta los archivos aquí...' : 'Suelta el archivo aquí...')
            : (fileName || placeholder)}
        </span>
        {fileName && !dragging && (
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" style={{ color: 'var(--success)', flexShrink: 0 }}>
            <path d="M20 6L9 17l-5-5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        )}
        {dragging && (
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" style={{ color: 'var(--accent)', flexShrink: 0 }}>
            <path d="M12 4v16M4 12l8-8 8 8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        )}
      </button>
      {multiple && Array.isArray(value) && value.length > 0 && (
        <div style={{ marginTop: 6, display: 'flex', flexDirection: 'column', gap: 3 }}>
          {value.map((f, i) => (
            <div key={i} style={{ fontSize: 11, color: 'var(--text-2)', fontFamily: 'JetBrains Mono, monospace', padding: '3px 8px', background: 'var(--bg-input)', borderRadius: 4, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{f.split(/[\\/]/).pop()}</span>
              <button onClick={(e) => { e.stopPropagation(); onChange(value.filter((_, j) => j !== i)) }}
                style={{ background: 'none', border: 'none', color: 'var(--text-3)', cursor: 'pointer', fontSize: 14, lineHeight: 1, marginLeft: 8 }}>×</button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function buildFilters(accept) {
  if (!accept) return [{ name: 'All Files', extensions: ['*'] }]
  const map = {
    video: { name: 'Video', extensions: ['mp4', 'mkv', 'avi', 'mov', 'webm', 'flv', 'wmv', 'm4v', 'ts'] },
    audio: { name: 'Audio', extensions: ['mp3', 'aac', 'wav', 'flac', 'ogg', 'm4a', 'wma'] },
    media: { name: 'Media', extensions: ['mp4', 'mkv', 'avi', 'mov', 'webm', 'mp3', 'aac', 'wav', 'flac'] }
  }
  return [map[accept] || { name: 'All Files', extensions: ['*'] }, { name: 'All Files', extensions: ['*'] }]
}
