"""
Constantes de ajuste del motor de simulación.

Todo lo "afinable" vive aquí para que puedas calibrar el realismo sin tocar
la lógica. Cambia estos valores y vuelve a correr la validación Monte Carlo
(scripts/prueba_montecarlo.py) para ver el efecto.
"""

# --- Tiempo de vuelta base ---------------------------------------------------
# Segundos de referencia de una vuelta "limpia" en un circuito medio.
TIEMPO_BASE_CIRCUITO = 90.0

# Peso relativo de piloto vs. coche en el ritmo. Deben sumar 1.0.
# El coche pesa algo más, como suele ocurrir en la realidad.
PESO_PILOTO = 0.45
PESO_COCHE = 0.55

# Rango total de pace (segundos) entre un combo perfecto (100) y uno pésimo (0).
RANGO_PACE = 2.5

# --- Combustible -------------------------------------------------------------
# Ganancia de tiempo por vuelta a medida que el coche se aligera (s/vuelta).
# Con depósito lleno el coche arranca lento y va acelerando.
GANANCIA_COMBUSTIBLE = 0.03

# --- Ruido gaussiano (consistencia) ------------------------------------------
# Desviación típica máxima del ruido por vuelta, para un piloto de
# consistencia 0. Un piloto de consistencia 100 tiene ruido ~0.
# Nota: en Fase 1 (solo ritmo) el ruido es la única fuente de sorpresas; la
# imprevisibilidad "de verdad" llegará con los eventos de la Fase 2 (safety
# car, incidentes, paradas). Por eso aquí lo mantenemos algo generoso.
SIGMA_MAX = 0.70

# --- Neumáticos --------------------------------------------------------------
# Cada compuesto: offset de ritmo (s, negativo = más rápido) y coeficientes
# de degradación (lineal y cuadrático) por vuelta de stint.
# El término cuadrático genera el "acantilado" al final del stint.
COMPUESTOS = {
    "blando": {"offset": -0.45, "deg_lineal": 0.045, "deg_cuad": 0.0060},
    "medio":  {"offset":  0.00, "deg_lineal": 0.030, "deg_cuad": 0.0030},
    "duro":   {"offset":  0.40, "deg_lineal": 0.018, "deg_cuad": 0.0012},
}
COMPUESTO_POR_DEFECTO = "medio"

# =============================================================================
# FASE 2 — Eventos, estrategia y adelantamientos
# =============================================================================

# --- Incidentes / errores ----------------------------------------------------
# Probabilidad base de incidente por piloto y vuelta (piloto medio, en seco).
# Se escala con la agresividad y el clima.
PROB_INCIDENTE_BASE = 0.008
# Cuánto multiplica la agresividad máxima (100) a la probabilidad base.
FACTOR_AGRESIVIDAD_INCIDENTE = 2.2
# Reparto de la gravedad de un incidente cuando ocurre:
PROB_INCIDENTE_LEVE = 0.65     # trompo/bloqueo: pierde tiempo, sigue
PROB_INCIDENTE_MEDIO = 0.25    # daño: pierde bastante tiempo (o para a reparar)
# el resto (0.10) es incidente grave -> DNF
PERDIDA_INCIDENTE_LEVE = (1.0, 4.0)     # rango de segundos perdidos
PERDIDA_INCIDENTE_MEDIO = (8.0, 20.0)

# --- Fiabilidad / fallos mecánicos -------------------------------------------
# Prob. de fallo mecánico por vuelta para fiabilidad 0. Fiabilidad 100 -> ~0.
PROB_FALLO_MECANICO_MAX = 0.010

# --- Safety car / VSC --------------------------------------------------------
# Probabilidad de que un incidente (medio/grave) despliegue el coche de seguridad.
PROB_SC_POR_INCIDENTE_GRAVE = 0.75
PROB_SC_POR_INCIDENTE_MEDIO = 0.25
PROB_VSC_EN_LUGAR_DE_SC = 0.40   # a veces es solo VSC (más leve)
DURACION_SC = (3, 5)             # rango de vueltas del safety car
DURACION_VSC = (1, 2)
FACTOR_RITMO_SC = 1.40           # las vueltas tras el SC son un 40% más lentas
FACTOR_RITMO_VSC = 1.35
GAP_BUNCHING_SC = 0.7            # segundos entre coches al agruparse tras el SC

# --- Paradas en boxes --------------------------------------------------------
TIEMPO_PARADA_BASE = 22.0        # pérdida total de una parada normal (s)
# Bajo safety car la parada "cuesta" menos tiempo relativo (todos van lentos).
FACTOR_PARADA_BAJO_SC = 0.55
UMBRAL_DEGRADACION_PARADA = 0.80 # nivel de desgaste (0-1) que dispara la parada
STINT_MINIMO = 8                 # vueltas mínimas antes de considerar parar

# --- Adelantamientos ---------------------------------------------------------
UMBRAL_ATAQUE = 1.0              # gap (s) por debajo del cual se intenta pasar
# Dificultad base de adelantar por tipo de trazado (mayor = más difícil).
DIFICULTAD_TRAZADO = {
    "ovalo": 0.6,
    "mixto": 0.9,
    "circuito": 1.0,
    "calle": 1.6,
}
PROB_CONTACTO_BASE = 0.012       # riesgo de contacto en un intento (agresividad media)
