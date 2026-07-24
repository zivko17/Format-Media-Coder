import { useEffect, useState } from 'react'

export default function Toast({ toast, onClose }) {
  const [visible, setVisible] = useState(true)

  useEffect(() => {
    const t = setTimeout(() => { setVisible(false); setTimeout(onClose, 300) }, 5000)
    return () => clearTimeout(t)
  }, [])

  const isSuccess = toast.type === 'success'
  const color = isSuccess ? 'var(--success)' : 'var(--error)'
  const bg = isSuccess ? 'rgba(63,185,80,0.1)' : 'rgba(248,81,73,0.1)'
  const border = isSuccess ? 'rgba(63,185,80,0.25)' : 'rgba(248,81,73,0.25)'

  return (
    <div style={{
      position: 'fixed', bottom: 24, right: 24, zIndex: 200,
      maxWidth: 380,
      background: 'var(--bg-card)',
      border: `1px solid ${border}`,
      borderLeft: `3px solid ${color}`,
      borderRadius: 8,
      padding: '14px 16px',
      boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
      opacity: visible ? 1 : 0,
      transform: visible ? 'translateY(0)' : 'translateY(12px)',
      transition: 'all 0.25s ease',
      display: 'flex',
      alignItems: 'flex-start',
      gap: 12,
    }}>
      <span style={{ fontSize: 16 }}>{isSuccess ? '✅' : '❌'}</span>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-1)', marginBottom: 2 }}>
          {isSuccess ? 'Completado' : 'Error'}
        </div>
        <div style={{ fontSize: 12, color: 'var(--text-2)', wordBreak: 'break-all' }}>
          {toast.message}
        </div>
        {isSuccess && toast.output && (
          <button
            onClick={() => window.api.showInFolder(toast.output)}
            style={{
              marginTop: 8, fontSize: 11, color: color,
              background: bg, border: `1px solid ${border}`,
              borderRadius: 4, padding: '3px 10px',
              cursor: 'pointer',
            }}
          >
            📂 Abrir carpeta
          </button>
        )}
      </div>
      <button onClick={() => { setVisible(false); setTimeout(onClose, 300) }}
        style={{ background: 'none', border: 'none', color: 'var(--text-3)', cursor: 'pointer', fontSize: 16, lineHeight: 1, padding: '0 2px' }}>×</button>
    </div>
  )
}
