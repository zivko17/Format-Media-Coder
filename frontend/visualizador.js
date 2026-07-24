/* ============================================================================
   Visualizador de carreras — anima el JSON de la API sobre un Canvas 2D.

   Idea del modelo de posición: no tenemos coordenadas de pista en el JSON, sino
   la clasificación y el tiempo acumulado por vuelta. Colocamos cada coche sobre
   el trazado (una elipse cerrada) según su DIFERENCIA DE TIEMPO al líder: el
   líder va en cabeza y el resto se reparten hacia atrás en proporción a su gap.
   Así se ven los adelantamientos (cuando dos gaps se cruzan) y el pelotón se
   agrupa solo bajo safety car (porque el gap del líder por vuelta se reduce).
   ============================================================================ */

'use strict';

// --- Conexión con la API -----------------------------------------------------
const API_BASE =
  location.protocol === 'file:' ? 'http://127.0.0.1:8000'
  : location.port === '8000' ? ''
  : `${location.protocol}//${location.hostname}:8000`;

// Paleta de colores para asignar a cada escudería según aparecen.
const PALETA = [
  '#ff2d55', '#3b82f6', '#22c55e', '#f5c518', '#a855f7',
  '#f59e0b', '#06b6d4', '#ec4899', '#84cc16', '#fb7185',
  '#14b8a6', '#f97316', '#8b5cf6', '#eab308', '#38bdf8',
  '#e11d48', '#10b981', '#6366f1', '#d946ef', '#facc15',
];

const ETIQUETAS_EVENTO = {
  parada: '🔧 Parada en boxes',
  adelanta: '↗️ Adelantamiento',
  incidente_leve: '⚠️ Incidente leve',
  incidente_medio: '⚠️ Incidente con daño',
  dnf_error: '💥 Abandono (error)',
  dnf_mecanico: '🔩 Abandono (mecánico)',
  dnf_contacto: '💢 Abandono (contacto)',
  sc: '🟡 Safety Car',
  vsc: '🟠 Virtual Safety Car',
};

// --- Estado global -----------------------------------------------------------
const estado = {
  resultado: null,       // JSON completo de la carrera
  lapMaps: [],           // por vuelta: Map(id -> fila)
  leaderTime: [],        // por vuelta: tiempo_total del líder
  refLap: [],            // por vuelta: tiempo de vuelta de referencia (líder)
  colores: {},           // escuderia -> color
  numVueltas: 0,
  lapFloat: 0,           // posición continua de reproducción (0..numVueltas-1)
  playing: false,
  velocidad: 2,          // vueltas por segundo
  ultimoIdxPanel: -1,
};

// --- Referencias DOM ---------------------------------------------------------
const $ = (id) => document.getElementById(id);
const canvas = $('pista');
const ctx = canvas.getContext('2d');
let geom = null;   // geometría del trazado (se recalcula al redimensionar)

// ============================================================================
// Arranque
// ============================================================================
window.addEventListener('load', async () => {
  ajustarCanvas();
  window.addEventListener('resize', () => { ajustarCanvas(); dibujar(); });
  await comprobarSalud();
  await cargarCategorias();
  configurarControles();
  // Simulación inicial para que se vea algo al entrar.
  lanzarSimulacion();
});

async function comprobarSalud() {
  const dot = $('estado-api');
  try {
    const r = await fetch(`${API_BASE}/health`);
    if (!r.ok) throw new Error();
    dot.classList.add('ok');
    dot.title = 'API conectada';
  } catch {
    dot.classList.add('error');
    dot.title = 'API no disponible (arranca uvicorn api.main:app)';
  }
}

async function cargarCategorias() {
  const sel = $('categoria');
  try {
    const cats = await fetch(`${API_BASE}/categorias`).then((r) => r.json());
    sel.innerHTML = '';
    for (const c of cats) {
      const opt = document.createElement('option');
      opt.value = c.id;
      opt.textContent = `${c.nombre} (${c.familia.replace('_', ' ')})`;
      sel.appendChild(opt);
    }
    sel.value = 'f1';
  } catch {
    sel.innerHTML = '<option value="generica">Genérica</option>';
  }
}

