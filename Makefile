# 
# Makefile para RolasApp
# 

DOTNET = dotnet
CONFIGURACION ?= Release

SOLUCION = RolasApp.slnx
PROYECTO = src/RolasApp/RolasApp.csproj
ESQUEMA_SQL = rolas.sql
BASE_DATOS = rolas.db

.PHONY: todo compilar ejecutar restaurar base-datos limpiar ayuda

# Compila la solución completa
todo: compilar

# Restaura las dependencias de paquetes NuGet
restaurar:
	$(DOTNET) restore $(SOLUCION)

# Compila el proyecto con la configuración elegida (Release por defecto)
compilar:
	$(DOTNET) build $(SOLUCION) --configuration $(CONFIGURACION)

# Ejecuta la aplicación Avalonia
ejecutar: compilar
	$(DOTNET) run --project $(PROYECTO) --configuration $(CONFIGURACION) --no-build

# Crea o reinicia la base de datos SQLite usando el esquema rolas.sql
base-datos:
	@if [ -f $(BASE_DATOS) ]; then \
		echo "Eliminando base de datos previa $(BASE_DATOS)..."; \
		rm -f $(BASE_DATOS); \
	fi
	sqlite3 $(BASE_DATOS) < $(ESQUEMA_SQL)
	@echo "Base de datos $(BASE_DATOS) creada exitosamente con $(ESQUEMA_SQL)."

# Limpia los binarios compilados y temporales
limpiar:
	$(DOTNET) clean $(SOLUCION)
	rm -rf src/RolasApp/bin src/RolasApp/obj

# Comandos
ayuda:
	@echo "Comandos disponibles:"
	@echo "  make            - Compila la solución completa en modo $(CONFIGURACION)"
	@echo "  make ejecutar   - Compila (si es necesario) y abre la ventana principal"
	@echo "  make base-datos - Genera $(BASE_DATOS) desde $(ESQUEMA_SQL) con sqlite3"
	@echo "  make limpiar    - Borra las carpetas bin/ y obj/"