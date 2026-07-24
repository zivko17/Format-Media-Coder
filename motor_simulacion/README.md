# Motor de simulación de carreras

Motor de simulación **agnóstico a la categoría**: el mismo núcleo sirve para
F1, IndyCar, WEC, GTs, DTM, Formula E, NASCAR, etc. Una categoría no es más que
*configuración + datos* (tiempo base del circuito, número de coches, compuestos
de neumático, reglas de puntos y formato del fin de semana). Esto es la base del
objetivo a largo plazo: un "Football Manager del automovilismo" con progresión
de carrera y múltiples categorías.

## Estado: Fase 1 (motor de ritmo) — COMPLETADA

Implementado:

- **Modelos** (`modelos/`): `Piloto`, `Coche`, `Neumatico`, `Clima`.
- **Tiempo de vuelta** (`simulacion_vuelta.py`): ritmo (piloto+coche) +
  degradación no lineal de neumáticos + efecto de combustible + ruido gaussiano
  cuya varianza depende de la consistencia del piloto.
- **Carrera** (`carrera.py`): bucle vuelta a vuelta con clasificación y salida
  en dict/JSON lista para la API.
- **Regens** (`generador_pilotos.py`): fábrica de pilotos aleatorios con edad y
  potencial, cimiento del sistema generacional por temporadas.
- **Datos** (`datos/parrilla_ejemplo.py`): parrilla data-driven; añadir pilotos
  = editar datos, sin tocar el motor.

## Fórmula del tiempo de vuelta

```
tiempo_vuelta = TIEMPO_BASE_CIRCUITO
              + penalizacion_ritmo        # (100 - (0.45*hab + 0.55*ren)) / 100 * RANGO_PACE
              + offset_compuesto          # blando/medio/duro
              + degradacion_neumaticos    # (deg_lineal*v + deg_cuad*v²) * factor_cuidado
              + efecto_combustible        # GANANCIA_COMBUSTIBLE * vueltas_restantes
              + penalizacion_grip         # clima (Fase 1 = 0)
              + N(0, sigma)               # sigma = SIGMA_MAX * (1 - consistencia/100)
```

Todas las constantes están en `config.py` para calibrar el realismo.

## Cómo probarlo

Desde la raíz del repo:

```bash
# Validación Monte Carlo (100 carreras, 30 vueltas por defecto)
python3 -m scripts.prueba_montecarlo

# Más simulaciones / otras vueltas
python3 -m scripts.prueba_montecarlo --simulaciones 500 --vueltas 30
```

Deberías ver una tabla con la posición media, mejor/peor resultado y % de
victorias por piloto. Resultado esperado: el equipo puntero domina y su mejor
piloto gana la mayoría de las veces (~74%), **pero no siempre** (su compañero
gana ~26%), y el mediocampo baraja posiciones carrera a carrera. Esa es la
variabilidad realista buscada.

> Nota de diseño: en Fase 1 la única fuente de sorpresas es el ruido gaussiano,
> así que el resultado tiende a converger. La imprevisibilidad "de verdad"
> (safety cars, incidentes, estrategia de paradas, adelantamientos) llega en la
> Fase 2.
