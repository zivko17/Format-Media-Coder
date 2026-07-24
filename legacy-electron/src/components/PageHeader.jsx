import React from 'react'
import logoImg from '../assets/logo.png'

export default function PageHeader({ title, desc }) {
  return (
    <div style={{ 
      marginBottom: 32, 
      display: 'flex', 
      alignItems: 'center', 
      gap: 16,
      borderBottom: '1.5px solid var(--border)',
      paddingBottom: 20
    }}>
      {/* Contenedor del Logo de la Nano Banana */}
      <div style={{ 
        width: 52, 
        height: 52, 
        borderRadius: 12, 
        background: 'var(--bg-input)',
        border: '1px solid var(--border)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
        flexShrink: 0,
        boxShadow: '0 4px 10px rgba(0,0,0,0.2)'
      }}>
        <img 
          src={logoImg} 
          alt="Logo" 
          style={{ 
            width: '85%', 
            height: '85%', 
            objectFit: 'contain' 
          }} 
        />
      </div>

      <div>
        <h1 style={{ 
          fontSize: 24, 
          fontWeight: 800, 
          color: 'var(--text-1)', 
          margin: 0,
          letterSpacing: '-0.8px',
          textTransform: 'uppercase'
        }}>
          {title}
        </h1>
        <p style={{ 
          fontSize: 13, 
          color: 'var(--text-3)', 
          margin: '2px 0 0 0',
          fontWeight: 500
        }}>
          {desc}
        </p>
      </div>
    </div>
  )
}