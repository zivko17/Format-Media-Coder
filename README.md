# 🎬 Format Media Coder

**Prepara material audiovisual para eventos.** Recibe lo que te manda el cliente,
en el desorden en que te lo manda, y lo deja listo para el servidor de
reproducción del bolo (Resolume, Watchout, vMix/OBS, reproductores LED).

No es una caja de herramientas genérica de FFmpeg más: la unidad central es el
**perfil de destino**. La app mira cada archivo, lo compara con el destino y
**opina**: "esto no va a funcionar, y por esto". Ese juicio es el producto.

> Reescritura en **C# / .NET 8 + WPF**. La versión anterior (Electron/React) queda
> archivada en [`legacy-electron/`](legacy-electron/) como referencia.

---

## Arquitectura

```
FormatMediaCoder.sln
├── src/FormatMediaCoder.Core        ← lógica pura, multiplataforma, testeable
│   ├── Models/                       TargetProfile · MediaInfo · ComplianceReport · PrepPlan · SlideSize
│   ├── Profiles/BuiltinProfiles.cs   Resolume HAP/HAP Alpha, Watchout, vMix/OBS, LED
│   └── Services/
│       ├── ComplianceService.cs      EL MOTOR: archivo + perfil → desviaciones
│       ├── PrepPlanBuilder.cs         desviaciones → plan (copiar si ya cumple)
│       ├── FfmpegArgs.cs              construcción de args (incl. fix del alfa HAP)
│       ├── ProbeParser.cs             JSON de ffprobe → MediaInfo
│       ├── ProfileStore.cs            perfiles como JSON en %AppData%
│       ├── SlideLayout.cs             encaje de páginas en la diapositiva y resolución
│       └── PptxWriter.cs              escritura del .pptx (OOXML) sin dependencias
│
├── src/FormatMediaCoder.App          ← WPF (Windows). Solo proceso y UI.
│   ├── Services/FFmpegService.cs      spawn de ffmpeg/ffprobe, progreso, cancelación
│   ├── Services/PdfRenderService.cs   rasterizado de PDF con el motor de Windows
│   ├── ViewModels/                    MVVM de Preparar Evento y de las herramientas
│   └── Views/PrepararEventoView       los 4 pasos: entrada → destino → diagnóstico → plan
│
└── tests/FormatMediaCoder.Core.Tests ← xUnit sobre el Core (57 tests)
```

El **Core no depende de WPF ni de FFmpeg**: es lógica pura y se compila y prueba
en cualquier sistema. La **App** es WPF y solo compila en Windows.

## El flujo: Preparar Evento

1. **Entrada** — suelta una carpeta entera (con subcarpetas), como llega del cliente.
2. **Destino** — elige el perfil del sistema de reproducción del bolo.
3. **Diagnóstico** — tabla con un archivo por fila y su semáforo: 🟢 cumple ·
   🟡 avisa · 🔴 bloquea · ⚪ no se puede juzgar (perfil incompleto). Arriba, el
   resumen: *"34 archivos · 12 listos · 18 hay que recodificar · 4 no se pueden
   arreglar solos"*.
4. **Plan y ejecución** — qué se hará con cada uno (desmarcable). Si un archivo ya
   cumple, **se copia tal cual, no se recodifica**. La salida va a `_preparado/`,
   sin sobrescribir nada.

## La herramienta: PDF → PowerPoint

El cliente manda el guion, el dossier o los rótulos en PDF, y el bolo se dispara
desde PowerPoint. Esta herramienta hace ese salto:

- **Cada página → una diapositiva** a página completa. Si la proporción no cuadra
  con la diapositiva, la página se centra entera con bandas: **nunca se recorta
  ni se deforma**.
- **Varios PDFs → una sola presentación**, en el orden que marques en la lista.
- **Tamaño de diapositiva**: 16:9, 4:3, o el formato exacto del PDF (para pantallas
  LED con proporciones raras).
- **Resolución**: de 1280 px a 4K en el lado largo. El PDF es vectorial, así que
  la nitidez la decides tú: 4K si el texto tiene que leerse en una pantalla grande.
