"""
Estrategia de paradas en boxes.

Diseñada para ser extensible: `EstrategiaBase` define la interfaz y puedes
crear tus propias estrategias (una parada, dos paradas, sobreparada,
"undercut" agresivo...). La estrategia por defecto,
`EstrategiaUmbralDegradacion`, decide parar cuando el neumático supera un
umbral de desgaste, con dos matices realistas:

- Oportunismo con safety car: si sale el SC y ya llevas suficientes vueltas,
  paras aunque no estés al límite (la parada "cuesta" mucho menos tiempo).
- No parar al final: cerca de meta no compensa perder el tiempo de la parada.

`decidir` devuelve el compuesto a montar (str) si hay que parar, o None.
"""

from __future__ import annotations

import random
from typing import Optional, TYPE_CHECKING

from . import config

if TYPE_CHECKING:
    from .modelos.neumaticos import Neumatico
    from .modelos.clima import Clima
    from .categorias.categoria import Categoria
    from .eventos import GestorEventos


class EstrategiaBase:
    """Interfaz de estrategia. Hereda y sobreescribe `decidir`."""

    def decidir(self, *, neumatico: "Neumatico", vueltas_stint: int,
                factor_cuidado: float, vuelta: int, num_vueltas: int,
                posicion: int, categoria: "Categoria", clima: "Clima",
                gestor: "GestorEventos", rng: random.Random) -> Optional[str]:
        raise NotImplementedError


class EstrategiaUmbralDegradacion(EstrategiaBase):
    """Estrategia por defecto: parar por umbral de degradación (+ oportunismo SC)."""

    def _elegir_compuesto(self, vueltas_restantes: int, num_vueltas: int,
                          categoria: "Categoria", clima: "Clima") -> str:
        disponibles = categoria.compuestos

        # En lluvia, montar neumático de lluvia si la categoría lo tiene.
        if clima.es_lluvia and "lluvia" in disponibles:
            return "lluvia"

        # En seco: elegir dureza según cuántas vueltas quedan.
        secos = [c for c in disponibles if c != "lluvia"]
        proporcion = vueltas_restantes / max(1, num_vueltas)
        if proporcion > 0.4:
            for pref in ("duro", "medio", "blando"):
                if pref in secos:
                    return pref
        else:
            for pref in ("blando", "medio", "duro"):
                if pref in secos:
                    return pref
        return secos[0] if secos else categoria.compuesto_defecto

    def decidir(self, *, neumatico, vueltas_stint, factor_cuidado, vuelta,
                num_vueltas, posicion, categoria, clima, gestor, rng):
        vueltas_restantes = num_vueltas - vuelta

        # Nunca parar con un stint demasiado corto.
        if vueltas_stint < config.STINT_MINIMO:
            return None
        # Cerca del final no compensa (salvo desgaste extremo).
        desgaste = neumatico.nivel_desgaste(vueltas_stint, factor_cuidado)
        if vueltas_restantes <= config.STINT_MINIMO and desgaste < 1.0:
            return None

        # Oportunismo: safety car recién desplegado y gomas ya usadas.
        if gestor.acaba_de_desplegar and gestor.hay_safety_car and desgaste > 0.4:
            return self._elegir_compuesto(vueltas_restantes, num_vueltas, categoria, clima)

        # Cambio de condiciones: si llueve y llevo neumático seco, parar.
        if clima.es_lluvia and neumatico.compuesto != "lluvia" \
                and "lluvia" in categoria.compuestos:
            return "lluvia"

        # Parada normal por degradación.
        if desgaste >= config.UMBRAL_DEGRADACION_PARADA:
            return self._elegir_compuesto(vueltas_restantes, num_vueltas, categoria, clima)

        return None
