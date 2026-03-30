export default function ProgressOverlay({ processing, progress, currentFile, queueInfo, onCancel }) {
  const pct = Math.min(Math.round(progress.percent || 0), 100)

  return (
    <div style={{
      flexShrink: 0,
      height: processing ? 52 : 0,
      overflow: 'hidden',
      transition: 'height 0.2s ease',
      background: 'var(--bg-panel)',
      borderTop: processing ? '1px solid var(--border)' : 'none',
      position: 'relative',
    }}>
      {/* Accent progress line at top */}
      <div style={{
        position: 'absolute', top: 0, left: 0,
        height: 2, width: `${pct}%`,
        background: 'linear-gradient(90deg, var(--accent) 0%, #fde68a 100%)',
        transition: 'width 0.4s ease',
        zIndex: 1
      }} />

      <div style={{ display: 'flex', alignItems: 'center', gap: 14, padding: '0 24px', height: 52 }}>
        {/* Pulse dot */}
        <div style={{
          width: 8, height: 8, borderRadius: '50%',
          background: 'var(--accent)', flexShrink: 0,
          animation: 'pulse 1.2s ease-in-out infinite'
        }} />

        {/* File info */}
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontSize: 12, color: 'var(--text-1)', fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {currentFile ? currentFile.split(/[\\/]/).pop() : 'Procesando...'}
          </div>
          {queueInfo && (
            <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 1 }}>{queueInfo}</div>
          )}
        </div>

        {progress.timemark && (
          <div style={{ fontSize: 11, color: 'var(--text-2)', fontFamily: 'JetBrains Mono, monospace' }}>
            ⏱ {progress.timemark}
          </div>
        )}
        {progress.fps > 0 && (
          <div style={{ fontSize: 11, color: 'var(--text-3)', fontFamily: 'JetBrains Mono, monospace' }}>
            {Math.round(progress.fps)} fps
          </div>
        )}
        <div style={{ fontSize: 15, fontWeight: 700, fontFamily: 'JetBrains Mono, monospace', color: 'var(--accent)', minWidth: 44, textAlign: 'right' }}>
          {pct}%
        </div>
        <button onClick={onCancel} style={{
          padding: '4px 12px', borderRadius: 5, fontSize: 11,
          border: '1px solid var(--border)', background: 'transparent',
          color: 'var(--text-3)', cursor: 'pointer', flexShrink: 0, transition: 'all 0.12s'
        }}>
          ✕ Cancelar
        </button>
      </div>
    </div>
  )
}
