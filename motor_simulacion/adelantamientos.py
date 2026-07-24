"""
Modelo probabilístico de adelantamientos.

Cuando un coche alcanza al de delante (gap por debajo de UMBRAL_ATAQUE), se
resuelve un intento de adelantamiento cuyo resultado depende de:

- La ventaja de ritmo del atacante (más rápido -> más probable).
- La eficiencia aerodinámica del atacante (DRS/rebufo).
- La habilidad del defensor (defiende mejor).
- El tipo de trazado (adelantar en óvalo es fácil; en circuito urbano, difícil).

Además hay un riesgo de contacto que crece con la agresividad de ambos y con la
lluvia. Un contacto puede costar tiempo o provocar un abandono.
"""

from __future__ import annotations

import random
from dataclasses import dataclass
from typing import TYPE_CHECKING

from . import config

if TYPE_CHECKING:
    from .modelos.piloto import Piloto
    from .modelos.coche import Coche
    from .modelos.clima import Clima
    from .categorias.categoria import Categoria

# Resultados posibles de un intento
ADELANTA = "adelanta"
BLOQUEADO = "bloqueado"
CONTACTO = "contacto"


@dataclass
class ResultadoAdelantamiento:
    resultado: str                 # ADELANTA | BLOQUEADO | CONTACTO
    atacante_fuera: bool = False   # el atacante abandona por el contacto
    defensor_fuera: bool = False   # el defensor abandona por el contacto
    penalizacion_atacante: float = 0.0  # segundos perdidos por el atacante
    penalizacion_defensor: float = 0.0  # segundos perdidos por el defensor


def _prob_exito(
    atacante_pil: "Piloto", atacante_coc: "Coche",
    defensor_pil: "Piloto", ventaja_ritmo: float,
    categoria: "Categoria",
) -> float:
    """
    Probabilidad de completar el adelantamiento (0-1).

    ventaja_ritmo: cuánto más rápido es el atacante (s/vuelta aprox., >0).
    """
    dificultad = config.DIFICULTAD_TRAZADO.get(categoria.tipo_trazado, 1.0)

    # La ventaja de ritmo es el motor principal del éxito.
    base = ventaja_ritmo / (0.6 * dificultad)          # ~0..1+
    # Bonus por aerodinámica/DRS del atacante (0.5 = neutro).
    base += (atacante_coc.eficiencia_aero - 50.0) / 200.0
    # La habilidad del defensor resta.
    base -= (defensor_pil.habilidad - 50.0) / 250.0

    return max(0.02, min(0.95, base))


def _prob_contacto(atacante_pil: "Piloto", defensor_pil: "Piloto",
                   clima: "Clima") -> float:
    """Riesgo de contacto en el intento (0-1), según agresividad y clima."""
    agresividad = (atacante_pil.agresividad * 0.7 + defensor_pil.agresividad * 0.3)
    prob = config.PROB_CONTACTO_BASE * (agresividad / 50.0)
    return min(0.5, prob * clima.multiplicador_error(atacante_pil))


def intentar_adelantamiento(
    atacante_pil: "Piloto", atacante_coc: "Coche",
    defensor_pil: "Piloto", defensor_coc: "Coche",
    ventaja_ritmo: float,
    categoria: "Categoria",
    clima: "Clima",
    rng: random.Random,
) -> ResultadoAdelantamiento:
    """Resuelve un intento de adelantamiento y devuelve el resultado."""
    # 1) ¿Hay contacto?
    if rng.random() < _prob_contacto(atacante_pil, defensor_pil, clima):
        # Reparto de consecuencias del contacto. La mayoría son toques leves;
        # un abandono por contacto es poco frecuente (como en la realidad).
        tirada = rng.random()
        if tirada < 0.08:
            # Ambos fuera (accidente grave)
            return ResultadoAdelantamiento(CONTACTO, atacante_fuera=True,
                                           defensor_fuera=True)
        elif tirada < 0.25:
            # El atacante se lleva la peor parte (a veces abandona)
            return ResultadoAdelantamiento(CONTACTO, atacante_fuera=(rng.random() < 0.4),
                                           penalizacion_atacante=rng.uniform(5, 15),
                                           penalizacion_defensor=rng.uniform(1, 4))
        else:
            # Toque leve: ambos pierden algo de tiempo, el atacante no pasa
            return ResultadoAdelantamiento(CONTACTO,
                                           penalizacion_atacante=rng.uniform(2, 6),
                                           penalizacion_defensor=rng.uniform(1, 3))

    # 2) Sin contacto: ¿completa el adelantamiento?
    if rng.random() < _prob_exito(atacante_pil, atacante_coc, defensor_pil,
                                  ventaja_ritmo, categoria):
        # Adelanta; pequeño coste de tiempo por la maniobra.
        return ResultadoAdelantamiento(ADELANTA,
                                       penalizacion_atacante=rng.uniform(0.1, 0.4))
    # 3) Bloqueado: se queda detrás, pierde algo de tiempo en aire sucio.
    return ResultadoAdelantamiento(BLOQUEADO,
                                   penalizacion_atacante=rng.uniform(0.2, 0.6))
