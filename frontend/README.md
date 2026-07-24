# Frontend — Visualizador de carreras (Fase 4)

Frontend 2D en Canvas, **mobile-first** y PWA-ready, que anima el JSON que
devuelve la API (`POST /simular`).

## Qué incluye

- **Trazado 2D** en Canvas (elipse) con línea de meta y línea central.
- **Animación de los coches** posicionados según su diferencia de tiempo al
  líder: se ven adelantamientos y el pelotón se agrupa solo bajo safety car.
- **Panel de clasificación en vivo**: posición, color de escudería, neumático,
  número de paradas y gap al líder; los abandonos se tachan.
- **Bandera de carrera** (verde / safety car / VSC) y banner de neutralización.
- **Feed de eventos** (paradas, adelantamientos, incidentes, abandonos, SC).
- **Controles de reproducción**: play/pausa, reinicio, barra de vueltas
  (scrubber) y control de velocidad.
- **Configuración**: categoría (se puebla desde `GET /categorias`), vueltas,
  clima y semilla.

## Cómo probarlo

Necesitas la API en marcha (ver `/api`). Desde la raíz del repo, en dos terminales:

```bash
# 1) API
uvicorn api.main:app --reload

# 2) Frontend (servidor estático simple)
python3 -m http.server 5500 --directory frontend
```

Abre **http://127.0.0.1:5500** en el navegador (o en el móvil, apuntando a la IP
de tu equipo). Al cargar se lanza una simulación de ejemplo automáticamente;
cambia categoría/clima/semilla y pulsa **Simular** para relanzar.

> El indicador ● de la cabecera se pone verde si la API responde. Si está rojo,
> arranca `uvicorn api.main:app` primero.

## Convertir en PWA (más adelante)

Ya está el `manifest.json` y el diseño responsive. El siguiente paso sería
añadir un *service worker* para cache offline e instalación en pantalla de
inicio.
