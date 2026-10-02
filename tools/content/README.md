# Contenidos del curso

Aqui viven los 35 ejercicios del curso (10 de Fundamentos, 10 de Estructuras de
control, 10 de Funciones y 5 del Tutorial) y las herramientas que los verifican y
los dan de alta en la API.

## Por que un generador y no un JSON escrito a mano

El backend compara la salida del alumno con `ExpectedOutput` de forma **exacta**
(`StringComparison.Ordinal`, ver `Services/OutputNormalizer.cs`). Un `ExpectedOutput`
escrito a mano es una apuesta: en cuanto la solucion real de un alumno difiera en un
espacio o en un salto de linea, el ejercicio queda imposible de aprobar aunque el
programa sea correcto.

Por eso `build_exercises.py` **ejecuta** las tres soluciones de referencia con los
mismos datos de entrada y solo acepta el ejercicio si Python, Java y PSeint producen
exactamente la misma salida. Esa salida es la que se guarda.

Ademas valida las plantillas que veran los alumnos: el codigo de inicio de Java tiene
que compilar y el de PSeint tiene que traducirse. Una plantilla rota hace que el primer
"Ejecutar" del alumno falle por un motivo que no tiene que ver con su solution.

## Archivos

| Archivo | Que hace |
|---|---|
| `exercises_part1.py` | Ejercicios 1-15: Fundamentos (10) y los 4 faciles de Estructuras de control |
| `exercises_part2.py` | Ejercicios 16-30: los 6 restantes de control y Funciones (10) |
| `tutorial.py` | Los 5 ejercicios del Tutorial con sus 4 pasos guiados cada uno |
| `build_exercises.py` | Verifica los 35 (y los 20 pasos) y escribe `exercises.json` |
| `exercises.json` | Salida del generador. **Es el unico archivo que consume la siembra** |
| `seed-exercises.mjs` | Da de alta `exercises.json` en la API de administracion |
| `../pseint-tool/` | Traductor PSeint -> Python en linea de comandos |

El repositorio tiene un `tools/.gitignore` propio: la carpeta `out/` del traductor no
se versiona, se compila en local.

## Uso

Compilar el traductor (solo la primera vez, y despues de tocar `PSeintTranslator.cs`):

```bash
dotnet build tools/pseint-tool -c Release -o tools/pseint-tool/out
```

Verificar los 35 ejercicios y regenerar el JSON:

```bash
python tools/content/build_exercises.py --out tools/content/exercises.json
```

Requiere Python 3, un JDK en el PATH (para `javac`) y el SDK de .NET.

Dar de alta el catalogo en la API:

```bash
PS_API_URL=https://psacademy-api.onrender.com \
PS_ADMIN_EMAIL=<tu admin> \
PS_ADMIN_PASSWORD=<tu clave> \
node tools/content/seed-exercises.mjs
```

`PS_DRY_RUN=1` muestra lo que haria sin enviar nada. El script es idempotente: si un
titulo ya existe lo actualiza con `PUT` en lugar de duplicarlo, y desactiva (no borra)
los ejercicios que ya no estan en el JSON, porque borrarlos en cascada llevaria por
delante el progreso de los alumnos.

## Reglas que sigue el contenido

Estas reglas no son esteticas: si no se cumplen, el generador falla.

1. **Un solo lenguaje activo por solucion PSeint.** El traductor de
   `Services/PSeintTranslator.cs` admite un subconjunto: no soporta `Dimension`,
   `Segun`, `SubProceso`/`Funcion`, ni `Sino Si`, ni bloques en una sola linea. Como
   `EjerciciosController` traduce el PSeint del alumno con ese mismo traductor, un
   ejercicio con PSeint fuera del subconjunto seria imposible de resolver.
2. **Sin division con decimales.** En Python 3, `10 / 2` da `5.0` mientras que en
   Java da `5`. Cuando un ejercicio divide, las tres soluciones fuerzan entero:
   `//` en Python, division entre `int` en Java y `Trunc(...)` en PSeint.
3. **Los ejercicios de la categoria Funciones no usan funciones en PSeint.** El
   traductor no soporta subprocesos, asi que la solucion PSeint de esos ejercicios
   resuelve la logica en un solo bloque y el enunciado lo dice.
4. **Reparto fijo:** las categorias de retos usan 10 ejercicios con 4 `Easy`, 4 `Medium`
   y 2 `Hard`; el Tutorial son 5 ejercicios `Easy`. El generador lo comprueba y falla
   si no cuadra.
5. **Los operadores logicos de PSeint se distinguen por mayuscula.** `Y`, `O`, `No` y
   `Mod` se traducen solo en mayuscula para que una variable llamada `y` no se convierta
   en el operador `and`.
6. **Las entradas son valores, no un texto para analizar.** `Inputs` alimenta el `Leer`
   de los tres lenguajes a traves de `stdin`, asi que los textos no llevan comillas y
   los numeros son parseables. Cada paso del tutorial que use `Leer` necesita su propio
   `stdin` (el que se le pasa a `run_pseint`), porque el del ejercicio no siempre sirve.
7. **La salida del ultimo paso es la del ejercicio.** El frontend solo deja pasar al
   siguiente paso cuando el backend confirma que la salida coincide, y el backend solo
   marca la leccion como superada en el ultimo paso: por eso ese paso tiene que
   producir exactamente `ExpectedOutput`. El generador lo comprueba y falla si no.