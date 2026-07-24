"""
Eventos de carrera: incidentes/errores, fallos mecánicos y safety car / VSC.

- Los incidentes se correlacionan con la agresividad del piloto y con el clima
  (la lluvia multiplica la probabilidad).
- Los fallos mecánicos dependen de la fiabilidad del coche.
- Un incidente medio o grave puede desplegar el coche de seguridad (SC) o un
  virtual safety car (VSC). El SC agrupa el pelotón (bunching) y neutraliza el
  ritmo durante unas vueltas; el VSC solo ralentiza sin agrupar.

`GestorEventos` mantiene el estado del safety car entre vueltas.
"""

from __future__ import annotations

import random
from dataclasses import dataclass
from typing import Optional, TYPE_CHECKING

from . import config

if TYPE_CHECKING:
    from .modelos.piloto import Piloto
    from .modelos.coche import Coche
    from .modelos.clima import Clima

# Tipos de incidente
LEVE = "leve"
MEDIO = "medio"
GRAVE = "grave"


@dataclass
class Incidente:
    tipo: str                    # LEVE | MEDIO | GRAVE
    perdida_tiempo: float        # segundos perdidos (0 si DNF)
    dnf: bool                    # ¿abandona?
    causa: str                   # "error" | "mecanico"


class GestorEventos:
    """Gestiona incidentes y el estado del safety car a lo largo de la carrera."""

    def __init__(self, clima: "Clima", rng: random.Random):
        self.clima = clima
        self.rng = rng
        # Estado del safety car / VSC
        self.tipo_neutralizacion: Optional[str] = None   # "sc" | "vsc" | None
        self.vueltas_neutralizacion: int = 0
        self.acaba_de_desplegar: bool = False            # True la vuelta que sale

    # --- Incidentes de pilotaje ---------------------------------------------
    def evaluar_incidente(self, piloto: "Piloto") -> Optional[Incidente]:
        """Devuelve un Incidente si el piloto comete un error esta vuelta."""
        prob = config.PROB_INCIDENTE_BASE * (
            1.0 + config.FACTOR_AGRESIVIDAD_INCIDENTE * (piloto.agresividad / 100.0)
        )
        prob *= self.clima.multiplicador_error(piloto)

        if self.rng.random() >= prob:
            return None

        # Ha ocurrido: determinar gravedad.
        t = self.rng.random()
        if t < config.PROB_INCIDENTE_LEVE:
            perdida = self.rng.uniform(*config.PERDIDA_INCIDENTE_LEVE)
            return Incidente(LEVE, perdida, dnf=False, causa="error")
        elif t < config.PROB_INCIDENTE_LEVE + config.PROB_INCIDENTE_MEDIO:
            perdida = self.rng.uniform(*config.PERDIDA_INCIDENTE_MEDIO)
            return Incidente(MEDIO, perdida, dnf=False, causa="error")
        else:
            return Incidente(GRAVE, 0.0, dnf=True, causa="error")

    # --- Fallos mecánicos ----------------------------------------------------
    def evaluar_fallo_mecanico(self, coche: "Coche") -> Optional[Incidente]:
        """Devuelve un Incidente(DNF) si el coche rompe esta vuelta."""
        prob = config.PROB_FALLO_MECANICO_MAX * (1.0 - coche.fiabilidad / 100.0)
        if self.rng.random() < prob:
            return Incidente(GRAVE, 0.0, dnf=True, causa="mecanico")
        return None

    # --- Safety car / VSC ----------------------------------------------------
    def quizas_desplegar_neutralizacion(self, incidente: Incidente) -> None:
        """Según la gravedad del incidente, quizá despliega SC o VSC."""
        if self.neutralizado:
            return  # ya hay uno activo
        if incidente.tipo == GRAVE:
            prob = config.PROB_SC_POR_INCIDENTE_GRAVE
        elif incidente.tipo == MEDIO:
            prob = config.PROB_SC_POR_INCIDENTE_MEDIO
        else:
            return  # los incidentes leves no sacan el SC

        if self.rng.random() < prob:
            if self.rng.random() < config.PROB_VSC_EN_LUGAR_DE_SC:
                self._desplegar("vsc", config.DURACION_VSC)
            else:
                self._desplegar("sc", config.DURACION_SC)

    def _desplegar(self, tipo: str, rango_duracion: tuple) -> None:
        self.tipo_neutralizacion = tipo
        self.vueltas_neutralizacion = self.rng.randint(*rango_duracion)
        self.acaba_de_desplegar = True

    def tick(self) -> None:
        """Avanza una vuelta el estado de neutralización. Llamar al final de cada vuelta."""
        self.acaba_de_desplegar = False
        if self.neutralizado:
            self.vueltas_neutralizacion -= 1
            if self.vueltas_neutralizacion <= 0:
                self.tipo_neutralizacion = None

    @property
    def neutralizado(self) -> bool:
        return self.tipo_neutralizacion is not None

    @property
    def hay_safety_car(self) -> bool:
        return self.tipo_neutralizacion == "sc"

    @property
    def factor_ritmo(self) -> float:
        """Multiplicador del tiempo de vuelta mientras hay neutralización."""
        if self.tipo_neutralizacion == "sc":
            return config.FACTOR_RITMO_SC
        if self.tipo_neutralizacion == "vsc":
            return config.FACTOR_RITMO_VSC
        return 1.0
