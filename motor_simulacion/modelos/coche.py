"""
Modelo de Coche.

En Fase 1 se usa de forma activa `rendimiento_base` y `desgaste_neumatico`.
`fiabilidad` y `eficiencia_aero` quedan reservados para Fase 2 (DNFs y
adelantamientos con DRS/rebufo).
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass
class Coche:
    escuderia: str
    rendimiento_base: float       # pace del monoplaza (0-100)
    fiabilidad: float = 90.0      # robustez mecánica; prob. de DNF (Fase 2)
    eficiencia_aero: float = 50.0 # efecto DRS/rebufo en adelantamientos (Fase 2)
    desgaste_neumatico: float = 50.0  # cuánto castiga las gomas; modula degradación

    def __str__(self) -> str:
        return f"{self.escuderia} (REN {self.rendimiento_base:.0f})"
