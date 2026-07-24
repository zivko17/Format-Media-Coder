import logo from '../assets/logo.png'

export default function TitleBar() {
  const isMac = navigator.platform.toLowerCase().includes('mac')

  return (
    <div style={{
      height: 38,
      background: 'var(--bg-panel)',
      borderBottom: '1px solid var(--border)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      flexShrink: 0,
      WebkitAppRegion: 'drag',
      paddingLeft: isMac ? 80 : 16,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <img src={logo} alt="logo" style={{ width: 20, height: 20, objectFit: 'contain', borderRadius: 4 }} />
        <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-2)', letterSpacing: '0.08em', textTransform: 'uppercase' }}>
          Format Media Coder
        </span>
      </div>

      {/* Windows controls */}
      {!isMac && (
        <div style={{ display: 'flex', WebkitAppRegion: 'no-drag' }}>
          <WinBtn onClick={() => window.api.minimize()} title="Minimizar">
            <svg width="10" height="1" viewBox="0 0 10 1"><line x1="0" y1="0.5" x2="10" y2="0.5" stroke="currentColor" strokeWidth="1.5"/></svg>
          </WinBtn>
          <WinBtn onClick={() => window.api.maximize()} title="Maximizar">
            <svg width="10" height="10" viewBox="0 0 10 10"><rect x="0.75" y="0.75" width="8.5" height="8.5" rx="1" stroke="currentColor" strokeWidth="1.5" fill="none"/></svg>
          </WinBtn>
          <WinBtn onClick={() => window.api.close()} title="Cerrar" danger>
            <svg width="10" height="10" viewBox="0 0 10 10">
              <line x1="1" y1="1" x2="9" y2="9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
              <line x1="9" y1="1" x2="1" y2="9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
            </svg>
          </WinBtn>
        </div>
      )}
    </div>
  )
}

function WinBtn({ children, onClick, danger }) {
  return (
    <button onClick={onClick} style={{
      width: 46,
      height: 38,
      border: 'none',
      background: 'transparent',
      color: 'var(--text-3)',
      cursor: 'pointer',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      transition: 'background 0.1s, color 0.1s',
    }}
    onMouseEnter={e => {
      e.currentTarget.style.background = danger ? '#c42b1c' : 'var(--bg-card)'
      e.currentTarget.style.color = danger ? '#fff' : 'var(--text-1)'
    }}
    onMouseLeave={e => {
      e.currentTarget.style.background = 'transparent'
      e.currentTarget.style.color = 'var(--text-3)'
    }}>
      {children}
    </button>
  )
}
