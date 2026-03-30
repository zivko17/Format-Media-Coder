# 🎬 FFmpeg GUI

App de escritorio para FFmpeg — Electron + React + Vite + Tailwind.

## Requisitos

- **Node.js v18+** → https://nodejs.org
- **VSCode** (ya instalado ✓)
- **No necesitas instalar FFmpeg** — viene incluido como paquete npm (`ffmpeg-static`)

## Instalación

```bash
# 1. Entrar a la carpeta del proyecto
cd ffmpeg-gui

# 2. Instalar dependencias (tarda 1-2 min la primera vez)
npm install

# 3. Arrancar en modo desarrollo
npm run dev
```

Se abrirá la ventana de la app automáticamente.

## Scripts disponibles

| Comando | Descripción |
|---------|-------------|
| `npm run dev` | Modo desarrollo con hot-reload |
| `npm run build` | Compila y empaqueta la app |

## Estructura del proyecto

```
ffmpeg-gui/
├── electron/
│   ├── main.js        ← Proceso principal Electron + lógica FFmpeg
│   └── preload.js     ← Puente seguro entre Electron y React
├── src/
│   ├── components/    ← Todos los componentes de la UI
│   ├── App.jsx        ← Componente raíz
│   ├── main.jsx       ← Entry point React
│   └── index.css      ← Estilos globales
├── index.html
├── vite.config.js
├── tailwind.config.js
└── package.json
```

## Funcionalidades

- 🔄 **Convertir** — MP4, WebM, MKV, AVI, MOV, MP3, AAC, WAV
- 📦 **Comprimir** — Control CRF, preset, resolución
- ✂️ **Recortar** — Trim por tiempo (HH:MM:SS)
- 🔗 **Unir** — Merge de múltiples archivos
- 🎵 **Extraer Audio** — MP3, AAC, WAV, FLAC, OGG
- 🖼️ **Extraer Frames** — JPG/PNG a la frecuencia que quieras