// ============================================================================
// Lanzar simulación (POST /simular) y preparar los datos
// ============================================================================
async function lanzarSimulacion() {
  const btn = $('btn-simular');
  btn.disabled = true;
  $('cargando').classList.remove('oculto');

  const cuerpo = {
    categoria: $('categoria').value,
    clima: $('clima').value,
  };
  const vueltas = parseInt($('vueltas').value, 10);
  if (!Number.isNaN(vueltas)) cuerpo.num_vueltas = vueltas;
  const semilla = parseInt($('semilla').value, 10);
  if (!Number.isNaN(semilla)) cuerpo.semilla = semilla;

  try {
    const r = await fetch(`${API_BASE}/simular`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(cuerpo),
    });
    if (!r.ok) {
      const err = await r.json().catch(() => ({}));
      throw new Error(err.detail || `Error ${r.status}`);
    }
    prepararDatos(await r.json());
    reiniciar();
    play();
  } catch (e) {
    alert('No se pudo simular: ' + e.message +
          '\n¿Está arrancada la API? (uvicorn api.main:app --reload)');
  } finally {
    btn.disabled = false;
    $('cargando').classList.add('oculto');
  }
}

function prepararDatos(resultado) {
  estado.resultado = resultado;
  estado.numVueltas = resultado.vueltas.length;
  estado.lapMaps = [];
  estado.leaderTime = [];
  estado.refLap = [];
  estado.colores = {};

  let colorIdx = 0;
  let ultimoRef = resultado.vueltas[0]?.[0]?.tiempo_vuelta || 90;

  resultado.vueltas.forEach((filas) => {
    const mapa = new Map();
    for (const f of filas) {
      mapa.set(f.id_piloto, f);
      if (!(f.escuderia in estado.colores)) {
        estado.colores[f.escuderia] = PALETA[colorIdx++ % PALETA.length];
      }
    }
    const lider = filas.find((f) => f.posicion === 1) || filas[0];
    estado.lapMaps.push(mapa);
    estado.leaderTime.push(lider.tiempo_total);
    const ref = lider.tiempo_vuelta > 1 ? lider.tiempo_vuelta : ultimoRef;
    ultimoRef = ref;
    estado.refLap.push(ref);
  });

  $('vuelta-total').textContent = `/ ${estado.numVueltas}`;
  $('scrubber').max = String(estado.numVueltas - 1);
}

// ============================================================================
// Geometría del trazado (elipse)
// ============================================================================
function ajustarCanvas() {
  const dpr = window.devicePixelRatio || 1;
  const w = canvas.clientWidth || canvas.parentElement.clientWidth;
  const h = canvas.clientHeight || w;
  canvas.width = Math.round(w * dpr);
  canvas.height = Math.round(h * dpr);
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

  const margen = Math.min(w, h) * 0.14;
  geom = {
    w, h,
    cx: w / 2, cy: h / 2,
    rx: (w - 2 * margen) / 2,
    ry: (h - 2 * margen) / 2,
    ancho: Math.max(10, Math.min(w, h) * 0.05), // ancho de la banda de asfalto
  };
}

// Punto (x,y) sobre la línea central del trazado para el parámetro t in [0,1).
function puntoEnTrazado(t) {
  const ang = -Math.PI / 2 + (t % 1) * Math.PI * 2; // arranca arriba, sentido horario
  return {
    x: geom.cx + geom.rx * Math.cos(ang),
    y: geom.cy + geom.ry * Math.sin(ang),
  };
}

// ============================================================================
// Bucle de animación
// ============================================================================
let ultimoTs = 0;
function bucle(ts) {
  const dt = (ts - ultimoTs) / 1000 || 0;
  ultimoTs = ts;
  if (estado.playing && estado.numVueltas > 0) {
    estado.lapFloat += dt * estado.velocidad;
    if (estado.lapFloat >= estado.numVueltas - 1) {
      estado.lapFloat = estado.numVueltas - 1;
      pausar();
    }
    $('scrubber').value = String(estado.lapFloat);
  }
  dibujar();
  requestAnimationFrame(bucle);
}
requestAnimationFrame((ts) => { ultimoTs = ts; bucle(ts); });

