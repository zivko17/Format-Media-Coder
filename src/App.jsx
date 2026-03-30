import { useState, useEffect, useCallback, useRef } from 'react'
import Sidebar from './components/Sidebar'
import TitleBar from './components/TitleBar'
import Convert from './components/Convert'
import Compress from './components/Compress'
import TrimMerge from './components/TrimMerge'
import Extract from './components/Extract'
import ImageConvert from './components/ImageConvert'
import Probe from './components/Probe'
import AudioConvert from './components/AudioConvert'
import ProgressOverlay from './components/ProgressOverlay'
import Toast from './components/Toast'
import About from './components/About'

export default function App() {
  const [tab, setTab] = useState('convert')
  const [processing, setProcessing] = useState(false)
  const [progress, setProgress] = useState({ percent: 0, timemark: '', fps: 0 })
  const [toast, setToast] = useState(null)
  const [currentFile, setCurrentFile] = useState('')
  const [queueInfo, setQueueInfo] = useState('')
  const batchModeRef = useRef(false)

  const showToast = useCallback((type, message, output) => {
    setToast({ type, message, output, id: Date.now() })
  }, [])

  const setBatchMode = (v) => { batchModeRef.current = v }

  useEffect(() => {
    const off1 = window.api.onProgress(data => setProgress(data))
    const off2 = window.api.onDone(({ output }) => {
      if (!batchModeRef.current) {
        setProcessing(false)
        showToast('success', 'Completado', output)
      }
      setProgress({ percent: 100, timemark: '', fps: 0 })
      setTimeout(() => setProgress({ percent: 0 }), 1500)
    })
    const off3 = window.api.onError(({ message }) => {
      if (!batchModeRef.current) {
        setProcessing(false)
      }
      setProgress({ percent: 0 })
      showToast('error', message)
    })
    return () => { off1(); off2(); off3() }
  }, [showToast])

  const pages = {
    convert: Convert,
    compress: Compress,
    trim: TrimMerge,
    extract: Extract,
    image: ImageConvert,
    probe: Probe,
    audio: AudioConvert,
    about: About
  }

  const Page = pages[tab]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', background: 'var(--bg-app)' }}>
      <TitleBar />
      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
        <Sidebar active={tab} onChange={setTab} />
        <main style={{ flex: 1, overflowY: 'auto', padding: '28px 32px' }}>
          <Page
            processing={processing}
            setProcessing={setProcessing}
            showToast={showToast}
            setCurrentFile={setCurrentFile}
            setQueueInfo={setQueueInfo}
            setBatchMode={setBatchMode}
          />
        </main>
      </div>
      <ProgressOverlay
        processing={processing}
        progress={progress}
        currentFile={currentFile}
        queueInfo={queueInfo}
        onCancel={() => {
          window.api.cancel()
          setProcessing(false)
          setProgress({ percent: 0 })
        }}
      />
      {toast && <Toast key={toast.id} toast={toast} onClose={() => setToast(null)} />}
    </div>
  )
}
