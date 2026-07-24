"""
Orquestador de la carrera vuelta a vuelta (Fase 2).

Integra sobre el modelo de ritmo de la Fase 1:
- Orden en pista explícito (la posición no es solo "suma de tiempos": para
  ganar posiciones hay que adelantar de verdad).
- Incidentes, fallos mecánicos y DNFs.
- Safety car / VSC (neutralización + agrupamiento del pelotón).
- Estrategia de paradas en boxes.
- Adelantamientos probabilísticos con riesgo de contacto.

El resultado sigue siendo un dict serializable a JSON para la API (Fase 3),
ahora enriquecido con eventos por piloto y vuelta.
"""

from __future__ import annotations

import random
from dataclasses import dataclass, field
from typing import Optional

from . import config, simulacion_vuelta, adelantamientos
from .modelos.piloto import Piloto
from .modelos.coche import Coche
from .modelos.neumaticos import Neumatico
from .modelos.clima import Clima
from .categorias.categoria import Categoria
from .eventos import GestorEventos, Incidente, GRAVE
from .estrategia import EstrategiaBase, EstrategiaUmbralDegradacion

# Separación mínima (s) que se impone entre coches consecutivos al resolver
# el orden en pista, para mantener tiempos y posiciones coherentes.
_GAP_MINIMO = 0.3


@dataclass
class Participante:
    """Un binomio piloto + coche, con su estado durante la carrera."""

    piloto: Piloto
    coche: Coche
    neumatico: Neumatico = field(default_factory=Neumatico)
    tiempo_total: float = 0.0
    vueltas_stint: int = 0
    paradas: int = 0
    activo: bool = True
    vuelta_abandono: Optional[int] = None
    # estado transitorio de la vuelta en curso
    _ultimo_tiempo: float = 0.0
    _eventos_vuelta: list = field(default_factory=list)


