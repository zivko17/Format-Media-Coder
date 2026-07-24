"""
Orquestador de la carrera vuelta a vuelta (Fase 1).

Construye la parrilla a partir de participantes (piloto + coche + neumático),
simula N vueltas acumulando tiempos y genera una clasificación vuelta a vuelta
lista para serializar a JSON en la Fase 3.

En Fase 1 no hay paradas, incidentes ni safety car: el objetivo es validar que
el modelo de ritmo produce variabilidad realista (el mejor gana más a menudo,
pero no siempre).
"""

from __future__ import annotations

import random
from dataclasses import dataclass, field
from typing import Optional

from . import config, simulacion_vuelta
from .modelos.piloto import Piloto
from .modelos.coche import Coche
from .modelos.neumaticos import Neumatico
from .modelos.clima import Clima


@dataclass
class Participante:
    """Un binomio piloto + coche en la carrera, con su estado de neumáticos."""

    piloto: Piloto
    coche: Coche
    neumatico: Neumatico = field(default_factory=Neumatico)
    tiempo_total: float = 0.0
    vueltas_stint: int = 0   # vueltas rodadas con el juego actual de neumáticos


@dataclass
class Carrera:
    participantes: list[Participante]
    num_vueltas: int
    clima: Clima = field(default_factory=Clima)
    semilla: Optional[int] = None

    def __post_init__(self) -> None:
        # rng propio de la carrera: fijando la semilla, la carrera es reproducible.
        self._rng = random.Random(self.semilla)

    def simular(self) -> dict:
        """
        Ejecuta la carrera completa.

        Devuelve un dict con la estructura (pensada para el JSON de la API):
            {
              "num_vueltas": int,
              "vueltas": [ [ {id_piloto, nombre, escuderia, posicion,
                              tiempo_vuelta, tiempo_total}, ... ], ... ],
              "clasificacion_final": [ {posicion, id_piloto, nombre, ...}, ... ]
            }
        """
        vueltas_registro: list[list[dict]] = []

        for vuelta in range(1, self.num_vueltas + 1):
            vueltas_restantes = self.num_vueltas - vuelta

            for p in self.participantes:
                p.vueltas_stint += 1
                tiempo_vuelta = simulacion_vuelta.calcular_tiempo_vuelta(
                    piloto=p.piloto,
                    coche=p.coche,
                    neumatico=p.neumatico,
                    vueltas_stint=p.vueltas_stint,
                    vueltas_restantes=vueltas_restantes,
                    clima=self.clima,
                    rng=self._rng,
                )
                p.tiempo_total += tiempo_vuelta
                p._ultimo_tiempo = tiempo_vuelta  # type: ignore[attr-defined]

            vueltas_registro.append(self._clasificacion_actual(vuelta))

        return {
            "num_vueltas": self.num_vueltas,
            "clima": self.clima.condicion,
            "vueltas": vueltas_registro,
            "clasificacion_final": [
                {k: v for k, v in fila.items() if k != "vuelta"}
                for fila in vueltas_registro[-1]
            ],
        }

    def _clasificacion_actual(self, vuelta: int) -> list[dict]:
        """Ordena por tiempo total acumulado (menor = líder) y numera posiciones."""
        ordenados = sorted(self.participantes, key=lambda p: p.tiempo_total)
        clasificacion = []
        for posicion, p in enumerate(ordenados, start=1):
            clasificacion.append(
                {
                    "vuelta": vuelta,
                    "posicion": posicion,
                    "id_piloto": p.piloto.id_piloto,
                    "nombre": p.piloto.nombre,
                    "escuderia": p.coche.escuderia,
                    "tiempo_vuelta": round(getattr(p, "_ultimo_tiempo", 0.0), 3),
                    "tiempo_total": round(p.tiempo_total, 3),
                }
            )
        return clasificacion


def crear_participantes(
    parrilla: list[tuple[Piloto, Coche]],
    compuesto: str = config.COMPUESTO_POR_DEFECTO,
) -> list[Participante]:
    """Helper: convierte una lista de (piloto, coche) en participantes."""
    return [
        Participante(piloto=pil, coche=coc, neumatico=Neumatico(compuesto))
        for pil, coc in parrilla
    ]
