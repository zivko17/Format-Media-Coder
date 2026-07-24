"""
Modelo de Piloto.

Atributos numéricos 0-100 (salvo edad). En Fase 1 se usan de forma activa
`habilidad`, `consistencia` y `gestion_neumaticos`; el resto quedan definidos
para que la interfaz no cambie cuando lleguen las Fases 2-3 (errores, clima,
adelantamientos) y el sistema de regens/temporadas.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import itertools

# Contador para asignar identificadores únicos a cada piloto creado.
_secuencia_id = itertools.count(1)


@dataclass
class Piloto:
    nombre: str
    habilidad: float          # ritmo puro (0-100)
    consistencia: float       # regularidad vuelta a vuelta; reduce el ruido (0-100)
    agresividad: float = 50.0        # propensión a arriesgar (Fase 2)
    adaptacion_lluvia: float = 50.0  # rendimiento en mojado (Fase 2)
    gestion_neumaticos: float = 50.0 # cuida el neumático; modula la degradación
    edad: int = 25                   # para desarrollo/retiro por temporadas (regens)
    potencial: float = 0.0           # techo de habilidad futura (regens)
    id_piloto: int = field(default_factory=lambda: next(_secuencia_id))

    def __post_init__(self) -> None:
        # Si no se especifica potencial, por defecto es su habilidad actual.
        if self.potencial <= 0.0:
            self.potencial = self.habilidad

    def __str__(self) -> str:
        return f"{self.nombre} (HAB {self.habilidad:.0f}, CON {self.consistencia:.0f})"
