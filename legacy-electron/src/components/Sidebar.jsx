import React from 'react'
import logo from '../assets/logo.png'

const TABS = [
  {
    id: 'convert',
    label: 'Convertir Video',
    desc: 'Codecs & Batch',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/>
        <path d="M14 17h6M17 14v6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      </svg>
    )
  },
  {
    id: 'image',
    label: 'Convertir Imágenes',
    desc: 'JPG, PNG, ICO',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" strokeWidth="1.5"/>
        <circle cx="8.5" cy="8.5" r="1.5" fill="currentColor"/>
        <path d="M21 15l-5-5L5 21" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    )
  },
  {
    id: 'probe',
    label: 'Analizador',
    desc: 'Forense / ffprobe',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M20 20l-3-3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
        <path d="M11 8v3l2 2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    )
  },
  {
    id: 'compress',
    label: 'Comprimir',
    desc: 'Reducir tamaño',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <path d="M12 2v10m0 0l-3-3m3 3l3-3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
        <path d="M12 22v-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
        <path d="M4 16H2a1 1 0 000 2h20a1 1 0 000-2h-2" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M20 12H4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeDasharray="2 2"/>
      </svg>
    )
  },
  {
    id: 'trim',
    label: 'Edición',
    desc: 'Cortar, unir, filtros',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <path d="M6 3v18M18 3v18" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
        <path d="M3 8h18M3 16h18" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
        <rect x="6" y="8" width="12" height="8" fill="currentColor" fillOpacity="0.1" stroke="currentColor" strokeWidth="1"/>
      </svg>
    )
  },
  {
    id: 'audio',
    label: 'Convertir Audio',
    desc: 'MP3, AAC, FLAC...',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <path d="M9 18V5l12-2v13" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
        <circle cx="6" cy="18" r="3" stroke="currentColor" strokeWidth="1.5"/>
        <circle cx="18" cy="16" r="3" stroke="currentColor" strokeWidth="1.5"/>
      </svg>
    )
  },
  {
    id: 'extract',
    label: 'Extraer Audio',
    desc: 'Audio / Frames',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.5"/>
        <circle cx="12" cy="12" r="3" fill="currentColor" fillOpacity="0.3" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M12 3v2M12 19v2M3 12h2M19 12h2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      </svg>
    )
  },
  {
    id: 'about',
    label: 'Acerca de',
    desc: 'Licencia & Legal',
    icon: (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.5"/>
        <path d="M12 11v5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
        <circle cx="12" cy="8" r="0.75" fill="currentColor" stroke="currentColor" strokeWidth="0.5"/>
      </svg>
    )
  }
]

export default function Sidebar({ active, onChange }) {
  return (
    <nav style={{
      width: 210,
      background: 'var(--bg-panel)',
      borderRight: '1px solid var(--border)',
      display: 'flex',
      flexDirection: 'column',
      padding: '8px 8px',
      gap: 2,
      flexShrink: 0
    }}>
      {/* BRAND HEADER */}
      <div style={{ padding: '12px 12px', marginBottom: 8, display: 'flex', alignItems: 'center', gap: 10 }}>
        <img src={logo} alt="Logo" style={{ width: 36, height: 36, objectFit: 'contain', borderRadius: 6, flexShrink: 0 }} />
        <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--text-1)', letterSpacing: -0.3, lineHeight: 1.2 }}>
          Format Media<br />
          <span style={{ color: 'var(--accent)' }}>Coder</span>
        </div>
      </div>

      {TABS.map(tab => {
        const isActive = active === tab.id
        return (
          <button
            key={tab.id}
            onClick={() => onChange(tab.id)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '10px 12px',
              borderRadius: 7,
              border: 'none',
              background: isActive ? 'var(--accent-dim)' : 'transparent',
              cursor: 'pointer',
              textAlign: 'left',
              transition: 'background 0.12s',
              width: '100%',
            }}
            onMouseEnter={e => { if (!isActive) e.currentTarget.style.background = 'var(--bg-card)' }}
            onMouseLeave={e => { if (!isActive) e.currentTarget.style.background = 'transparent' }}
          >
            <span style={{ color: isActive ? 'var(--accent)' : 'var(--text-3)', flexShrink: 0, transition: 'color 0.12s' }}>
              {tab.icon}
            </span>
            <div>
              <div style={{
                fontSize: 13,
                fontWeight: isActive ? 600 : 400,
                color: isActive ? 'var(--text-1)' : 'var(--text-2)',
                transition: 'color 0.12s'
              }}>{tab.label}</div>
              <div style={{ fontSize: 11, color: 'var(--text-3)', marginTop: 1 }}>{tab.desc}</div>
            </div>
            {isActive && (
              <div style={{
                marginLeft: 'auto',
                width: 3,
                height: 20,
                borderRadius: 2,
                background: 'var(--accent)',
                flexShrink: 0
              }} />
            )}
          </button>
        )
      })}

      <div style={{ marginTop: 'auto', padding: '12px 12px 4px', borderTop: '1px solid var(--border)' }}>
        <div style={{ fontSize: 11, color: 'var(--text-3)', fontFamily: 'JetBrains Mono, monospace' }}>
          v1.0.0-PRO
        </div>
        <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 2 }}>Format Media Coder Engine</div>
      </div>
    </nav>
  )
}
