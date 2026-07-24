"""
Motor de simulación de carreras.

Paquete agnóstico a la categoría: el mismo motor sirve para F1, IndyCar, WEC,
GTs, DTM, Formula E, NASCAR, etc. cambiando únicamente configuración y datos
(categoría, parrilla, circuito, compuestos, reglas).

Fase 1: modelo de ritmo y bucle de carrera.
Fase 2: eventos (incidentes, DNFs), safety car/VSC, estrategia y adelantamientos.
"""

from . import (
    config,
    simulacion_vuelta,
    carrera,
    generador_pilotos,
    eventos,
    estrategia,
    adelantamientos,
    categorias,
)
from .modelos import Piloto, Coche, Neumatico, Clima
from .categorias import Categoria, catalogo
from .carrera import Carrera, Participante, crear_participantes

__all__ = [
    "config",
    "simulacion_vuelta",
    "carrera",
    "generador_pilotos",
    "eventos",
    "estrategia",
    "adelantamientos",
    "categorias",
    "Piloto",
    "Coche",
    "Neumatico",
    "Clima",
    "Categoria",
    "catalogo",
    "Carrera",
    "Participante",
    "crear_participantes",
]
