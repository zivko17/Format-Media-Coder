import { useState, useEffect } from 'react'
import FilePicker from './FilePicker'
import PageHeader from './PageHeader'

// ── Helpers ────────────────────────────────────────────────────────────────────
const parseFps = (str) => {
  if (!str || str === '0/0') return '—'
  const [n, d] = str.split('/').map(Number)
  const val = d ? n / d : n
  return val % 1 === 0 ? String(val) : val.toFixed(3).replace(/\.?0+$/, '')
}

const formatDuration = (secs) => {
  if (!secs) return '—'
  const s = parseFloat(secs)
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = Math.floor(s % 60)
  return [h, m, sec].map(v => String(v).padStart(2, '0')).join(':')
}

const formatSize = (bytes) => {
  if (!bytes) return '—'
  const b = +bytes
  if (b > 1e9) return (b / 1e9).toFixed(2) + ' GB'
  if (b > 1e6) return (b / 1e6).toFixed(1) + ' MB'
  return (b / 1e3).toFixed(0) + ' KB'
}

const formatBitrate = (bps) => {
  if (!bps) return '—'
  const b = +bps
  if (b > 1e6) return (b / 1e6).toFixed(1) + ' Mb/s'
  return (b / 1e3).toFixed(0) + ' kb/s'
}

const channelLabel = (n) => {
  const map = { 1: 'Mono', 2: 'Stereo', 6: '5.1', 8: '7.1' }
  return map[n] ? `${map[n]} (${n}ch)` : `${n}ch`
}

const toTimecode = (frames, fps) => {
  if (!frames || !fps) return '—'
  const f = Math.round(frames)
  const fpsR = Math.round(fps)
  const ff = f % fpsR
  const totalSec = Math.floor(f / fpsR)
  const ss = totalSec % 60
  const mm = Math.floor(totalSec / 60) % 60
  const hh = Math.floor(totalSec / 3600)
  return [hh, mm, ss].map(v => String(v).padStart(2, '0')).join(':') + ':' + String(ff).padStart(2, '0')
}

// ── StatCard ───────────────────────────────────────────────────────────────────
function StatCard({ label, value, sub, accent = false, wide = false }) {
  return (
    <div style={{
      background: 'var(--bg-card)',
      border: '1px solid var(--border)',
      borderRadius: 8,
      padding: '12px 14px',
      gridColumn: wide ? 'span 2' : undefined
    }}>
      <div style={{ fontSize: 10, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 5 }}>
        {label}
      </div>
      <div style={{
        fontSize: 15,
        fontWeight: 600,
        fontFamily: 'JetBrains Mono, monospace',
        color: accent ? 'var(--accent)' : 'var(--text-1)',
        wordBreak: 'break-all',
        lineHeight: 1.2
      }}>
        {value || '—'}
      </div>
      {sub && (
        <div style={{ fontSize: 10, color: 'var(--text-3)', marginTop: 4, fontFamily: 'JetBrains Mono, monospace' }}>
          {sub}
        </div>
      )}
    </div>
  )
}

// ── Section label ──────────────────────────────────────────────────────────────
function SectionLabel({ children }) {
  return (
    <div style={{
      fontSize: 10,
      fontWeight: 600,
      color: 'var(--text-3)',
      textTransform: 'uppercase',
      letterSpacing: '0.08em',
      marginBottom: 8,
      marginTop: 18,
      display: 'flex',
      alignItems: 'center',
      gap: 8
    }}>
      <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
      {children}
      <div style={{ flex: 1, height: 1, background: 'var(--border)' }} />
    </div>
  )
}

