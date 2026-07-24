"""
Modelo de clima.

- "seco": sin penalización.
- "lluvia": añade penalización de grip (más lento) y aumenta la varianza de
  errores. Ambas cosas se atenúan si el piloto tiene buena `adaptacion_lluvia`:
  un especialista de lluvia pierde menos tiempo y comete menos errores.

`intensidad` (0-1) permite modular desde llovizna hasta diluvio.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .piloto import Piloto

# Segundos de penalización de grip con lluvia máxima para un piloto medio.
_GRIP_LLUVIA_MAX = 3.5
# Multiplicador máximo de la varianza de error con lluvia máxima.
_ERROR_LLUVIA_MAX = 2.5


@dataclass
class Clima:
    condicion: str = "seco"      # "seco" | "lluvia"
    intensidad: float = 0.0      # 0-1; solo aplica en lluvia

    def __post_init__(self) -> None:
        if self.es_lluvia and self.intensidad <= 0.0:
            self.intensidad = 0.6   # lluvia "normal" por defecto

    @property
    def es_lluvia(self) -> bool:
        return self.condicion == "lluvia"

    def _factor_adaptacion(self, piloto: "Piloto | None") -> float:
        """1.0 para piloto medio; <1 si se adapta bien, >1 si mal."""
        if piloto is None:
            return 1.0
        # adaptacion 100 -> 0.6 ; adaptacion 0 -> 1.4
        return 1.4 - 0.8 * (piloto.adaptacion_lluvia / 100.0)

    def penalizacion_grip(self, piloto: "Piloto | None" = None) -> float:
        """Segundos extra por falta de grip. Seco = 0."""
        if not self.es_lluvia:
            return 0.0
        return _GRIP_LLUVIA_MAX * self.intensidad * self._factor_adaptacion(piloto)

    def multiplicador_error(self, piloto: "Piloto | None" = None) -> float:
        """Factor que multiplica la varianza/probabilidad de error. Seco = 1."""
        if not self.es_lluvia:
            return 1.0
        extra = (_ERROR_LLUVIA_MAX - 1.0) * self.intensidad * self._factor_adaptacion(piloto)
        return 1.0 + extra
