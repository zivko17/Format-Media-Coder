"""
Motor de simulación de carreras.

Paquete agnóstico a la categoría: el mismo motor sirve para F1, IndyCar, WEC,
GTs, etc. cambiando únicamente configuración y datos (parrilla, circuito,
compuestos, reglas). Fase 1: modelo de ritmo y bucle de carrera.
"""

from . import config, simulacion_vuelta, carrera, generador_pilotos
from .modelos import Piloto, Coche, Neumatico, Clima
from .carrera import Carrera, Participante, crear_participantes

__all__ = [
    "config",
    "simulacion_vuelta",
    "carrera",
    "generador_pilotos",
    "Piloto",
    "Coche",
    "Neumatico",
    "Clima",
    "Carrera",
    "Participante",
    "crear_participantes",
]
