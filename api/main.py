"""
API FastAPI del simulador de carreras (Fase 3).

Endpoints:
- GET  /health      -> comprobación de vida del servidor.
- GET  /categorias  -> catálogo de categorías (para poblar el frontend).
- POST /simular     -> simula una carrera y devuelve el resultado vuelta a vuelta.

Arranque:
    uvicorn api.main:app --reload
Docs interactivas (Swagger): http://127.0.0.1:8000/docs
"""

from __future__ import annotations

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from motor_simulacion.carrera import Carrera, crear_participantes
from motor_simulacion.modelos.piloto import Piloto
from motor_simulacion.modelos.coche import Coche
from motor_simulacion.modelos.clima import Clima
from motor_simulacion.categorias import Categoria, catalogo
from motor_simulacion.datos import cargar_parrilla_ejemplo

from .esquemas import ConfiguracionCarrera, CategoriaOut

app = FastAPI(
    title="Simulador de Carreras",
    description="Motor de simulación de automovilismo multi-categoría.",
    version="0.3.0",
)

# CORS abierto para desarrollo (el frontend se sirve desde otro origen).
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health")
def health() -> dict:
    """Comprobación simple de que el servidor está vivo."""
    return {"estado": "ok", "categorias": len(catalogo.CATALOGO)}


@app.get("/categorias", response_model=list[CategoriaOut])
def listar_categorias() -> list[CategoriaOut]:
    """Devuelve el catálogo de categorías, ordenado por familia y nivel."""
    return [
        CategoriaOut(
            id=c.id, nombre=c.nombre, familia=c.familia, nivel=c.nivel,
            tipo_trazado=c.tipo_trazado, formato=c.formato,
            num_coches=c.num_coches, vueltas_tipicas=c.vueltas_tipicas,
            es_monomarca=c.es_monomarca, asciende_a=c.asciende_a,
        )
        for c in catalogo.todas()
    ]


def _resolver_categoria(id_categoria: str) -> Categoria:
    if id_categoria == "generica":
        return Categoria.generica()
    try:
        return catalogo.obtener(id_categoria)
    except KeyError:
        raise HTTPException(
            status_code=404,
            detail=f"Categoría desconocida: {id_categoria!r}. "
                   f"Consulta GET /categorias.",
        )


def _construir_parrilla(config: ConfiguracionCarrera):
    """Devuelve la lista de (Piloto, Coche) a partir de la configuración."""
    if not config.parrilla:
        return cargar_parrilla_ejemplo()
    parrilla = []
    for entrada in config.parrilla:
        piloto = Piloto(**entrada.piloto.model_dump())
        coche = Coche(**entrada.coche.model_dump())
        parrilla.append((piloto, coche))
    return parrilla


@app.post("/simular")
def simular(config: ConfiguracionCarrera) -> dict:
    """Simula una carrera completa y devuelve el resultado vuelta a vuelta."""
    categoria = _resolver_categoria(config.categoria)
    num_vueltas = config.num_vueltas or categoria.vueltas_tipicas

    parrilla = _construir_parrilla(config)
    if len(parrilla) < 2:
        raise HTTPException(status_code=422, detail="Se necesitan al menos 2 coches.")

    clima = Clima(condicion=config.clima,
                  intensidad=config.intensidad_lluvia or 0.0)

    participantes = crear_participantes(parrilla, categoria)
    carrera = Carrera(
        participantes, num_vueltas=num_vueltas, categoria=categoria,
        clima=clima, semilla=config.semilla,
    )
    return carrera.simular()
