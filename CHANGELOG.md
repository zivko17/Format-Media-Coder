# Changelog

Historial de versiones de Format Media Coder. Formato basado en
[Keep a Changelog](https://keepachangelog.com/es/1.0.0/).

## [2.1.0] — 2026-08-23 · Interfaz WinUI 3

Misma app que la 2.0, con la interfaz migrada a **WinUI 3** (Fluent Design),
sobria y técnica.

### Cambiado
- 🪟 UI nativa **WinUI 3**: `NavigationView` (Flujo / Herramientas), fondo Mica,
  barra de título integrada y tipografía **Segoe UI Variable** del sistema.
- ◻️ Iconos **Segoe Fluent** monocromos (sin emojis de colores).
- 📊 Diagnóstico con **stat tiles** (Total / Listos / Recodificar / No se pueden).

### Se mantiene
- Flujo Preparar Evento, Convertir Vídeo (HAP + fix del alfa), Analizador.
- Núcleo de lógica pura con 36 tests, intacto.

## [2.0.0] — 2026-07-28 · Enfoque eventos

Reescritura completa en **C# / .NET 8 + WPF**. La app deja de ser una caja de
herramientas genérica de FFmpeg y pasa a **preparar material para eventos**:
mira cada archivo, lo compara con el sistema de reproducción del bolo y opina.

### Añadido
- 🎯 **Preparar Evento**: flujo de 4 pasos (entrada → destino → diagnóstico →
  plan). Suelta la carpeta del cliente, elige destino y ve en un vistazo qué
  archivos fallarán y por qué (semáforo verde/ámbar/rojo).
- 🧩 **Perfiles de destino** (`TargetProfile`): Resolume HAP / HAP Alpha y
  Watchout HAP. Los perfiles de vMix/OBS y reproductor LED quedan marcados como
  incompletos a propósito hasta confirmar sus especificaciones.
- ⚙️ **Motor de cumplimiento** (`ComplianceService`): lógica pura que compara un
  archivo con un perfil y devuelve las desviaciones con su gravedad y arreglo.
- 📋 **Plan de preparación** (`PrepPlanBuilder`): si un archivo ya cumple, se
  copia tal cual; no se recodifica por costumbre.
- 🛠 Herramientas portadas a WPF: **Convertir Vídeo** y **Analizador**.
- 🧪 Núcleo de lógica pura con 36 tests (xUnit).
- 🗂 Menú reagrupado en **Flujo** + **Herramientas**.
- 🚀 Lanzador `abrir.cmd` de doble clic.

### Corregido
- 🔴 **Bug del alfa HAP**: la rama HAP no pasaba `-format`, así que la variante
  por defecto (DXT1, sin alfa) descartaba la transparencia. Ahora se aplica
  `hap_alpha` / `hap_q` según el perfil y los logos/capas conservan el alfa.

### Cambiado
- La versión anterior (Electron + React) queda archivada en `legacy-electron/`.

### Pendiente
- Confirmar specs de vMix/OBS y reproductor LED.
- Verificar el alfa HAP en Resolume real (Windows).
- Portar las herramientas restantes (Comprimir, Convertir Audio, Extraer,
  Convertir Imágenes, Edición).
- Firma de código del instalador y auto-updater.

## [1.1.0] — 2026-03-30

### Añadido
- Interfaz disponible en Español e Inglés, con toggle ES/EN en la barra de título.

## [1.0.0] — 2026-03-30

- Primera versión estable. Interfaz gráfica (GUI wrapper) para FFmpeg, con
  FFmpeg incluido en el instalador.

[2.1.0]: https://github.com/zivko17/Format-Media-Coder/releases/tag/v2.1.0
[2.0.0]: https://github.com/zivko17/Format-Media-Coder/releases/tag/v2.0.0
[1.1.0]: https://github.com/zivko17/Format-Media-Coder/releases/tag/v1.1.0
[1.0.0]: https://github.com/zivko17/Format-Media-Coder/releases/tag/V1.0.0
