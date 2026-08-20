## Format Media Coder 2.0 — Enfoque eventos

Reescritura completa en **C# / .NET 8 + WPF**. La app deja de ser una caja de
herramientas genérica de FFmpeg y pasa a **preparar material para eventos**:
mira cada archivo, lo compara con el sistema de reproducción del bolo y opina.

### Novedades
- 🎯 **Preparar Evento**: suelta la carpeta del cliente, elige destino y ve en
  un vistazo qué archivos fallarán y por qué (semáforo verde/ámbar/rojo).
- 🧩 **Perfiles de destino**: Resolume HAP / HAP Alpha, Watchout HAP.
- 🔴 **Fix del alfa HAP**: la variante (`hap_alpha` / `hap_q`) ahora se aplica de
  verdad; los logos y capas conservan la transparencia en Resolume.
- 🧪 Núcleo de lógica pura con 36 tests.
- 🗂 Menú reagrupado en Flujo + Herramientas.

### Descarga
`FormatMediaCoder-v2.0.0-win-x64.zip` — descomprime y ejecuta `FormatMediaCoder.exe`.
Es **autónomo** (no necesitas instalar .NET) y trae **FFmpeg con HAP** incluido.

### Requisitos
- Windows 10/11 (64-bit).