- **PNG o JPEG**: PNG para texto y vectores; JPEG para escaneados y fotos, que
  bajan el archivo de cientos de MB a unas decenas.

**No convierte el PDF en texto editable**, y es deliberado: reconstruir cajas de
texto depende de tener las fuentes instaladas y descoloca líneas y tablas. En un
evento lo que no puede pasar es que una diapositiva se vea distinta a como la
aprobó el cliente, así que se rasteriza: **lo que se proyecta es exactamente el PDF**.

El rasterizado usa `Windows.Data.Pdf`, el motor de PDF que ya trae Windows (el
mismo de Edge): no añade binarios ni dependencias al proyecto. El `.pptx` se
escribe desde el Core, sin librerías de terceros, y las diapositivas se vuelcan
al disco según se generan, así que un PDF de 300 páginas en 4K no se acumula en
memoria. Si algo falla o se cancela, el archivo a medias se borra.

## Requisitos

- **.NET 8 SDK** → https://dotnet.microsoft.com/download
- **Windows 10 (versión 2004 / build 19041) o superior** para la app WPF. Esa es
  la versión del SDK a la que apunta el proyecto para usar el motor de PDF del
  sistema; FFmpeg y el resto funcionan igual en versiones anteriores.
- **FFmpeg con soporte HAP** (`bin\ffmpeg.exe`, `bin\ffprobe.exe`). El full-build de
  [gyan.dev](https://www.gyan.dev/ffmpeg/builds/) incluye el encoder `hap`. Si no
  hay `bin\`, la app usa el `ffmpeg`/`ffprobe` del PATH.

## Compilar y ejecutar

```bash
# Compilar todo (en Windows)
dotnet build

# Ejecutar la app (Windows)
dotnet run --project src/FormatMediaCoder.App

# Ejecutar los tests del núcleo (cualquier sistema)
dotnet test tests/FormatMediaCoder.Core.Tests
```

## Nota técnica: HAP Alpha

El encoder `hap` de FFmpeg tiene tres variantes y **por defecto sale `hap`
(DXT1, sin canal alfa)**. La versión anterior hacía `-c:v hap -vf format=rgba`
sin pasar `-format`, así que aunque entregara una imagen con alfa, el archivo
resultante perdía la transparencia — fatal para logos y capas en Resolume.

Aquí se corrige: `FfmpegArgs.BuildEncodeArgs` pasa `-format hap_alpha` (con alfa)
o `-format hap_q` (máxima calidad sin alfa) según la variante fija del perfil.
Cubierto por tests (`FfmpegArgsTests`).

> **Pendiente de verificar en Windows** (10 min): generar un clip con
> transparencia, pasarlo por la app con un perfil HAP Alpha y comprobar en
> Resolume que el alfa sobrevive.

## Perfiles de destino

| Perfil | Estado |
|---|---|
| Resolume Arena — HAP Alpha | ✅ Completo |
| Resolume Arena — HAP Q | ✅ Completo |
| Watchout — HAP | ✅ Usable · variante/límites por confirmar |
| vMix / OBS | ⚪ Por definir (faltan specs) |
| Reproductor LED | ⚪ Por definir (varía por fabricante) |

Los perfiles incompletos no se juzgan a ojo: la app lo dice en vez de inventar un
veredicto. Los perfiles de usuario se guardan como JSON en
`%AppData%\FormatMediaCoder\profiles\` y se pueden compartir entre equipos.

## Estado / pendientes

- [ ] Confirmar specs de vMix/OBS y reproductor LED para completar sus perfiles.
- [ ] Verificar el alfa HAP en Resolume real (Windows).
- [ ] Probar **PDF → PowerPoint** en Windows con PDFs de cliente reales: páginas
      de tamaños mixtos, PDFs protegidos con contraseña y dossieres muy pesados.
      El Core (montaje del `.pptx` y encaje de páginas) ya está cubierto por tests;
      lo que falta es el rasterizado, que depende del motor de Windows.
- [ ] Portar las herramientas del trabajo suelto que quedan (hoy agrupadas bajo
      "Herramientas", pendientes de porte desde la versión Electron).
- [ ] Firma de código del instalador y auto-updater.