class Carrera:
    def __init__(
        self,
        participantes: list[Participante],
        num_vueltas: int,
        categoria: Optional[Categoria] = None,
        clima: Optional[Clima] = None,
        estrategia: Optional[EstrategiaBase] = None,
        semilla: Optional[int] = None,
    ):
        self.participantes = participantes
        self.num_vueltas = num_vueltas
        self.categoria = categoria or Categoria.generica()
        self.clima = clima or Clima()
        self.estrategia = estrategia or EstrategiaUmbralDegradacion()
        self._rng = random.Random(semilla)
        self.gestor = GestorEventos(self.clima, self._rng)

        # Orden en pista (solo coches en carrera) y lista de abandonos.
        self._orden_pista: list[Participante] = list(participantes)
        self._retirados: list[Participante] = []
        self._parrilla_de_salida()

    # ------------------------------------------------------------------ API
    def simular(self) -> dict:
        vueltas_registro: list[list[dict]] = []
        for vuelta in range(1, self.num_vueltas + 1):
            self._simular_vuelta(vuelta)
            vueltas_registro.append(self._clasificacion(vuelta))
            self.gestor.tick()

        return {
            "categoria": self.categoria.id,
            "categoria_nombre": self.categoria.nombre,
            "num_vueltas": self.num_vueltas,
            "clima": self.clima.condicion,
            "vueltas": vueltas_registro,
            "clasificacion_final": [
                {k: v for k, v in fila.items() if k != "vuelta"}
                for fila in vueltas_registro[-1]
            ],
        }

    # -------------------------------------------------------------- interno
    def _parrilla_de_salida(self) -> None:
        """Qualy simple: ordena la parrilla por ritmo puro + un poco de azar."""
        def clave(p: Participante) -> float:
            pen = simulacion_vuelta.penalizacion_ritmo(p.piloto, p.coche, self.categoria)
            return pen + self._rng.gauss(0.0, 0.3)
        self._orden_pista.sort(key=clave)

    def _simular_vuelta(self, vuelta: int) -> None:
        vueltas_restantes = self.num_vueltas - vuelta

        # Reiniciar los eventos de la vuelta para TODOS (incluidos retirados,
        # para que no arrastren su evento de abandono vuelta tras vuelta).
        for p in self.participantes:
            p._eventos_vuelta = []

        for p in list(self._orden_pista):
            p.vueltas_stint += 1

            if self.gestor.neutralizado:
                self._vuelta_neutralizada(p)
            else:
                if self._vuelta_verde(p, vuelta):
                    continue  # ha abandonado; ya está retirado

            # Decisión de parada (también válida bajo safety car: es la ocasión).
            self._quizas_parar(p, vuelta, vueltas_restantes)

            p.tiempo_total += p._ultimo_tiempo

        # Agrupamiento del pelotón si acaba de salir el safety car.
        if self.gestor.acaba_de_desplegar and self.gestor.hay_safety_car:
            self._agrupar_peloton()

        # Adelantamientos solo en verde (no se adelanta bajo neutralización).
        if not self.gestor.neutralizado:
            self._resolver_adelantamientos(vuelta)

    def _vuelta_neutralizada(self, p: Participante) -> None:
        """Vuelta a ritmo de safety car / VSC (neutralizada)."""
        p._ultimo_tiempo = self.categoria.tiempo_base * self.gestor.factor_ritmo
        p._eventos_vuelta.append(self.gestor.tipo_neutralizacion)

    def _vuelta_verde(self, p: Participante, vuelta: int) -> bool:
        """
        Vuelta en verde con posibles incidentes. Devuelve True si abandona.
        """
        # Fallo mecánico
        fallo = self.gestor.evaluar_fallo_mecanico(p.coche)
        if fallo:
            self._abandonar(p, vuelta, "dnf_mecanico")
            self.gestor.quizas_desplegar_neutralizacion(fallo)
            return True

        # Error de pilotaje
        incidente = self.gestor.evaluar_incidente(p.piloto)

        factor = simulacion_vuelta.factor_cuidado_neumatico(p.piloto, p.coche)
        tiempo = simulacion_vuelta.calcular_tiempo_vuelta(
            piloto=p.piloto, coche=p.coche, neumatico=p.neumatico,
            vueltas_stint=p.vueltas_stint, vueltas_restantes=self.num_vueltas - vuelta,
            clima=self.clima, categoria=self.categoria, rng=self._rng,
        )

        if incidente:
            if incidente.dnf:
                self._abandonar(p, vuelta, "dnf_error")
                self.gestor.quizas_desplegar_neutralizacion(incidente)
                return True
            tiempo += incidente.perdida_tiempo
            p._eventos_vuelta.append(f"incidente_{incidente.tipo}")
            self.gestor.quizas_desplegar_neutralizacion(incidente)

        p._ultimo_tiempo = tiempo
        return False

    def _quizas_parar(self, p: Participante, vuelta: int, vueltas_restantes: int) -> None:
        if not p.activo:
            return
        factor = simulacion_vuelta.factor_cuidado_neumatico(p.piloto, p.coche)
        posicion = self._orden_pista.index(p) + 1
        nuevo = self.estrategia.decidir(
            neumatico=p.neumatico, vueltas_stint=p.vueltas_stint,
            factor_cuidado=factor, vuelta=vuelta, num_vueltas=self.num_vueltas,
            posicion=posicion, categoria=self.categoria, clima=self.clima,
            gestor=self.gestor, rng=self._rng,
        )
        if nuevo:
            coste = config.TIEMPO_PARADA_BASE
            if self.gestor.hay_safety_car:
                coste *= config.FACTOR_PARADA_BAJO_SC
            p._ultimo_tiempo += coste
            p.neumatico = Neumatico(nuevo, self.categoria.compuestos)
            p.vueltas_stint = 0
            p.paradas += 1
            p._eventos_vuelta.append("parada")

    def _abandonar(self, p: Participante, vuelta: int, causa: str) -> None:
        p.activo = False
        p.vuelta_abandono = vuelta
        p._ultimo_tiempo = 0.0
        p._eventos_vuelta.append(causa)
        if p in self._orden_pista:
            self._orden_pista.remove(p)
        self._retirados.append(p)

    def _agrupar_peloton(self) -> None:
        """El safety car agrupa a los coches: gaps mínimos, orden conservado."""
        if not self._orden_pista:
            return
        base = self._orden_pista[0].tiempo_total
        for i, p in enumerate(self._orden_pista):
            p.tiempo_total = base + i * config.GAP_BUNCHING_SC

    def _resolver_adelantamientos(self, vuelta: int) -> None:
        """Una pasada de atrás hacia delante: cada coche puede ganar 1 posición."""
        orden = self._orden_pista
        # La longitud no cambia durante la pasada: los abandonos por contacto se
        # marcan y se retiran de la lista al final, para no romper los índices.
        for i in range(len(orden) - 1, 0, -1):
            detras, delante = orden[i], orden[i - 1]
            if not (detras.activo and delante.activo):
                continue
            gap = detras.tiempo_total - delante.tiempo_total
            if gap >= config.UMBRAL_ATAQUE:
                continue  # aún no lo ha alcanzado

            ventaja = (
                simulacion_vuelta.penalizacion_ritmo(delante.piloto, delante.coche, self.categoria)
                - simulacion_vuelta.penalizacion_ritmo(detras.piloto, detras.coche, self.categoria)
            )
            # Si no es más rápido y sigue por detrás, no hay intento real.
            if ventaja <= 0 and gap >= 0:
                continue

            res = adelantamientos.intentar_adelantamiento(
                atacante_pil=detras.piloto, atacante_coc=detras.coche,
                defensor_pil=delante.piloto, defensor_coc=delante.coche,
                ventaja_ritmo=max(0.05, ventaja), categoria=self.categoria,
                clima=self.clima, rng=self._rng,
            )

            detras.tiempo_total += res.penalizacion_atacante
            delante.tiempo_total += res.penalizacion_defensor

            # DNFs por contacto (marcado; se retira de la pista tras la pasada)
            if res.atacante_fuera:
                self._marcar_dnf_contacto(detras, vuelta)
            if res.defensor_fuera:
                self._marcar_dnf_contacto(delante, vuelta)
            if res.atacante_fuera or res.defensor_fuera:
                # Un accidente puede sacar el coche de seguridad.
                self.gestor.quizas_desplegar_neutralizacion(
                    Incidente(GRAVE, 0.0, dnf=True, causa="contacto")
                )
                continue

            if res.resultado == adelantamientos.ADELANTA:
                orden[i - 1], orden[i] = detras, delante
                detras._eventos_vuelta.append("adelanta")
                # Coherencia tiempo/posición: el que adelanta queda por delante.
                detras.tiempo_total = min(detras.tiempo_total,
                                          delante.tiempo_total - _GAP_MINIMO)
            else:
                # Bloqueado: se queda detrás (pierde el tiempo del atasco).
                detras.tiempo_total = max(detras.tiempo_total,
                                          delante.tiempo_total + _GAP_MINIMO)

        # Retirar de la pista los que abandonaron por contacto en esta pasada.
        for p in list(orden):
            if not p.activo:
                orden.remove(p)

    def _marcar_dnf_contacto(self, p: Participante, vuelta: int) -> None:
        if not p.activo:
            return
        p.activo = False
        p.vuelta_abandono = vuelta
        p._ultimo_tiempo = 0.0
        p._eventos_vuelta.append("dnf_contacto")
        self._retirados.append(p)

    def _clasificacion(self, vuelta: int) -> list[dict]:
        """
        Clasificación de la vuelta: primero los que siguen en pista (orden de
        pista), luego los retirados (el que abandona más tarde, mejor clasificado).
        """
        retirados_ordenados = sorted(
            self._retirados, key=lambda p: (p.vuelta_abandono or 0), reverse=True
        )
        orden_final = self._orden_pista + retirados_ordenados

        filas = []
        for posicion, p in enumerate(orden_final, start=1):
            filas.append({
                "vuelta": vuelta,
                "posicion": posicion,
                "id_piloto": p.piloto.id_piloto,
                "nombre": p.piloto.nombre,
                "escuderia": p.coche.escuderia,
                "tiempo_vuelta": round(p._ultimo_tiempo, 3),
                "tiempo_total": round(p.tiempo_total, 3),
                "neumatico": p.neumatico.compuesto,
                "paradas": p.paradas,
                "estado": "activo" if p.activo else "abandonado",
                "eventos": list(p._eventos_vuelta),
            })
        return filas


def crear_participantes(
    parrilla: list[tuple[Piloto, Coche]],
    categoria: Optional[Categoria] = None,
    compuesto: Optional[str] = None,
) -> list[Participante]:
    """Convierte una lista de (piloto, coche) en participantes de una categoría."""
    categoria = categoria or Categoria.generica()
    compuesto = compuesto or categoria.compuesto_defecto
    return [
        Participante(
            piloto=pil, coche=coc,
            neumatico=Neumatico(compuesto, categoria.compuestos),
        )
        for pil, coc in parrilla
    ]
