"""
Modelo de Categoría.

Una categoría (F1, IndyCar, WEC, GT3, DTM, Formula E, NASCAR, F2, F3...) NO es
código: es un conjunto de parámetros que el mismo motor usa para simular. Aquí
vive la definición; el catálogo concreto está en `catalogo.py`.

Campos clave para el "Football Manager del motor":
- `familia` y `nivel`: sitúan la categoría en la pirámide de disciplinas y
  permiten construir la escalera de ascensos/descensos del modo carrera.
- `es_monomarca`: en categorías de coche único (F2, F3, F4, Indy Lights,
  NASCAR...) el talento del piloto pesa más que el coche. Es donde el modo
  carrera "descubre" a los regens con potencial.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Optional

from .. import config


@dataclass
class Categoria:
    id: str
    nombre: str
    familia: str            # ver catalogo.FAMILIAS
    nivel: int              # 1 = cumbre; número mayor = categoría más inferior
    tipo_trazado: str       # "circuito" | "ovalo" | "calle" | "mixto"
    formato: str            # "sprint" | "gp" | "resistencia"
    num_coches: int
    tiempo_base: float                 # tiempo de vuelta de referencia (s)
    vueltas_tipicas: int = 40

    # --- Parámetros de simulación (heredan del config global si no se indican) ---
    rango_pace: float = config.RANGO_PACE
    sigma_max: float = config.SIGMA_MAX
    ganancia_combustible: float = config.GANANCIA_COMBUSTIBLE
    peso_piloto: float = config.PESO_PILOTO
    peso_coche: float = config.PESO_COCHE
    compuestos: dict = field(default_factory=lambda: dict(config.COMPUESTOS))
    compuesto_defecto: str = config.COMPUESTO_POR_DEFECTO

    # --- Reglas / metajuego ---
    es_monomarca: bool = False
    puntos: tuple = (25, 18, 15, 12, 10, 8, 6, 4, 2, 1)  # reparto por defecto (F1)
    asciende_a: Optional[str] = None   # id de la categoría superior en la escalera

    def __post_init__(self) -> None:
        # En categorías monomarca el coche es (casi) igual para todos, así que
        # el peso del piloto sube: es el talento lo que marca la diferencia.
        if self.es_monomarca and self.peso_piloto == config.PESO_PILOTO:
            self.peso_piloto = 0.70
            self.peso_coche = 0.30

    @classmethod
    def generica(cls) -> "Categoria":
        """Categoría por defecto equivalente al comportamiento de Fase 1."""
        return cls(
            id="generica",
            nombre="Categoría genérica",
            familia="generica",
            nivel=1,
            tipo_trazado="circuito",
            formato="gp",
            num_coches=10,
            tiempo_base=config.TIEMPO_BASE_CIRCUITO,
            vueltas_tipicas=30,
        )

    def __str__(self) -> str:
        return f"{self.nombre} (nivel {self.nivel}, {self.familia})"
