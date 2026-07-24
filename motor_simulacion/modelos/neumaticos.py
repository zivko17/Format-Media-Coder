"""
Modelo de neumáticos: curvas de degradación no lineales por compuesto.

La degradación crece con las vueltas del stint. El término cuadrático genera
el "acantilado" (cliff) cuando el neumático se agota. En Fase 1 no hay
paradas, así que cada coche rueda con un único compuesto toda la carrera.
"""

from __future__ import annotations

from dataclasses import dataclass

from .. import config


@dataclass
class Neumatico:
    compuesto: str = config.COMPUESTO_POR_DEFECTO

    def __post_init__(self) -> None:
        if self.compuesto not in config.COMPUESTOS:
            raise ValueError(
                f"Compuesto desconocido: {self.compuesto!r}. "
                f"Opciones: {list(config.COMPUESTOS)}"
            )

    @property
    def _params(self) -> dict:
        return config.COMPUESTOS[self.compuesto]

    @property
    def offset_ritmo(self) -> float:
        """Aporte fijo de ritmo del compuesto (s). Negativo = más rápido."""
        return self._params["offset"]

    def degradacion(self, vueltas_stint: int, factor_cuidado: float = 1.0) -> float:
        """
        Segundos de penalización acumulada por degradación.

        vueltas_stint: nº de vueltas rodadas con este juego de neumáticos.
        factor_cuidado: <1 si piloto/coche cuidan la goma, >1 si la castigan.
        """
        p = self._params
        deg = p["deg_lineal"] * vueltas_stint + p["deg_cuad"] * vueltas_stint ** 2
        return deg * factor_cuidado
