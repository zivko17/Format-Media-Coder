"""
Modelo de tiempo de vuelta.

Combina los términos de la fórmula acordada:

    tiempo_vuelta = tiempo_base_categoria
                  + penalizacion_ritmo      (habilidad + rendimiento del coche)
                  + degradacion_neumaticos   (no lineal según vueltas de stint)
                  + efecto_combustible        (decrece según avanza la carrera)
                  + penalizacion_grip         (clima: seco/lluvia)
                  + ruido_gaussiano           (sigma depende de la consistencia)

Los parámetros de ajuste (tiempo base, rango de ritmo, sigma, combustible,
pesos piloto/coche) vienen de la `Categoria`, de modo que el MISMO motor sirve
para F1, IndyCar, WEC, GT3, etc. La función es pura respecto al azar: recibe un
`random.Random` (rng) para que la validación Monte Carlo sea reproducible.
"""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

if TYPE_CHECKING:  # solo para type hints, evita imports en tiempo de ejecución
    from .modelos.piloto import Piloto
    from .modelos.coche import Coche
    from .modelos.neumaticos import Neumatico
    from .modelos.clima import Clima
    from .categorias.categoria import Categoria


def penalizacion_ritmo(piloto: "Piloto", coche: "Coche", categoria: "Categoria") -> float:
    """Convierte habilidad + rendimiento (0-100) en segundos de penalización."""
    ritmo = categoria.peso_piloto * piloto.habilidad + categoria.peso_coche * coche.rendimiento_base
    return (100.0 - ritmo) / 100.0 * categoria.rango_pace


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


def sigma_ruido(piloto: "Piloto", categoria: "Categoria", clima: "Clima") -> float:
    """
    Desviación típica del ruido por vuelta.

    Depende de la consistencia del piloto y, en mojado, aumenta según lo mal
    que se adapte a la lluvia (adaptacion_lluvia baja -> más varianza).
    """
    base = categoria.sigma_max * (1.0 - piloto.consistencia / 100.0)
    return base * clima.multiplicador_error(piloto)


def efecto_combustible(vueltas_restantes: int, categoria: "Categoria") -> float:
    """Penalización por peso de combustible. Decrece hasta 0 al final."""
    return categoria.ganancia_combustible * vueltas_restantes


def calcular_tiempo_vuelta(
    piloto: "Piloto",
    coche: "Coche",
    neumatico: "Neumatico",
    vueltas_stint: int,
    vueltas_restantes: int,
    clima: "Clima",
    categoria: "Categoria",
    rng: random.Random,
) -> float:
    """
    Devuelve el tiempo (s) de una vuelta concreta para un piloto.

    vueltas_stint: nº de vueltas ya rodadas con el juego actual de neumáticos.
    vueltas_restantes: vueltas que quedan por delante (para el combustible).
    """
    factor = factor_cuidado_neumatico(piloto, coche)

    tiempo = (
        categoria.tiempo_base
        + penalizacion_ritmo(piloto, coche, categoria)
        + neumatico.offset_ritmo
        + neumatico.degradacion(vueltas_stint, factor)
        + efecto_combustible(vueltas_restantes, categoria)
        + clima.penalizacion_grip(piloto)
        + rng.gauss(0.0, sigma_ruido(piloto, categoria, clima))
    )
    return tiempo
