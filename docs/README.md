# RolasApp — Guía de Usuario

**RolasApp** es una aplicación de escritorio para organizar y consultar tu colección de música en formato MP3. Permite escanear carpetas para importar tus canciones, buscar pistas rápidamente, editar sus datos y clasificar a los artistas como solistas o bandas musicales con sus respectivos integrantes.

> **Nota importante:** Todos los cambios que realices se guardan únicamente en la biblioteca del programa. **Tus archivos `.mp3` originales nunca se modifican ni se borran de tu disco.**

## Cómo Ejecutar el Programa

### Requisitos

- Tener instalado **.NET 10** en tu sistema.
- Tener instalado `make` y `sqlite3` (opcional, solo si deseas reiniciar la base de datos manualmente).

### Pasos desde la terminal

Ubícate en la carpeta raíz del proyecto y ejecuta:

```bash
# Compilar y abrir la aplicación
make ejecutar
```

Si solo deseas compilar sin abrir la ventana o limpiar archivos temporales:

```bash
make compilar   # Compila el programa
make limpiar    # Borra archivos temporales de compilación
```

## Cómo Compilar el Reporte (LaTeX)

El código fuente del reporte técnico se encuentra en `docs/reporte.tex` y se compila de forma independiente a la aplicación.

### Instalación de dependencias de LaTeX

Antes de compilar el PDF por primera vez, instala `pdflatex` y los paquetes requeridos (`babel-spanish`, `lmodern` y `tikz`) según tu distribución Linux:

- **Debian / Ubuntu / Linux Mint:**

  ```bash
  sudo apt update
  sudo apt install texlive-latex-base texlive-latex-recommended texlive-latex-extra texlive-fonts-recommended texlive-pictures texlive-lang-spanish
  ```

- **Fedora:**
  ```bash
  sudo dnf install texlive-scheme-basic texlive-collection-latexrecommended texlive-collection-fontsrecommended texlive-collection-pictures texlive-babel-spanish texlive-pgf
  ```

### Compilar y abrir el PDF

Desde la carpeta raíz del proyecto, ejecuta:

```bash
make ejecutar reporte
```

Este comando compila `docs/reporte.tex` mediante `pdflatex` sin ejecutar la aplicación en C# y abre automáticamente `docs/reporte.pdf`.

## Guía de la Interfaz y Botones

### 1. Ventana Principal

#### A. Barra Superior (Búsqueda e Importación)

| Botón / Control               | ¿Para qué sirve?                                                                                                                           |
| :---------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------- |
| **Barra de Búsqueda**         | Filtra las canciones en tiempo real mientras escribes. Puedes buscar cualquier palabra o usar filtros específicos (ver ejemplos al final). |
| **`Limpiar`**                 | Borra el texto escrito en el buscador y vuelve a mostrar toda tu colección completa.                                                       |
| **`Intérpretes y Bandas...`** | Abre la ventana secundaria para definir si un artista es solista o banda, editar sus datos biográficos y administrar sus integrantes.      |
| **`Elegir Directorio...`**    | Abre el explorador de archivos para que elijas cualquier carpeta de tu computadora e importes sus archivos MP3.                            |
| **`Minar ~/Música`**          | Escanea automáticamente tu carpeta predeterminada de `Música` (incluyendo subcarpetas) e importa todas las canciones encontradas.          |

#### B. Lista de Canciones y Panel de Detalles

| Elemento / Botón                    | ¿Para qué sirve?                                                                                                                                                                |
| :---------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Tabla de Canciones**              | Muestra tu biblioteca ordenada por título con número de pista, título, intérprete, álbum, año y género. Haz clic sobre cualquier fila para ver y editar sus datos a la derecha. |
| **Recuadros `Portada` y `Artista`** | Muestran la carátula del álbum y la fotografía del artista si el archivo MP3 las tiene incrustadas.                                                                             |
| **Campos de Texto**                 | Te permiten corregir o cambiar el **Título**, **Intérprete**, **Álbum**, **Pista**, **Año** y **Género** de la canción seleccionada. La ruta del archivo es solo informativa.   |
| **`Guardar en BD`**                 | Guarda los cambios que hayas escrito en los campos del panel derecho.                                                                                                           |
| **`Eliminar`**                      | Quita la canción seleccionada de la lista del programa (sin borrar el archivo MP3 de tu computadora).                                                                           |

### 2. Ventana de Intérpretes, Personas y Bandas

Al pulsar **`Intérpretes y Bandas...`** en la ventana principal, se abre esta vista dividida en dos secciones:

#### A. Clasificar y Editar Intérpretes (Izquierda y Centro)

| Control / Botón                      | ¿Para qué sirve?                                                                                                                 |
| :----------------------------------- | :------------------------------------------------------------------------------------------------------------------------------- |
| **Lista de Intérpretes**             | Muestra todos los artistas detectados en tus canciones junto con su clasificación actual (`Persona`, `Grupo` o `Desconocido`).   |
| **Selector `Tipo de intérprete`**    | Permite definir si el artista seleccionado es una **`Persona (Individuo)`**, un **`Grupo (Banda musical)`** o **`Desconocido`**. |
| **`Guardar Detalles de Persona`**    | Guarda el nombre artístico, nombre real, fecha de nacimiento y fecha de fallecimiento de un solista.                             |
| **`Guardar Detalles de la Banda`**   | Guarda el nombre oficial, año/fecha de inicio y fecha de separación de una banda.                                                |
| **`Agregar a la Banda`**             | Añade al músico elegido en el menú desplegable como integrante de la banda seleccionada.                                         |
| **`Quitar integrante seleccionado`** | Elimina al músico seleccionado de la lista de integrantes de la banda.                                                           |

#### B. Alta de Individuos Independientes (Parte Inferior Derecha)

| Control / Botón             | ¿Para qué sirve?                                                                                                                                                                       |
| :-------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`Dar de Alta Individuo`** | Permite registrar a un músico nuevo (nombre artístico, nombre real y fechas) **aunque no tenga canciones como solista**, para poder agregarlo después como miembro de cualquier banda. |
| **`Cerrar`**                | Cierra esta ventana y actualiza los cambios en la pantalla principal.                                                                                                                  |

## Ejemplos de Búsqueda

En la barra superior puedes escribir palabras sueltas o combinar filtros con dos puntos (`:`):

- `Rock` — Muestra todas las canciones que contengan _Rock_ en cualquier campo.
- `artista:The Beatles` — Busca canciones cuyo intérprete sea _Queen_.
- `album:Abbey año:1969` — Filtra canciones del álbum _Abbey_ del año _1969_.
- `genero:Jazz` — Muestra únicamente las canciones del género _Jazz_.
