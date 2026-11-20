# 
# Makefile para RolasApp
# 

DOTNET = dotnet
CONFIGURACION ?= Release

SOLUCION = RolasApp.slnx
PROYECTO = src/RolasApp/RolasApp.csproj
ESQUEMA_SQL = rolas.sql
BASE_DATOS = rolas.db

LATEX ?= pdflatex
DIR_DOCS = docs
REPORTE_TEX = reporte.tex
REPORTE_PDF = $(DIR_DOCS)/reporte.pdf

.PHONY: todo compilar ejecutar reporte restaurar base-datos limpiar limpiar-reporte ayuda

# Compila la solución completa de C#
todo: compilar

# Restaura las dependencias de paquetes NuGet
restaurar:
	$(DOTNET) restore $(SOLUCION)

# Compila el proyecto con la configuración elegida (Release por defecto)
compilar:
	$(DOTNET) build $(SOLUCION) --configuration $(CONFIGURACION)

# Si se invoca "make ejecutar reporte", omite la compilación y ejecución de .NET
ifeq ($(filter reporte,$(MAKECMDGOALS)),reporte)
ejecutar:
	@true
else
ejecutar: compilar
	$(DOTNET) run --project $(PROYECTO) --configuration $(CONFIGURACION) --no-build
endif

# Compila el reporte en LaTeX dentro de docs/ y abre el PDF resultante
reporte:
	cd $(DIR_DOCS) && $(LATEX) -interaction=nonstopmode -halt-on-error $(REPORTE_TEX)
	cd $(DIR_DOCS) && $(LATEX) -interaction=nonstopmode -halt-on-error $(REPORTE_TEX)
	@xdg-open $(REPORTE_PDF) >/dev/null 2>&1 &

# Crea o reinicia la base de datos SQLite usando el esquema rolas.sql
base-datos:
	@if [ -f $(BASE_DATOS) ]; then \
		echo "Eliminando base de datos previa $(BASE_DATOS)..."; \
		rm -f $(BASE_DATOS); \
	fi
	sqlite3 $(BASE_DATOS) < $(ESQUEMA_SQL)
	@echo "Base de datos $(BASE_DATOS) creada exitosamente con $(ESQUEMA_SQL)."

# Limpia los binarios compilados y temporales de .NET y LaTeX
limpiar: limpiar-reporte
	$(DOTNET) clean $(SOLUCION)
	rm -rf src/RolasApp/bin src/RolasApp/obj

# Limpia únicamente los auxiliares y el PDF generados por LaTeX en docs/
limpiar-reporte:
	rm -f $(DIR_DOCS)/*.aux $(DIR_DOCS)/*.log $(DIR_DOCS)/*.out $(DIR_DOCS)/*.toc $(DIR_DOCS)/*.fls $(DIR_DOCS)/*.fdb_latexmk $(REPORTE_PDF)

# Comandos disponibles
ayuda:
	@echo "Comandos disponibles:"
	@echo "  make                  - Compila la solución completa en modo $(CONFIGURACION)"
	@echo "  make ejecutar         - Compila (si es necesario) y abre la ventana principal"
	@echo "  make ejecutar reporte - Compila docs/reporte.tex de forma independiente y abre docs/reporte.pdf"
	@echo "  make base-datos       - Genera $(BASE_DATOS) desde $(ESQUEMA_SQL) con sqlite3"
	@echo "  make limpiar          - Borra bin/, obj/ y los temporales de LaTeX"