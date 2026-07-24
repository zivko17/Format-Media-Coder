"""Categorías: el motor es agnóstico a la categoría; aquí viven las definiciones."""

from .categoria import Categoria
from . import catalogo
from .catalogo import CATALOGO, obtener, por_familia, escalera, todas, FAMILIAS

__all__ = [
    "Categoria",
    "catalogo",
    "CATALOGO",
    "obtener",
    "por_familia",
    "escalera",
    "todas",
    "FAMILIAS",
]
