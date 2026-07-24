"""
Validación Monte Carlo de la Fase 1.

Corre N veces la MISMA carrera (misma parrilla) variando solo la semilla y
muestra la distribución de posiciones finales por piloto. Sirve para verificar
que hay variabilidad realista, pero el mejor coche/piloto gana más a menudo.

Uso:
    python -m scripts.prueba_montecarlo
    python -m scripts.prueba_montecarlo --simulaciones 500 --vueltas 30
"""

from __future__ import annotations

import argparse
from collections import defaultdict

from motor_simulacion.carrera import Carrera, crear_participantes
from motor_simulacion.datos import cargar_parrilla_ejemplo


def ejecutar(num_simulaciones: int, num_vueltas: int) -> None:
    parrilla = cargar_parrilla_ejemplo()
    nombres = [pil.nombre for pil, _ in parrilla]

    # posiciones[nombre] = lista con la posición final de cada simulación
    posiciones: dict[str, list[int]] = defaultdict(list)
    victorias: dict[str, int] = defaultdict(int)

    for s in range(num_simulaciones):
        # Reconstruimos participantes en cada simulación (estado limpio) y
        # variamos la semilla para tener una carrera distinta cada vez.
        participantes = crear_participantes(cargar_parrilla_ejemplo())
        carrera = Carrera(participantes, num_vueltas=num_vueltas, semilla=s)
        resultado = carrera.simular()

        for fila in resultado["clasificacion_final"]:
            posiciones[fila["nombre"]].append(fila["posicion"])
        ganador = resultado["clasificacion_final"][0]["nombre"]
        victorias[ganador] += 1

    _imprimir_resumen(nombres, posiciones, victorias, num_simulaciones, num_vueltas)


def _imprimir_resumen(nombres, posiciones, victorias, num_simulaciones, num_vueltas):
    print(f"\n=== Validación Monte Carlo: {num_simulaciones} carreras de "
          f"{num_vueltas} vueltas, {len(nombres)} pilotos ===\n")

    # Ordenamos por posición media (mejor arriba).
    orden = sorted(nombres, key=lambda n: sum(posiciones[n]) / len(posiciones[n]))

    cabecera = f"{'Piloto':<16} {'Pos.media':>9} {'Mejor':>6} {'Peor':>5} {'Victorias':>10} {'% vict':>7}"
    print(cabecera)
    print("-" * len(cabecera))

    for nombre in orden:
        lista = posiciones[nombre]
        media = sum(lista) / len(lista)
        mejor = min(lista)
        peor = max(lista)
        vict = victorias.get(nombre, 0)
        pct = 100.0 * vict / num_simulaciones
        print(f"{nombre:<16} {media:>9.2f} {mejor:>6} {peor:>5} {vict:>10} {pct:>6.1f}%")

    print("\nInterpretación esperada: el piloto/coche de arriba debería ganar")
    print("bastante más que el resto, pero sin barrer al 100% (variabilidad real).")


def main() -> None:
    parser = argparse.ArgumentParser(description="Validación Monte Carlo (Fase 1).")
    parser.add_argument("--simulaciones", type=int, default=100,
                        help="Número de carreras a simular (por defecto 100).")
    parser.add_argument("--vueltas", type=int, default=30,
                        help="Vueltas por carrera (por defecto 30).")
    args = parser.parse_args()
    ejecutar(args.simulaciones, args.vueltas)


if __name__ == "__main__":
    main()
