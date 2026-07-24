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
