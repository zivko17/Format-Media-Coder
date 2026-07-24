"""
Esquemas Pydantic de entrada/salida de la API.

Definen y validan la configuración de carrera que llega por POST /simular.
Todos los atributos numéricos de piloto/coche están acotados a 0-100.
"""

from __future__ import annotations

from typing import Optional
from pydantic import BaseModel, Field


class PilotoIn(BaseModel):
    nombre: str
    habilidad: float = Field(ge=0, le=100)
    consistencia: float = Field(ge=0, le=100)
    agresividad: float = Field(default=50, ge=0, le=100)
    adaptacion_lluvia: float = Field(default=50, ge=0, le=100)
    gestion_neumaticos: float = Field(default=50, ge=0, le=100)
    edad: int = Field(default=25, ge=15, le=60)
    potencial: float = Field(default=0, ge=0, le=100)


class CocheIn(BaseModel):
    escuderia: str
    rendimiento_base: float = Field(ge=0, le=100)
    fiabilidad: float = Field(default=90, ge=0, le=100)
    eficiencia_aero: float = Field(default=50, ge=0, le=100)
    desgaste_neumatico: float = Field(default=50, ge=0, le=100)


class ParticipanteIn(BaseModel):
    piloto: PilotoIn
    coche: CocheIn


class ConfiguracionCarrera(BaseModel):
    """Configuración de una carrera a simular."""

    categoria: str = Field(
        default="generica",
        description="id de categoría del catálogo (o 'generica').",
    )
    num_vueltas: Optional[int] = Field(
        default=None, ge=1, le=500,
        description="Nº de vueltas. Si se omite, usa las típicas de la categoría.",
    )
    clima: str = Field(default="seco", pattern="^(seco|lluvia)$")
    intensidad_lluvia: Optional[float] = Field(default=None, ge=0, le=1)
    semilla: Optional[int] = Field(
        default=None, description="Semilla para reproducibilidad."
    )
    parrilla: Optional[list[ParticipanteIn]] = Field(
        default=None,
        description="Parrilla explícita. Si se omite, usa la parrilla de ejemplo.",
    )


class CategoriaOut(BaseModel):
    id: str
    nombre: str
    familia: str
    nivel: int
    tipo_trazado: str
    formato: str
    num_coches: int
    vueltas_tipicas: int
    es_monomarca: bool
    asciende_a: Optional[str]
