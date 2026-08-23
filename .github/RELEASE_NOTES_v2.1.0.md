## Format Media Coder 2.1 — Interfaz WinUI 3

Misma app que la 2.0 (preparación de material para eventos), ahora con una
**interfaz nativa WinUI 3** (Fluent Design), sobria y técnica.

### Novedades de la interfaz
- 🪟 **WinUI 3 nativo**: `NavigationView` con grupos Flujo / Herramientas, fondo
  **Mica**, barra de título integrada y **tipografía Segoe UI Variable** del sistema.
- ◻️ **Iconos Segoe Fluent** monocromos (sin emojis de colores), estética sobria.
- 📊 **Diagnóstico con stat tiles**: Total / Listos / Recodificar / No se pueden,
  de un vistazo.

### Sigue igual que la 2.0
- Flujo **Preparar Evento** (entrada → destino → diagnóstico → plan).
- **Convertir Vídeo** con HAP y el fix del alfa (`hap_alpha` / `hap_q`).
- **Analizador**. Núcleo de lógica pura con 36 tests.

### Descarga
`FormatMediaCoder-v2.1.0-win-x64.zip` — descomprime y ejecuta `FormatMediaCoder.exe`.
Autónomo (no necesitas instalar .NET ni el Windows App SDK) y con **FFmpeg (HAP) incluido**.

### Requisitos
- Windows 10 (1809+) / Windows 11, 64-bit.
