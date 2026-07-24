"""
Catálogo de categorías (data-driven).

El automovilismo no es una sola pirámide, sino varias "familias" (disciplinas)
con su propia escalera de niveles. Aquí se definen las categorías y, mediante
`asciende_a`, la escalera de ascensos que usará el modo carrera.

Añadir una categoría = añadir una entrada aquí. El motor no cambia.

Valores de tiempo/vueltas son aproximados y 100% calibrables; el objetivo es
que cada familia "se sienta" distinta (óvalos rápidos, resistencia larga,
monomarcas donde manda el piloto, etc.).
"""

from __future__ import annotations

from .categoria import Categoria

# Familias / disciplinas
FAMILIAS = {
    "monoplaza_fia": "Monoplaza FIA (escalera europea hacia F1)",
    "monoplaza_us": "Monoplaza USA (escalera hacia IndyCar)",
    "electrico": "Monoplaza eléctrico",
    "resistencia": "Resistencia / Sport prototipos y GT",
    "turismos": "Turismos",
    "stock_us": "Stock car USA",
}

# Neumáticos "de resistencia": degradan poco, pensados para stints largos.
_COMP_RESISTENCIA = {
    "medio": {"offset": 0.0, "deg_lineal": 0.020, "deg_cuad": 0.0010},
    "duro":  {"offset": 0.3, "deg_lineal": 0.012, "deg_cuad": 0.0006},
    "lluvia": {"offset": 2.0, "deg_lineal": 0.015, "deg_cuad": 0.0008},
}

