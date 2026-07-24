import logo from '../assets/logo.png'

const SECTION = ({ title, children }) => (
  <div style={{ marginBottom: 24 }}>
    <div style={{
      fontSize: 10,
      fontWeight: 700,
      color: 'var(--accent)',
      letterSpacing: '0.12em',
      textTransform: 'uppercase',
      marginBottom: 10,
      fontFamily: 'JetBrains Mono, monospace'
    }}>
      {title}
    </div>
    <div style={{
      background: 'var(--bg-input)',
      border: '1px solid var(--border)',
      borderRadius: 8,
      padding: '14px 16px',
      fontSize: 13,
      color: 'var(--text-2)',
      lineHeight: 1.7
    }}>
      {children}
    </div>
  </div>
)

export default function About() {
  return (
    <div className="animate-in" style={{ maxWidth: 680 }}>

      {/* Header */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 20,
        marginBottom: 32,
        paddingBottom: 24,
        borderBottom: '1px solid var(--border)'
      }}>
        <img src={logo} alt="Logo" style={{ width: 64, height: 64, borderRadius: 14, objectFit: 'contain' }} />
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 800, color: 'var(--text-1)', margin: 0, letterSpacing: '-0.5px' }}>
            Format Media Coder
          </h1>
          <div style={{ fontSize: 12, color: 'var(--text-3)', marginTop: 4, fontFamily: 'JetBrains Mono, monospace' }}>
            v1.0.0
          </div>
          <div style={{ fontSize: 13, color: 'var(--text-2)', marginTop: 6 }}>
            © 2025 Andrés Zivkovic González
          </div>
        </div>
      </div>

      <div style={{ fontSize: 13, color: 'var(--text-2)', lineHeight: 1.7, marginBottom: 28 }}>
        Proyecto personal desarrollado de forma independiente. Format Media Coder es una
        interfaz gráfica (GUI wrapper) construida sobre FFmpeg para simplificar tareas de
        conversión y procesado de medios.
      </div>

      <SECTION title="Licencia">
        Este programa es software libre: puedes redistribuirlo y/o modificarlo bajo los
        términos de la <span style={{ color: 'var(--text-1)', fontWeight: 600 }}>Licencia Pública General GNU (GPL v3)</span> publicada
        por la Free Software Foundation.
        <br /><br />
        <span style={{ color: 'var(--text-3)', fontSize: 12, fontFamily: 'JetBrains Mono, monospace' }}>
          https://www.gnu.org/licenses/gpl-3.0.html
        </span>
      </SECTION>

      <SECTION title="FFmpeg">
        Este programa incluye <span style={{ color: 'var(--text-1)', fontWeight: 600 }}>FFmpeg</span>, software multimedia de código abierto
        desarrollado por sus respectivos autores y distribuido bajo la GNU General Public
        License v2 o posterior. FFmpeg no es propiedad de Andrés Zivkovic González.
        <br /><br />
        <span style={{ color: 'var(--text-3)', fontSize: 12, fontFamily: 'JetBrains Mono, monospace' }}>
          https://ffmpeg.org &nbsp;·&nbsp; Código fuente: https://ffmpeg.org/download.html
        </span>
      </SECTION>

      <SECTION title="Aviso legal">
        Este software se distribuye <span style={{ color: 'var(--text-1)', fontWeight: 600 }}>"tal cual"</span>, sin garantías de ningún tipo,
        expresas ni implícitas. El autor no se responsabiliza de pérdidas de datos,
        interrupciones del sistema ni daños de ninguna índole derivados del uso de esta
        herramienta. Úsala bajo tu propia responsabilidad.
      </SECTION>

      <SECTION title="Privacidad">
        Format Media Coder <span style={{ color: 'var(--text-1)', fontWeight: 600 }}>no recopila, almacena ni transmite</span> ningún dato
        personal ni de uso del sistema.
      </SECTION>

    </div>
  )
}
