#!/bin/bash

# ============================================
# SCRIPT DE PRUEBA DE RATE LIMITING
# ============================================
# Límite configurado: 60 requests por minuto

HOST="http://localhost:5016"
ENDPOINT="/health"
TOTAL_REQUESTS=80
DELAY=0.1  # Delay en segundos

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}=========================================="
echo "  PRUEBA DE RATE LIMITING - BFF API"
echo -e "==========================================${NC}"
echo ""
echo -e "${YELLOW}URL: ${HOST}${ENDPOINT}"
echo "Total Requests: ${TOTAL_REQUESTS}"
echo "Delay: ${DELAY}s"
echo -e "Límite esperado: 60 requests/minuto${NC}"
echo ""
echo -e "${GREEN}Iniciando prueba...${NC}"
echo ""

SUCCESS_COUNT=0
RATE_LIMITED_COUNT=0
ERROR_COUNT=0

for ((i=1; i<=${TOTAL_REQUESTS}; i++)); do
    # Hacer el request y capturar el status code
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${HOST}${ENDPOINT}")

    if [ "$HTTP_CODE" -eq 200 ]; then
        SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
        echo -e "${GREEN}[$i/$TOTAL_REQUESTS] ✓ Status: 200 OK${NC}"
    elif [ "$HTTP_CODE" -eq 429 ]; then
        RATE_LIMITED_COUNT=$((RATE_LIMITED_COUNT + 1))
        echo -e "${RED}[$i/$TOTAL_REQUESTS] ⚠ Status: 429 - RATE LIMITED!${NC}"

        # Capturar el mensaje de error
        ERROR_MSG=$(curl -s "${HOST}${ENDPOINT}")
        echo -e "    ${RED}Mensaje: ${ERROR_MSG}${NC}"
    else
        ERROR_COUNT=$((ERROR_COUNT + 1))
        echo -e "${YELLOW}[$i/$TOTAL_REQUESTS] ✗ Status: ${HTTP_CODE} - ERROR${NC}"
    fi

    # Delay entre requests
    if [ $i -lt $TOTAL_REQUESTS ]; then
        sleep $DELAY
    fi
done

echo ""
echo -e "${CYAN}=========================================="
echo "  RESULTADOS"
echo -e "==========================================${NC}"
echo "Total Requests:        ${TOTAL_REQUESTS}"
echo -e "${GREEN}✓ Exitosos (200):      ${SUCCESS_COUNT}${NC}"
echo -e "${RED}⚠ Rate Limited (429):  ${RATE_LIMITED_COUNT}${NC}"
echo -e "${YELLOW}✗ Errores:             ${ERROR_COUNT}${NC}"
echo ""

# Validación
if [ $SUCCESS_COUNT -gt 0 ] && [ $RATE_LIMITED_COUNT -gt 0 ]; then
    echo -e "${GREEN}✓ RATE LIMITING ESTÁ FUNCIONANDO CORRECTAMENTE!${NC}"
    echo -e "${GREEN}  - Primeros ${SUCCESS_COUNT} requests fueron exitosos${NC}"
    echo -e "${GREEN}  - Siguientes ${RATE_LIMITED_COUNT} requests fueron bloqueados${NC}"
elif [ $SUCCESS_COUNT -eq $TOTAL_REQUESTS ]; then
    echo -e "${YELLOW}⚠ ADVERTENCIA: Todos los requests fueron exitosos${NC}"
    echo -e "${YELLOW}  El rate limiting puede no estar funcionando${NC}"
else
    echo -e "${RED}✗ ERROR: Resultados inesperados${NC}"
fi

echo ""
