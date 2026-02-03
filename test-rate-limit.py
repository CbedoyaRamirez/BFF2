#!/usr/bin/env python3
"""
Script de prueba de Rate Limiting para BFF API
Límite configurado: 60 requests por minuto
"""

import requests
import time
import sys
from datetime import datetime

# Configuración
HOST = "http://localhost:5016"
ENDPOINT = "/health"
TOTAL_REQUESTS = 80
DELAY_MS = 0.1  # Delay entre requests en segundos (100ms)

# Colores para terminal
class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'

def print_header():
    print(f"{Colors.OKCYAN}==========================================")
    print("  PRUEBA DE RATE LIMITING - BFF API")
    print("=========================================={Colors.ENDC}")
    print()
    print(f"{Colors.WARNING}URL: {HOST}{ENDPOINT}")
    print(f"Total Requests: {TOTAL_REQUESTS}")
    print(f"Delay: {DELAY_MS * 1000}ms")
    print(f"Límite esperado: 60 requests/minuto{Colors.ENDC}")
    print()
    print(f"{Colors.OKGREEN}Iniciando prueba...{Colors.ENDC}")
    print()

def test_rate_limit():
    success_count = 0
    rate_limited_count = 0
    error_count = 0
    start_time = datetime.now()

    for i in range(1, TOTAL_REQUESTS + 1):
        try:
            response = requests.get(f"{HOST}{ENDPOINT}", timeout=5)

            if response.status_code == 200:
                success_count += 1
                print(f"{Colors.OKGREEN}[{i}/{TOTAL_REQUESTS}] ✓ Status: 200 OK{Colors.ENDC}")
            elif response.status_code == 429:
                rate_limited_count += 1
                print(f"{Colors.FAIL}[{i}/{TOTAL_REQUESTS}] ⚠ Status: 429 - RATE LIMITED!{Colors.ENDC}")

                # Intentar mostrar el mensaje de error
                try:
                    error_msg = response.json()
                    print(f"    {Colors.FAIL}Mensaje: {error_msg}{Colors.ENDC}")
                except:
                    print(f"    {Colors.FAIL}Mensaje: Too Many Requests{Colors.ENDC}")
            else:
                error_count += 1
                print(f"{Colors.WARNING}[{i}/{TOTAL_REQUESTS}] ✗ Status: {response.status_code} - ERROR{Colors.ENDC}")

        except requests.exceptions.RequestException as e:
            error_count += 1
            print(f"{Colors.FAIL}[{i}/{TOTAL_REQUESTS}] ✗ ERROR: {str(e)}{Colors.ENDC}")

        # Delay entre requests
        if i < TOTAL_REQUESTS:
            time.sleep(DELAY_MS)

    end_time = datetime.now()
    duration = (end_time - start_time).total_seconds()

    return success_count, rate_limited_count, error_count, duration

def print_results(success, rate_limited, errors, duration):
    print()
    print(f"{Colors.OKCYAN}==========================================")
    print("  RESULTADOS")
    print(f"=========================================={Colors.ENDC}")
    print(f"Total Requests:        {TOTAL_REQUESTS}")
    print(f"{Colors.OKGREEN}✓ Exitosos (200):      {success}{Colors.ENDC}")
    print(f"{Colors.FAIL}⚠ Rate Limited (429):  {rate_limited}{Colors.ENDC}")
    print(f"{Colors.WARNING}✗ Errores:             {errors}{Colors.ENDC}")
    print(f"Duración total:        {duration:.2f}s")
    print()

    # Validación
    if success > 0 and rate_limited > 0:
        print(f"{Colors.OKGREEN}✓ RATE LIMITING ESTÁ FUNCIONANDO CORRECTAMENTE!{Colors.ENDC}")
        print(f"{Colors.OKGREEN}  - Primeros {success} requests fueron exitosos{Colors.ENDC}")
        print(f"{Colors.OKGREEN}  - Siguientes {rate_limited} requests fueron bloqueados{Colors.ENDC}")
    elif success == TOTAL_REQUESTS:
        print(f"{Colors.WARNING}⚠ ADVERTENCIA: Todos los requests fueron exitosos{Colors.ENDC}")
        print(f"{Colors.WARNING}  El rate limiting puede no estar funcionando{Colors.ENDC}")
    else:
        print(f"{Colors.FAIL}✗ ERROR: Resultados inesperados{Colors.ENDC}")
    print()

def main():
    try:
        print_header()
        success, rate_limited, errors, duration = test_rate_limit()
        print_results(success, rate_limited, errors, duration)
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}Prueba interrumpida por el usuario{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"\n{Colors.FAIL}Error inesperado: {str(e)}{Colors.ENDC}")
        sys.exit(1)

if __name__ == "__main__":
    main()
