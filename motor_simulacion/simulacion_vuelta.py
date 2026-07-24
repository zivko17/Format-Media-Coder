"""
Modelo de tiempo de vuelta.

Combina los términos de la fórmula acordada:

    tiempo_vuelta = tiempo_base_circuito
                  + penalizacion_ritmo      (habilidad + rendimiento del coche)
                  + degradacion_neumaticos   (no lineal según vueltas de stint)
                  + efecto_combustible        (decrece según avanza la carrera)
                  + penalizacion_grip         (clima; Fase 1 = 0)
                  + ruido_gaussiano           (sigma depende de la consistencia)

La función es pura respecto al azar: recibe un `random.Random` (rng) para que
la validación Monte Carlo sea reproducible fijando la semilla.
"""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from . import config

if TYPE_CHECKING:  # solo para type hints, evita imports en tiempo de ejecución
    from .modelos.piloto import Piloto
    from .modelos.coche import Coche
    from .modelos.neumaticos import Neumatico
    from .modelos.clima import Clima


def penalizacion_ritmo(piloto: "Piloto", coche: "Coche") -> float:
    """Convierte habilidad + rendimiento (0-100) en segundos de penalización."""
    ritmo = config.PESO_PILOTO * piloto.habilidad + config.PESO_COCHE * coche.rendimiento_base
    return (100.0 - ritmo) / 100.0 * config.RANGO_PACE


def factor_cuidado_neumatico(piloto: "Piloto", coche: "Coche") -> float:
    """
    Factor multiplicador de la degradación.

    < 1  -> piloto/coche cuidan la goma (menos degradación)
    > 1  -> la castigan (más degradación)

    Se centra en 1.0 cuando ambos valen 50. gestion_neumaticos alta reduce,
    desgaste_neumatico alto aumenta.
    """
    factor_piloto = 1.0 - (piloto.gestion_neumaticos - 50.0) / 200.0   # ~0.75..1.25
    factor_coche = 1.0 + (coche.desgaste_neumatico - 50.0) / 200.0     # ~0.75..1.25
    return factor_piloto * factor_coche


def sigma_ruido(piloto: "Piloto") -> float:
    """Desviación típica del ruido por vuelta según la consistencia del piloto."""
    return config.SIGMA_MAX * (1.0 - piloto.consistencia / 100.0)


def efecto_combustible(vueltas_restantes: int) -> float:
    """Penalización por peso de combustible. Decrece hasta 0 al final."""
    return config.GANANCIA_COMBUSTIBLE * vueltas_restantes


def calcular_tiempo_vuelta(
    piloto: "Piloto",
    coche: "Coche",
    neumatico: "Neumatico",
    vueltas_stint: int,
    vueltas_restantes: int,
    clima: "Clima",
    rng: random.Random,
) -> float:
    """
    Devuelve el tiempo (s) de una vuelta concreta para un piloto.

    vueltas_stint: nº de vueltas ya rodadas con el juego actual de neumáticos.
    vueltas_restantes: vueltas que quedan por delante (para el combustible).
    """
    factor = factor_cuidado_neumatico(piloto, coche)

    tiempo = (
        config.TIEMPO_BASE_CIRCUITO
        + penalizacion_ritmo(piloto, coche)
        + neumatico.offset_ritmo
        + neumatico.degradacion(vueltas_stint, factor)
        + efecto_combustible(vueltas_restantes)
        + clima.penalizacion_grip()
        + rng.gauss(0.0, sigma_ruido(piloto))
    )
    return tiempo