// ============================================================================
// Dibujo
// ============================================================================
function dibujar() {
  if (!geom) return;
  ctx.clearRect(0, 0, geom.w, geom.h);

  const idx = Math.max(0, Math.min(estado.numVueltas - 1, Math.round(estado.lapFloat)));
  const neutralizado = estado.resultado ? bandera(idx) : null;

  dibujarTrazado(neutralizado);

  if (!estado.resultado) return;

  dibujarCoches();
  actualizarPanel(idx, neutralizado);
}

function dibujarTrazado(neutralizado) {
  const { cx, cy, rx, ry, ancho } = geom;

  // Banda de asfalto (dos elipses).
  ctx.lineWidth = ancho;
  ctx.strokeStyle = neutralizado === 'sc' ? '#3a3720'
                  : neutralizado === 'vsc' ? '#3a3020' : '#20242e';
  ctx.beginPath();
  ctx.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
  ctx.stroke();

  // Línea central discontinua.
  ctx.lineWidth = 2;
  ctx.strokeStyle = 'rgba(255,255,255,0.15)';
  ctx.setLineDash([8, 10]);
  ctx.beginPath();
  ctx.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.setLineDash([]);

  // Línea de meta (arriba).
  const p = puntoEnTrazado(0);
  ctx.strokeStyle = '#ffffff';
  ctx.lineWidth = 3;
  ctx.beginPath();
  ctx.moveTo(p.x, p.y - ancho / 2);
  ctx.lineTo(p.x, p.y + ancho / 2);
  ctx.stroke();
}

