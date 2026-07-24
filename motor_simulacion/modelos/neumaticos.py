"""
Modelo de neumáticos: curvas de degradación no lineales por compuesto.

La degradación crece con las vueltas del stint. El término cuadrático genera
el "acantilado" (cliff) cuando el neumático se agota.

Cada categoría puede tener su propia tabla de compuestos (una fórmula de F1 no
degrada igual que un GT3 o un óvalo de NASCAR). Por eso el neumático recibe la
tabla `compuestos`; si no se indica, usa la del config global.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from .. import config


@dataclass
class Neumatico:
    compuesto: str = config.COMPUESTO_POR_DEFECTO
    compuestos: dict = field(default_factory=lambda: dict(config.COMPUESTOS))

    def __post_init__(self) -> None:
        if self.compuesto not in self.compuestos:
            raise ValueError(
                f"Compuesto desconocido: {self.compuesto!r}. "
                f"Opciones: {list(self.compuestos)}"
            )

    @property
    def _params(self) -> dict:
        return self.compuestos[self.compuesto]

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

    def nivel_desgaste(self, vueltas_stint: int, factor_cuidado: float = 1.0) -> float:
        """
        Desgaste normalizado 0-1 (aprox.) para que la estrategia decida paradas.
        1.0 ≈ neumático en el acantilado. Es una guía, no un límite físico.
        """
        # Referencia: ~2.5 s de degradación se considera "gastado".
        return min(1.0, self.degradacion(vueltas_stint, factor_cuidado) / 2.5)
