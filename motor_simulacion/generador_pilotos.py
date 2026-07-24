"""
Fábrica de pilotos ("regens").

Cimientos del sistema generacional: genera pilotos nuevos con atributos
realistas aleatorios. Más adelante (módulo de temporadas) esto se usará para
que, a medida que pasa el tiempo y los veteranos se retiran, entren caras
nuevas a la parrilla.

Los jóvenes (regens) entran con `potencial` por encima de su `habilidad`
actual, para que puedan desarrollarse temporada a temporada.
"""

from __future__ import annotations

import random

from .modelos.piloto import Piloto

# Banco de nombres para generar regens. Amplíalo libremente.
_NOMBRES = [
    "Lucas", "Mateo", "Hugo", "Leo", "Bruno", "Nico", "Marco", "Diego",
    "Iván", "Pablo", "Álex", "Adrián", "Sergio", "Gael", "Enzo", "Théo",
    "Liam", "Noah", "Kai", "Milan", "Aaron", "Dani", "Rubén", "Óscar",
]
_APELLIDOS = [
    "García", "Rossi", "Verstappen", "Silva", "Costa", "Fernández", "Müller",
    "Dubois", "Rodríguez", "Bianchi", "Nakamura", "Andersson", "Kovač",
    "Moreau", "Santos", "Weber", "Romano", "Ferreira", "López", "Novak",
]


def _muestra_truncada(rng: random.Random, media: float, sigma: float,
                      minimo: float = 1.0, maximo: float = 99.0) -> float:
    """Muestra normal recortada al rango [minimo, maximo]."""
    valor = rng.gauss(media, sigma)
    return max(minimo, min(maximo, valor))


def generar_piloto(
    rng: random.Random | None = None,
    *,
    media_habilidad: float = 65.0,
    sigma_habilidad: float = 12.0,
    es_joven: bool = True,
) -> Piloto:
    """
    Genera un piloto aleatorio.

    media_habilidad / sigma_habilidad controlan el nivel de la generación
    (sube la media para una "hornada" fuerte). Si es_joven, el piloto es
    joven y con potencial por encima de su habilidad actual (un regen típico).
    """
    rng = rng or random.Random()

    nombre = f"{rng.choice(_NOMBRES)} {rng.choice(_APELLIDOS)}"
    habilidad = _muestra_truncada(rng, media_habilidad, sigma_habilidad)
    consistencia = _muestra_truncada(rng, 65.0, 15.0)
    agresividad = _muestra_truncada(rng, 55.0, 18.0)
    adaptacion_lluvia = _muestra_truncada(rng, 60.0, 18.0)
    gestion_neumaticos = _muestra_truncada(rng, 60.0, 15.0)

    if es_joven:
        edad = rng.randint(18, 23)
        # El potencial supera la habilidad actual: margen de desarrollo.
        potencial = min(99.0, habilidad + _muestra_truncada(rng, 12.0, 6.0, 0.0, 30.0))
    else:
        edad = rng.randint(24, 36)
        potencial = habilidad

    return Piloto(
        nombre=nombre,
        habilidad=habilidad,
        consistencia=consistencia,
        agresividad=agresividad,
        adaptacion_lluvia=adaptacion_lluvia,
        gestion_neumaticos=gestion_neumaticos,
        edad=edad,
        potencial=potencial,
    )


def generar_parrilla(
    n: int,
    rng: random.Random | None = None,
    **kwargs,
) -> list[Piloto]:
    """Genera una lista de n pilotos regen."""
    rng = rng or random.Random()
    return [generar_piloto(rng, **kwargs) for _ in range(n)]