function dibujarCoches() {
  const L = Math.floor(estado.lapFloat);
  const frac = estado.lapFloat - L;
  const L1 = Math.min(L + 1, estado.numVueltas - 1);

  const mapaL = estado.lapMaps[L];
  const mapaL1 = estado.lapMaps[L1];
  const ref = estado.refLap[L] || 90;
  const liderL = estado.leaderTime[L];
  const liderL1 = estado.leaderTime[L1];

  const radio = Math.max(6, Math.min(geom.w, geom.h) * 0.018);

  // Preparamos la lista de coches con su gap interpolado.
  const coches = [];
  for (const [id, filaL] of mapaL) {
    if (filaL.estado === 'abandonado') continue; // los retirados no van en pista
    const filaL1 = mapaL1.get(id) || filaL;
    const gapL = filaL.tiempo_total - liderL;
    const gapL1 = filaL1.tiempo_total - liderL1;
    const gap = gapL + (gapL1 - gapL) * frac;
    const tCoche = frac - gap / ref;            // hacia atrás desde el líder
    coches.push({ id, fila: filaL, t: tCoche, gap });
  }

  // Dibujar de atrás hacia delante (líder encima).
  coches.sort((a, b) => b.gap - a.gap);
  for (const c of coches) {
    const p = puntoEnTrazado(((c.t % 1) + 1) % 1);
    const color = estado.colores[c.fila.escuderia] || '#888';

    // Anillo de evento (parada, adelantamiento, incidente).
    const tieneEvento = c.fila.eventos && c.fila.eventos.some(
      (e) => e !== 'sc' && e !== 'vsc');
    if (tieneEvento) {
      ctx.beginPath();
      ctx.arc(p.x, p.y, radio + 4, 0, Math.PI * 2);
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth = 2;
      ctx.stroke();
    }

    // Punto del coche.
    ctx.beginPath();
    ctx.arc(p.x, p.y, radio, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.lineWidth = c.fila.posicion === 1 ? 3 : 1.5;
    ctx.strokeStyle = c.fila.posicion === 1 ? '#fff' : 'rgba(0,0,0,0.5)';
    ctx.stroke();

    // Número de posición.
    ctx.fillStyle = '#0b0e14';
    ctx.font = `700 ${radio}px system-ui, sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(String(c.fila.posicion), p.x, p.y);
  }
}

// ============================================================================
// Panel lateral: clasificación, bandera y eventos
// ============================================================================
function bandera(idx) {
  const filas = estado.resultado.vueltas[idx];
  for (const f of filas) {
    if (f.eventos.includes('sc')) return 'sc';
    if (f.eventos.includes('vsc')) return 'vsc';
  }
  return null;
}

function actualizarPanel(idx, neutralizado) {
  $('vuelta-actual').textContent = idx + 1;

  const band = $('bandera');
  if (neutralizado === 'sc') { band.textContent = 'SAFETY CAR'; band.className = 'bandera bandera-sc'; }
  else if (neutralizado === 'vsc') { band.textContent = 'VSC'; band.className = 'bandera bandera-vsc'; }
  else { band.textContent = 'VERDE'; band.className = 'bandera bandera-verde'; }

  const banner = $('banner-evento');
  if (neutralizado) {
    banner.textContent = ETIQUETAS_EVENTO[neutralizado];
    banner.classList.remove('oculto');
  } else {
    banner.classList.add('oculto');
  }

  // Solo reconstruimos la lista al cambiar de vuelta (no en cada frame).
  if (idx === estado.ultimoIdxPanel) return;
  estado.ultimoIdxPanel = idx;

  renderClasificacion(idx);
  renderEventos(idx);
}

function renderClasificacion(idx) {
  const filas = estado.resultado.vueltas[idx];
  const lista = $('clasificacion');
  lista.innerHTML = '';
  const liderTime = estado.leaderTime[idx];

  for (const f of filas) {
    const li = document.createElement('li');
    li.className = 'fila' + (f.estado === 'abandonado' ? ' dnf' : '');

    const gapTxt = f.estado === 'abandonado' ? 'OUT'
      : f.posicion === 1 ? 'Líder'
      : `+${(f.tiempo_total - liderTime).toFixed(1)}s`;

    const parada = f.paradas > 0 ? `<span class="parada-badge">P${f.paradas}</span>` : '';

    li.innerHTML = `
      <span class="pos">${f.posicion}</span>
      <span class="color" style="background:${estado.colores[f.escuderia] || '#888'}"></span>
      <span class="nombre">${f.nombre}</span>
      <span class="meta">
        <span class="neum neum-${f.neumatico}">${(f.neumatico || '').slice(0,3).toUpperCase()}</span>
        ${parada}
        <span class="gap">${gapTxt}</span>
      </span>`;
    lista.appendChild(li);
  }
}

function renderEventos(idx) {
  // Recolectamos eventos notables hasta la vuelta actual y mostramos los últimos.
  const items = [];
  for (let L = 0; L <= idx; L++) {
    const filas = estado.resultado.vueltas[L];
    const scVisto = new Set();
    for (const f of filas) {
      for (const ev of f.eventos) {
        if (ev === 'sc' || ev === 'vsc') {
          if (scVisto.has(ev)) continue;
          scVisto.add(ev);
          items.push({ v: L + 1, txt: ETIQUETAS_EVENTO[ev] });
        } else if (ETIQUETAS_EVENTO[ev]) {
          items.push({ v: L + 1, txt: `${ETIQUETAS_EVENTO[ev]} · ${f.nombre}` });
        }
      }
    }
  }
  const lista = $('eventos');
  lista.innerHTML = '';
  for (const it of items.slice(-14).reverse()) {
    const li = document.createElement('li');
    li.innerHTML = `<span class="v">V${it.v}</span>${it.txt}`;
    lista.appendChild(li);
  }
}

// ============================================================================
// Controles de reproducción
// ============================================================================
function configurarControles() {
  $('config').addEventListener('submit', (e) => { e.preventDefault(); lanzarSimulacion(); });
  $('btn-play').addEventListener('click', () => (estado.playing ? pausar() : play()));
  $('btn-reinicio').addEventListener('click', reiniciar);
  $('scrubber').addEventListener('input', (e) => {
    pausar();
    estado.lapFloat = parseFloat(e.target.value);
  });
  const vel = $('velocidad');
  vel.addEventListener('input', (e) => {
    estado.velocidad = parseFloat(e.target.value);
    $('velocidad-val').textContent = `${estado.velocidad}×`;
  });
  $('velocidad-val').textContent = `${estado.velocidad}×`;
}

function play() {
  if (estado.lapFloat >= estado.numVueltas - 1) estado.lapFloat = 0;
  estado.playing = true;
  $('btn-play').textContent = '⏸';
}
function pausar() {
  estado.playing = false;
  $('btn-play').textContent = '▶';
}
function reiniciar() {
  estado.lapFloat = 0;
  estado.ultimoIdxPanel = -1;
  $('scrubber').value = '0';
  pausar();
}