// ── Main component ─────────────────────────────────────────────────────────────
export default function Probe({ showToast }) {
  const [input, setInput]         = useState('')
  const [meta, setMeta]           = useState(null)
  const [loading, setLoading]     = useState(false)
  const [view, setView]           = useState('summary')
  const [exactFrames, setExactFrames] = useState(null)
  const [counting, setCounting]   = useState(false)
  const [copied, setCopied]       = useState(false)

  // Auto-probe on file change
  useEffect(() => {
    if (!input) { setMeta(null); setExactFrames(null); return }
    setLoading(true)
    setMeta(null)
    setExactFrames(null)
    window.api.probe(input)
      .then(m => { setMeta(m); setLoading(false) })
      .catch(e => { setLoading(false); showToast('error', `ffprobe: ${e}`) })
  }, [input])

  const countExact = async () => {
    setCounting(true)
    try {
      const n = await window.api.countFrames({ input })
      setExactFrames(n)
    } catch (e) {
      showToast('error', `Error contando frames: ${e}`)
    } finally {
      setCounting(false)
    }
  }

  const copyJson = () => {
    navigator.clipboard.writeText(JSON.stringify(meta, null, 2))
    setCopied(true)
    setTimeout(() => setCopied(false), 1800)
  }

  // Parsed values
  const vStream = meta?.streams?.find(s => s.codec_type === 'video')
  const aStream = meta?.streams?.find(s => s.codec_type === 'audio')
  const fmt     = meta?.format

  const fps      = vStream ? parseFps(vStream.r_frame_rate) : null
  const fpsNum   = fps ? parseFloat(fps) : null
  const duration = fmt?.duration ? parseFloat(fmt.duration) : null
  const estFrames = duration && fpsNum ? Math.round(duration * fpsNum) : null
  const frames   = exactFrames ?? (vStream?.nb_frames ? +vStream.nb_frames : estFrames)

  return (
    <div className="animate-in">
      <PageHeader
        title="Analizador Forense"
        desc="Suelta un archivo y te diré la verdad sobre él."
        icon="🔬"
      />

      <FilePicker label="Archivo a analizar" value={input} onChange={setInput} accept="media" />

      {loading && (
        <div style={{
          marginTop: 32, textAlign: 'center', color: 'var(--text-3)', fontSize: 13,
          display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10
        }}>
          <div style={{
            width: 28, height: 28, border: '2px solid var(--border)',
            borderTopColor: 'var(--accent)', borderRadius: '50%',
            animation: 'spin 0.7s linear infinite'
          }} />
          Analizando con ffprobe…
        </div>
      )}

      {meta && !loading && (
        <>
          {/* ── View tabs ─────────────────────────────────────────────────── */}
          <div style={{ display: 'flex', gap: 4, marginTop: 20, marginBottom: 0 }}>
            {[['summary', 'Resumen'], ['json', 'JSON completo']].map(([id, label]) => (
              <button
                key={id}
                onClick={() => setView(id)}
                style={{
                  padding: '6px 16px',
                  borderRadius: '6px 6px 0 0',
                  border: '1px solid var(--border)',
                  borderBottom: view === id ? '1px solid var(--bg-card)' : '1px solid var(--border)',
                  background: view === id ? 'var(--bg-card)' : 'transparent',
                  color: view === id ? 'var(--text-1)' : 'var(--text-3)',
                  cursor: 'pointer',
                  fontSize: 12,
                  fontWeight: view === id ? 600 : 400,
                  transition: 'all 0.1s'
                }}
              >
                {label}
              </button>
            ))}
          </div>

          <div style={{
            border: '1px solid var(--border)',
            borderRadius: '0 6px 6px 6px',
            background: 'var(--bg-card)',
            padding: '16px 16px'
          }}>

            {/* ── SUMMARY VIEW ──────────────────────────────────────────────── */}
            {view === 'summary' && (
              <>
                {vStream && (
                  <>
                    <SectionLabel>Video</SectionLabel>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
                      <StatCard
                        label="Resolución"
                        value={`${vStream.width}×${vStream.height}`}
                        sub={vStream.display_aspect_ratio}
                        accent
                      />
                      <StatCard
                        label="FPS"
                        value={fps}
                        sub={vStream.r_frame_rate !== vStream.avg_frame_rate
                          ? `avg ${parseFps(vStream.avg_frame_rate)}`
                          : undefined}
                      />
                      <StatCard
                        label="Codec"
                        value={vStream.codec_name?.toUpperCase()}
                        sub={vStream.profile ? `${vStream.profile}@L${vStream.level}` : undefined}
                      />
                      <StatCard
                        label="Bitrate Vídeo"
                        value={formatBitrate(vStream.bit_rate || fmt?.bit_rate)}
                      />
                      <StatCard label="Pix Format" value={vStream.pix_fmt} />
                      <StatCard
                        label="Color Space"
                        value={vStream.color_space || vStream.color_primaries || '—'}
                        sub={vStream.color_transfer}
                      />
                      <StatCard label="Scan" value={vStream.field_order || 'progressive'} />
                      <StatCard
                        label="Ratio SAR"
                        value={vStream.sample_aspect_ratio || '1:1'}
                      />
                    </div>
                  </>
                )}

                {aStream && (
                  <>
                    <SectionLabel>Audio</SectionLabel>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
                      <StatCard label="Codec Audio" value={aStream.codec_name?.toUpperCase()} sub={aStream.profile} />
                      <StatCard label="Sample Rate" value={aStream.sample_rate ? `${(+aStream.sample_rate / 1000).toFixed(1)} kHz` : '—'} />
                      <StatCard label="Canales" value={aStream.channels ? channelLabel(aStream.channels) : '—'} sub={aStream.channel_layout} />
                      <StatCard label="Bitrate Audio" value={formatBitrate(aStream.bit_rate)} />
                    </div>
                  </>
                )}

                <SectionLabel>Archivo</SectionLabel>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8 }}>
                  <StatCard label="Duración" value={formatDuration(fmt?.duration)} sub={fmt?.duration ? `${parseFloat(fmt.duration).toFixed(3)}s` : undefined} />
                  <StatCard label="Tamaño" value={formatSize(fmt?.size)} />
                  <StatCard label="Bitrate Total" value={formatBitrate(fmt?.bit_rate)} />
                  <StatCard label="Contenedor" value={fmt?.format_name?.split(',')[0]?.toUpperCase()} sub={fmt?.format_long_name} />
                </div>
              </>
            )}

            {/* ── JSON VIEW ─────────────────────────────────────────────────── */}
            {view === 'json' && (
              <div style={{ position: 'relative' }}>
                <button
                  onClick={copyJson}
                  style={{
                    position: 'absolute', top: 8, right: 8,
                    padding: '4px 12px', borderRadius: 5,
                    border: '1px solid var(--border)',
                    background: copied ? 'var(--accent-dim)' : 'var(--bg-input)',
                    color: copied ? 'var(--accent)' : 'var(--text-2)',
                    cursor: 'pointer', fontSize: 11, zIndex: 1,
                    transition: 'all 0.15s'
                  }}
                >
                  {copied ? '✓ Copiado' : 'Copiar'}
                </button>
                <pre style={{
                  margin: 0,
                  padding: '12px 14px',
                  background: 'var(--bg-app)',
                  border: '1px solid var(--border)',
                  borderRadius: 6,
                  fontSize: 11,
                  fontFamily: 'JetBrains Mono, monospace',
                  color: 'var(--text-2)',
                  overflowX: 'auto',
                  maxHeight: 420,
                  overflowY: 'auto',
                  lineHeight: 1.6,
                  whiteSpace: 'pre'
                }}>
                  {JSON.stringify(meta, null, 2)}
                </pre>
              </div>
            )}
          </div>

          {/* ── FRAME COUNTER ─────────────────────────────────────────────────── */}
          {vStream && (
            <div style={{
              marginTop: 16,
              background: 'var(--bg-card)',
              border: '1px solid var(--border)',
              borderRadius: 8,
              padding: '16px 18px'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 14 }}>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-1)' }}>Contador de Frames</div>
                  <div style={{ fontSize: 11, color: 'var(--text-3)', marginTop: 2 }}>
                    Vital para sincronizar con sistemas de control de iluminación (Timecode)
                  </div>
                </div>
                <button
                  onClick={countExact}
                  disabled={counting}
                  style={{
                    padding: '7px 14px', borderRadius: 6, fontSize: 12,
                    border: '1px solid var(--border)',
                    background: counting ? 'var(--bg-input)' : 'var(--bg-input)',
                    color: counting ? 'var(--text-3)' : 'var(--text-2)',
                    cursor: counting ? 'not-allowed' : 'pointer',
                    display: 'flex', alignItems: 'center', gap: 6,
                    transition: 'all 0.12s'
                  }}
                >
                  {counting
                    ? <><span style={{ display: 'inline-block', width: 10, height: 10, border: '1.5px solid var(--border)', borderTopColor: 'var(--accent)', borderRadius: '50%', animation: 'spin 0.7s linear infinite' }} /> Contando...</>
                    : '🎞 Contar frames exactos'
                  }
                </button>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10 }}>
                <div style={{ background: 'var(--bg-app)', border: '1px solid var(--border)', borderRadius: 7, padding: '10px 14px', textAlign: 'center' }}>
                  <div style={{ fontSize: 10, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>
                    {exactFrames ? 'Frames exactos' : estFrames ? 'Frames (estimado)' : 'Frames'}
                  </div>
                  <div style={{ fontSize: 22, fontWeight: 700, fontFamily: 'JetBrains Mono, monospace', color: 'var(--accent)' }}>
                    {frames?.toLocaleString('es-ES') ?? '—'}
                  </div>
                  {!exactFrames && estFrames && (
                    <div style={{ fontSize: 9, color: 'var(--text-3)', marginTop: 4 }}>duración × fps</div>
                  )}
                </div>

                <div style={{ background: 'var(--bg-app)', border: '1px solid var(--border)', borderRadius: 7, padding: '10px 14px', textAlign: 'center' }}>
                  <div style={{ fontSize: 10, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>
                    Timecode final
                  </div>
                  <div style={{ fontSize: 18, fontWeight: 700, fontFamily: 'JetBrains Mono, monospace', color: 'var(--text-1)', letterSpacing: 2 }}>
                    {toTimecode(frames, fpsNum)}
                  </div>
                  <div style={{ fontSize: 9, color: 'var(--text-3)', marginTop: 4 }}>HH:MM:SS:FF</div>
                </div>

                <div style={{ background: 'var(--bg-app)', border: '1px solid var(--border)', borderRadius: 7, padding: '10px 14px', textAlign: 'center' }}>
                  <div style={{ fontSize: 10, color: 'var(--text-3)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>
                    Frame rate
                  </div>
                  <div style={{ fontSize: 22, fontWeight: 700, fontFamily: 'JetBrains Mono, monospace', color: 'var(--text-1)' }}>
                    {fps}
                  </div>
                  <div style={{ fontSize: 9, color: 'var(--text-3)', marginTop: 4 }}>fps</div>
                </div>
              </div>
            </div>
          )}
        </>
      )}

      {!meta && !loading && input && (
        <div style={{ marginTop: 24, textAlign: 'center', color: 'var(--text-3)', fontSize: 13 }}>
          No se pudo leer el archivo.
        </div>
      )}

      {!input && (
        <div style={{
          marginTop: 40, textAlign: 'center', color: 'var(--text-3)',
          display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10
        }}>
          <span style={{ fontSize: 36 }}>🔬</span>
          <span style={{ fontSize: 13 }}>Selecciona cualquier archivo multimedia para analizarlo</span>
        </div>
      )}
    </div>
  )
}
