"""
Modelo de clima.

En Fase 1 solo existe "seco" y no afecta al tiempo (penalización 0). La clase
queda lista para Fase 2, donde la lluvia añadirá penalización de grip y
aumentará la varianza de errores según la `adaptacion_lluvia` del piloto.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass
class Clima:
    condicion: str = "seco"   # "seco" | "lluvia" (lluvia se activa en Fase 2)

    @property
    def es_lluvia(self) -> bool:
        return self.condicion == "lluvia"

    def penalizacion_grip(self) -> float:
        """Segundos extra por falta de grip. Fase 1: siempre 0 (seco)."""
        return 0.0
