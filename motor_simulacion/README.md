# Motor de simulación de carreras

Motor de simulación **agnóstico a la categoría**: el mismo núcleo sirve para
F1, IndyCar, WEC, GTs, DTM, Formula E, NASCAR, etc. Una categoría no es más que
*configuración + datos* (tiempo base del circuito, número de coches, compuestos
de neumático, reglas de puntos y formato del fin de semana). Esto es la base del
objetivo a largo plazo: un "Football Manager del automovilismo" con progresión
de carrera y múltiples categorías.

## Estado: Fases 1 y 2 — COMPLETADAS

- **Fase 1**: modelo de ritmo (piloto+coche), degradación de neumáticos,
  combustible y ruido gaussiano; bucle de carrera; validación Monte Carlo.
- **Fase 2**: incidentes/errores (según agresividad y clima), fallos mecánicos
  y DNFs, safety car / VSC (con agrupamiento del pelotón), estrategia de paradas
  por umbral de degradación, y adelantamientos probabilísticos con riesgo de
  contacto. Todo **categoría-aware**: el tipo de trazado influye (adelantar en
  óvalo es fácil; en circuito urbano, difícil).

Además, el motor es ya **multi-categoría**: `categorias/catalogo.py` define F1,
F2, F3, F4, karting, IndyCar, Indy NXT, Fórmula E, WEC, IMSA, GT3, DTM, WTCR y
NASCAR (Cup/Xfinity/Trucks), organizadas por *familias* y con su *escalera de
ascensos* (`asciende_a`), base del futuro modo carrera.

### Detalle Fase 1 (motor de ritmo)

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
python3 -m scripts.prueba_montecarlo --simulaciones 500 --vueltas 30

# Demo narrada de UNA carrera con eventos (Fase 2), elige categoría/clima
python3 -m scripts.demo_carrera --categoria f1 --semilla 7
python3 -m scripts.demo_carrera --categoria f1 --clima lluvia --semilla 2
python3 -m scripts.demo_carrera --categoria nascar_cup --vueltas 60 --semilla 5
```

`demo_carrera` muestra parrilla de salida, eventos vuelta a vuelta (safety car,
paradas, incidentes, adelantamientos, abandonos) y clasificación final. Con la
Fase 2 activa, la validación Monte Carlo pasa de casi determinista a mostrar
verdadera variabilidad: el mejor sigue ganando más (~34%) pero cualquiera puede
ganar un día y el líder puede irse al fondo por un DNF.

Deberías ver una tabla con la posición media, mejor/peor resultado y % de
victorias por piloto. Resultado esperado: el equipo puntero domina y su mejor
piloto gana la mayoría de las veces (~74%), **pero no siempre** (su compañero
gana ~26%), y el mediocampo baraja posiciones carrera a carrera. Esa es la
variabilidad realista buscada.

> Nota de diseño: en Fase 1 la única fuente de sorpresas es el ruido gaussiano,
> así que el resultado tiende a converger. La imprevisibilidad "de verdad"
> (safety cars, incidentes, estrategia de paradas, adelantamientos) llega en la
> Fase 2.