# Lista de categorías. Ordenadas por familia y nivel (1 = cumbre).
_CATEGORIAS = [
    # --- Escalera monoplaza FIA -> F1 -------------------------------------
    Categoria(id="f1", nombre="Fórmula 1", familia="monoplaza_fia", nivel=1,
              tipo_trazado="circuito", formato="gp", num_coches=20,
              tiempo_base=90.0, vueltas_tipicas=57, rango_pace=2.6),
    Categoria(id="f2", nombre="Fórmula 2", familia="monoplaza_fia", nivel=2,
              tipo_trazado="circuito", formato="sprint", num_coches=22,
              tiempo_base=98.0, vueltas_tipicas=40, es_monomarca=True,
              asciende_a="f1"),
    Categoria(id="f3", nombre="Fórmula 3", familia="monoplaza_fia", nivel=3,
              tipo_trazado="circuito", formato="sprint", num_coches=30,
              tiempo_base=104.0, vueltas_tipicas=24, es_monomarca=True,
              asciende_a="f2"),
    Categoria(id="f4", nombre="Fórmula 4", familia="monoplaza_fia", nivel=4,
              tipo_trazado="circuito", formato="sprint", num_coches=28,
              tiempo_base=112.0, vueltas_tipicas=18, es_monomarca=True,
              asciende_a="f3"),
    Categoria(id="karting", nombre="Karting", familia="monoplaza_fia", nivel=5,
              tipo_trazado="circuito", formato="sprint", num_coches=32,
              tiempo_base=55.0, vueltas_tipicas=20, es_monomarca=True,
              rango_pace=3.5, asciende_a="f4"),

    # --- Escalera monoplaza USA -> IndyCar --------------------------------
    Categoria(id="indycar", nombre="IndyCar", familia="monoplaza_us", nivel=1,
              tipo_trazado="mixto", formato="gp", num_coches=27,
              tiempo_base=64.0, vueltas_tipicas=85, rango_pace=2.2),
    Categoria(id="indy_lights", nombre="Indy NXT (Indy Lights)",
              familia="monoplaza_us", nivel=2, tipo_trazado="mixto",
              formato="sprint", num_coches=18, tiempo_base=70.0,
              vueltas_tipicas=45, es_monomarca=True, asciende_a="indycar"),

    # --- Eléctrico --------------------------------------------------------
    Categoria(id="formula_e", nombre="Fórmula E", familia="electrico", nivel=1,
              tipo_trazado="calle", formato="sprint", num_coches=22,
              tiempo_base=80.0, vueltas_tipicas=40, es_monomarca=True,
              rango_pace=1.8),

    # --- Resistencia / prototipos y GT ------------------------------------
    Categoria(id="wec", nombre="WEC (Resistencia Mundial)", familia="resistencia",
              nivel=1, tipo_trazado="circuito", formato="resistencia",
              num_coches=36, tiempo_base=100.0, vueltas_tipicas=180,
              rango_pace=3.5, compuestos=dict(_COMP_RESISTENCIA),
              ganancia_combustible=0.05),
    Categoria(id="imsa", nombre="IMSA SportsCar", familia="resistencia", nivel=1,
              tipo_trazado="circuito", formato="resistencia", num_coches=40,
              tiempo_base=98.0, vueltas_tipicas=160, rango_pace=3.5,
              compuestos=dict(_COMP_RESISTENCIA), ganancia_combustible=0.05),
    Categoria(id="gt3", nombre="GT World (GT3)", familia="resistencia", nivel=2,
              tipo_trazado="circuito", formato="resistencia", num_coches=30,
              tiempo_base=108.0, vueltas_tipicas=60, rango_pace=3.0,
              compuestos=dict(_COMP_RESISTENCIA), asciende_a="wec"),

    # --- Turismos ---------------------------------------------------------
    Categoria(id="dtm", nombre="DTM", familia="turismos", nivel=1,
              tipo_trazado="circuito", formato="sprint", num_coches=24,
              tiempo_base=100.0, vueltas_tipicas=38, rango_pace=2.4),
    Categoria(id="wtcc", nombre="WTCR (Turismos)", familia="turismos", nivel=2,
              tipo_trazado="calle", formato="sprint", num_coches=26,
              tiempo_base=110.0, vueltas_tipicas=15, rango_pace=2.8,
              asciende_a="dtm"),

    # --- Stock car USA ----------------------------------------------------
    Categoria(id="nascar_cup", nombre="NASCAR Cup", familia="stock_us", nivel=1,
              tipo_trazado="ovalo", formato="gp", num_coches=40,
              tiempo_base=30.0, vueltas_tipicas=200, rango_pace=1.6),
    Categoria(id="nascar_xfinity", nombre="NASCAR Xfinity", familia="stock_us",
              nivel=2, tipo_trazado="ovalo", formato="gp", num_coches=38,
              tiempo_base=32.0, vueltas_tipicas=150, rango_pace=1.8,
              asciende_a="nascar_cup"),
    Categoria(id="nascar_trucks", nombre="NASCAR Trucks", familia="stock_us",
              nivel=3, tipo_trazado="ovalo", formato="gp", num_coches=36,
              tiempo_base=34.0, vueltas_tipicas=130, rango_pace=2.0,
              asciende_a="nascar_xfinity"),
]

# Índice por id para acceso rápido.
CATALOGO: dict[str, Categoria] = {c.id: c for c in _CATEGORIAS}


def obtener(id_categoria: str) -> Categoria:
    """Devuelve la categoría por id (lanza KeyError si no existe)."""
    return CATALOGO[id_categoria]


def por_familia(familia: str) -> list[Categoria]:
    """Categorías de una familia, ordenadas de la inferior a la cumbre."""
    cats = [c for c in CATALOGO.values() if c.familia == familia]
    return sorted(cats, key=lambda c: -c.nivel)


def escalera(id_categoria: str) -> list[Categoria]:
    """
    Cadena de ascensos desde una categoría hasta la cumbre de su rama.
    Ej: escalera('karting') -> [karting, f4, f3, f2, f1].
    """
    cadena = [obtener(id_categoria)]
    while cadena[-1].asciende_a:
        cadena.append(obtener(cadena[-1].asciende_a))
    return cadena


def todas() -> list[Categoria]:
    """Todas las categorías, ordenadas por familia y nivel."""
    return sorted(CATALOGO.values(), key=lambda c: (c.familia, c.nivel))
