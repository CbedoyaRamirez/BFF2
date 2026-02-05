#!/bin/bash

# Script para reiniciar la aplicación y verificar que las carpetas de logs se crean

echo ""
echo "========================================"
echo "  REINICIO DE APLICACIÓN - LOGS FIX"
echo "========================================"
echo ""

# 1. Matar procesos dotnet
echo -e "\033[33m[1/5] Deteniendo aplicación existente...\033[0m"
if pgrep -x "dotnet" > /dev/null; then
    killall dotnet 2>/dev/null
    echo -e "      \033[32m✓ Aplicación detenida\033[0m"
    sleep 2
else
    echo -e "      \033[90mℹ No hay aplicación corriendo\033[0m"
fi

# 2. Limpiar logs (para verificar que se crean)
echo ""
echo -e "\033[33m[2/5] Limpiando carpetas de logs...\033[0m"
for dir in "Chubb.Bot.AI.Assistant.Api/logs/error" "Chubb.Bot.AI.Assistant.Api/logs/performance" "Chubb.Bot.AI.Assistant.Api/logs/dev"; do
    if [ -d "$dir" ]; then
        rm -rf "$dir"
        echo -e "      \033[32m✓ Eliminado: $dir\033[0m"
    fi
done

# 3. Compilar
echo ""
echo -e "\033[33m[3/5] Compilando proyecto...\033[0m"
cd Chubb.Bot.AI.Assistant.Api
dotnet build > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo -e "      \033[32m✓ Compilación exitosa\033[0m"
else
    echo -e "      \033[31m✗ Error en compilación\033[0m"
    echo ""
    echo -e "\033[31mDetalles del error:\033[0m"
    dotnet build
    cd ..
    exit 1
fi

# 4. Verificar que las carpetas NO existen antes de iniciar
echo ""
echo -e "\033[33m[4/5] Verificando estado inicial...\033[0m"
if [ ! -d "logs/error" ] && [ ! -d "logs/performance" ] && [ ! -d "logs/dev" ]; then
    echo -e "      \033[32m✓ Carpetas listas para crearse\033[0m"
else
    echo -e "      \033[90mℹ Algunas carpetas ya existen\033[0m"
fi

# 5. Iniciar aplicación
echo ""
echo -e "\033[33m[5/5] Iniciando aplicación...\033[0m"
echo ""
echo "========================================"
echo "BUSCA ESTOS MENSAJES EN LA CONSOLA:"
echo "========================================"
echo ""
echo "  [INFO] Created log directory: logs"
echo "  [INFO] Created log directory: logs/error"
echo "  [INFO] Created log directory: logs/performance"
echo "  [INFO] Created log directory: logs/dev"
echo ""
echo -e "\033[90mO si ya existen:\033[0m"
echo -e "\033[90m  [INFO] Log directory already exists: ...\033[0m"
echo ""
echo "========================================"
echo ""

# Iniciar
dotnet run

cd ..
