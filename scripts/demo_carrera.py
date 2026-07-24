"""
Demo narrada de una carrera (Fase 2).

Simula UNA carrera de una categoría concreta y muestra:
- La parrilla de salida.
- Un resumen de eventos vuelta a vuelta (safety car, paradas, incidentes,
  adelantamientos, abandonos).
- La clasificación final.

Uso:
    python -m scripts.demo_carrera
    python -m scripts.demo_carrera --categoria f1 --clima lluvia --semilla 3
    python -m scripts.demo_carrera --categoria nascar_cup --vueltas 60
"""

from __future__ import annotations

import argparse

from motor_simulacion.carrera import Carrera, crear_participantes
from motor_simulacion.modelos.clima import Clima
from motor_simulacion.categorias import catalogo
from motor_simulacion.datos import cargar_parrilla_ejemplo


ETIQUETAS_EVENTO = {
    "sc": "🟡 Safety Car",
    "vsc": "🟠 VSC",
    "parada": "🔧 Parada en boxes",
    "adelanta": "↗️  Adelantamiento",
    "incidente_leve": "⚠️  Incidente leve",
    "incidente_medio": "⚠️  Incidente (daño)",
    "dnf_error": "💥 Abandono (error)",
    "dnf_mecanico": "🔩 Abandono (mecánico)",
    "dnf_contacto": "💢 Abandono (contacto)",
}


def ejecutar(categoria_id: str, num_vueltas: int | None, clima_str: str,
             semilla: int | None) -> None:
    categoria = catalogo.obtener(categoria_id)
    vueltas = num_vueltas or categoria.vueltas_tipicas
    clima = Clima(condicion=clima_str)

    parrilla = cargar_parrilla_ejemplo()
    participantes = crear_participantes(parrilla, categoria)
    carrera = Carrera(participantes, num_vueltas=vueltas, categoria=categoria,
                      clima=clima, semilla=semilla)
    resultado = carrera.simular()

    print(f"\n=== {categoria.nombre} | {vueltas} vueltas | clima: {clima_str} ===\n")

    print("Parrilla de salida:")
    for fila in resultado["vueltas"][0]:
        print(f"  P{fila['posicion']:<2} {fila['nombre']:<16} {fila['escuderia']}")

    print("\nEventos destacados:")
    hubo_evento = False
    for reg_vuelta in resultado["vueltas"]:
        vuelta = reg_vuelta[0]["vuelta"]
        for fila in reg_vuelta:
            for ev in fila["eventos"]:
                etiqueta = ETIQUETAS_EVENTO.get(ev)
                if etiqueta and ev not in ("sc", "vsc"):
                    print(f"  V{vuelta:<3} {etiqueta:<24} {fila['nombre']}")
                    hubo_evento = True
        # Safety car: una sola línea por vuelta (no por piloto)
        estados_sc = {ev for fila in reg_vuelta for ev in fila["eventos"]
                      if ev in ("sc", "vsc")}
        for ev in estados_sc:
            print(f"  V{vuelta:<3} {ETIQUETAS_EVENTO[ev]}")
            hubo_evento = True
    if not hubo_evento:
        print("  (carrera limpia, sin incidencias)")

    print("\nClasificación final:")
    print(f"  {'Pos':<4}{'Piloto':<17}{'Escudería':<20}{'Paradas':<8}{'Estado'}")
    for fila in resultado["clasificacion_final"]:
        print(f"  {fila['posicion']:<4}{fila['nombre']:<17}{fila['escuderia']:<20}"
              f"{fila['paradas']:<8}{fila['estado']}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Demo narrada de una carrera (Fase 2).")
    parser.add_argument("--categoria", default="f1",
                        help=f"id de categoría. Opciones: {', '.join(catalogo.CATALOGO)}")
    parser.add_argument("--vueltas", type=int, default=None,
                        help="Nº de vueltas (por defecto, las típicas de la categoría).")
    parser.add_argument("--clima", default="seco", choices=["seco", "lluvia"])
    parser.add_argument("--semilla", type=int, default=None)
    args = parser.parse_args()
    ejecutar(args.categoria, args.vueltas, args.clima, args.semilla)


if __name__ == "__main__":
    main()
