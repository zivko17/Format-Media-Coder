"""
Parrilla de ejemplo (data-driven).

Para añadir/quitar pilotos basta con editar la lista PARRILLA: cada entrada
es un dict con los atributos del piloto y del coche. El motor no cambia.

Los coches están ordenados de más a menos rendimiento para que puedas ver
claramente en la validación Monte Carlo que "el mejor gana más a menudo".
"""

from __future__ import annotations

from ..modelos.piloto import Piloto
from ..modelos.coche import Coche

# Cada fila: (piloto, coche). Edita libremente o genera regens con la fábrica.
# 5 escuderías x 2 coches. Los compañeros de equipo comparten coche y tienen
# ritmo parecido, así que se pelean entre ellos (variabilidad realista); las
# escuderías sí están escalonadas, de modo que el equipo puntero gana más.
PARRILLA = [
    {
        "piloto": {"nombre": "A. Puntero", "habilidad": 89, "consistencia": 80,
                   "gestion_neumaticos": 76},
        "coche": {"escuderia": "Escudería Alfa", "rendimiento_base": 88,
                  "desgaste_neumatico": 43},
    },
    {
        "piloto": {"nombre": "B. Retador", "habilidad": 88, "consistencia": 78,
                   "gestion_neumaticos": 74},
        "coche": {"escuderia": "Escudería Alfa", "rendimiento_base": 88,
                  "desgaste_neumatico": 43},
    },
    {
        "piloto": {"nombre": "C. Veloz", "habilidad": 86, "consistencia": 78,
                   "gestion_neumaticos": 68},
        "coche": {"escuderia": "Escudería Beta", "rendimiento_base": 85,
                  "desgaste_neumatico": 48},
    },
    {
        "piloto": {"nombre": "D. Sólido", "habilidad": 83, "consistencia": 82,
                   "gestion_neumaticos": 70},
        "coche": {"escuderia": "Escudería Beta", "rendimiento_base": 85,
                  "desgaste_neumatico": 50},
    },
    {
        "piloto": {"nombre": "E. Regular", "habilidad": 83, "consistencia": 74,
                   "gestion_neumaticos": 60},
        "coche": {"escuderia": "Escudería Gamma", "rendimiento_base": 82,
                  "desgaste_neumatico": 52},
    },
    {
        "piloto": {"nombre": "F. Inconstante", "habilidad": 80, "consistencia": 58,
                   "gestion_neumaticos": 55},
        "coche": {"escuderia": "Escudería Gamma", "rendimiento_base": 82,
                  "desgaste_neumatico": 54},
    },
    {
        "piloto": {"nombre": "G. Promesa", "habilidad": 79, "consistencia": 70,
                   "gestion_neumaticos": 62},
        "coche": {"escuderia": "Escudería Delta", "rendimiento_base": 79,
                  "desgaste_neumatico": 58},
    },
    {
        "piloto": {"nombre": "H. Medio", "habilidad": 76, "consistencia": 72,
                   "gestion_neumaticos": 56},
        "coche": {"escuderia": "Escudería Delta", "rendimiento_base": 79,
                  "desgaste_neumatico": 60},
    },
    {
        "piloto": {"nombre": "I. Novato", "habilidad": 75, "consistencia": 62,
                   "gestion_neumaticos": 50},
        "coche": {"escuderia": "Escudería Épsilon", "rendimiento_base": 76,
                  "desgaste_neumatico": 64},
    },
    {
        "piloto": {"nombre": "J. Veterano", "habilidad": 74, "consistencia": 78,
                   "gestion_neumaticos": 58},
        "coche": {"escuderia": "Escudería Épsilon", "rendimiento_base": 76,
                  "desgaste_neumatico": 64},
    },
]


def cargar_parrilla_ejemplo() -> list[tuple[Piloto, Coche]]:
    """Construye la lista de (Piloto, Coche) a partir de los datos PARRILLA."""
    parrilla = []
    for fila in PARRILLA:
        piloto = Piloto(**fila["piloto"])
        coche = Coche(**fila["coche"])
        parrilla.append((piloto, coche))
    return parrilla
