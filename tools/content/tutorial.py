"""
Tutorial de PS Academy: las cinco lecciones que abren el curso.

Cada leccion es un ejercicio de la categoria "Tutorial" (dificultad Easy) con sus
pasos guiados. Un paso explica un concepto, trae un programa completo en PSeint
que el alumno ejecuta en el navegador y una salida esperada propia: el generador
lo ejecuta de verdad, igual que hace con las soluciones.

El ULTIMO paso de cada leccion es el reto del ejercicio. Por eso sus dos salidas
tienen que coincidir, y `build_exercises.py` lo comprueba: sin esa garantia el
alumno podria superar el reto del ejercicio y quedarse atascado en el paso final.

Decisiones de contenido que no son esteticas:

1. **Solo PSeint en los pasos.** El curso se explica en pseudocodigo y el traductor
   del backend es el que lo ejecuta; escribir cada paso tres veces (PSeint, Python y
   Java) triplicaria el contenido sin aportar nada al objetivo, que es entender el
   concepto. Los tres lenguajes siguen disponibles en el reto final de cada leccion.
2. **Sin reales ni decimales.** En Python `10 / 2` da `5.0` y en Java `5`. Cuando
   hace falta dividir se usa `Trunc(...)`, que el traductor ya soporta.
3. **Los textos literales son identicos en los tres lenguajes**, porque la salida se
   compara con `StringComparison.Ordinal`.
"""

from __future__ import annotations

from build_exercises import Exercise, Step, i_number, i_text

# --------------------------------------------------------------- Plantillas


def java_program(*statements: str) -> str:
    """main() en Java con las instrucciones indicadas (una por linea de codigo)."""
    body = "\n".join(("        " + s.strip()) if s.strip() else "" for s in statements)
    return (
        "import java.util.Scanner;\n\n"
        "public class Main {\n\n"
        "    static Scanner sc = new Scanner(System.in);\n\n"
        "    public static void main(String[] args) {\n"
        f"{body}\n"
        "    }\n"
        "}\n"
    )


TUTORIAL_EXERCISES: list[Exercise] = []


def add(**kwargs) -> None:
    TUTORIAL_EXERCISES.append(Exercise(**kwargs))


# =====================================================================
# LECCION 1 - El programa y la consola: Proceso, Escribir y nada mas.
# =====================================================================

add(
    key="tut-01",
    category="Tutorial",
    title="Tu primer programa",
    description=(
        "Un programa es una lista de instrucciones que el ordenador ejecuta de arriba abajo.\n\n"
        "En esta leccion descubres las dos piezas que usaremos en todas las demas:\n"
        "- El bloque `Proceso ... FinProceso`, que encierra el programa.\n"
        "- La instruccion `Escribir`, que muestra un mensaje en la consola.\n\n"
        "No necesitas saber nada de PSeint: solo ejecuta los tres pasos y veras como "
        "nace un programa de verdad."
    ),
    difficulty="Easy",
    inputs=[],
    starter={
        "python": "# Muestra tres mensajes, uno por linea, con print().\n",
        "java": java_program(
            "// Muestra aqui los tres mensajes con System.out.println.",
        ),
        "pseint": (
            "Proceso MiPrimerPrograma\n"
            "\t// Cada linea que escribas aqui aparecera en la consola.\n"
            "\t// Usa Escribir con el texto entre comillas dobles.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": 'print("PS Academy")\nprint("Aprende a programar")\nprint("Es muy facil")\n',
        "java": java_program(
            'System.out.println("PS Academy");',
            'System.out.println("Aprende a programar");',
            'System.out.println("Es muy facil");',
        ),
        "pseint": (
            "Proceso MiPrimerPrograma\n"
            '\tEscribir "PS Academy"\n'
            '\tEscribir "Aprende a programar"\n'
            '\tEscribir "Es muy facil"\n'
            "FinProceso\n"
        ),
    },
    tutorial=[
        Step(
            title="Todo empieza con Proceso",
            body=(
                "Todo programa en PSeint tiene la misma estructura: `Proceso` abre el bloque "
                "y `FinProceso` lo cierra. Entre medias van las instrucciones, una por linea.\n\n"
                "El ordenador las ejecuta **de arriba abajo**, sin saltarse ninguna. Y nada "
                "fuera de ese bloque forma parte del programa.\n\n"
                "Este programa tiene una sola instruccion: `Escribir \"Hola\"`. Lo que va "
                "entre comillas dobles es un texto fijo, se muestra tal cual."
            ),
            snippet=(
                "Proceso MiPrimerPrograma\n"
                '\tEscribir "Hola"\n'
                "FinProceso\n"
            ),
            task="Ejecuta el programa. Despues cambia la palabra Hola por tu nombre de pila y vuelve a ejecutar.",
            tip="Si la consola se queda vacia, comprueba que el Escribir esta entre Proceso y FinProceso.",
        ),
        Step(
            title="Cada Escribir, una linea",
            body=(
                "Cada `Escribir` termina con un salto de linea, asi que tres instrucciones "
                "`Escribir` producen tres renglones.\n\n"
                "- `Escribir \"Uno\"` imprime `Uno` y baja a la siguiente linea.\n"
                "- `Escribir \"Uno\", \"Dos\"` imprime `UnoDos` en la **misma** linea: la coma "
                "concatena sin dejar espacio.\n"
            ),
            snippet=(
                "Proceso VariasLineas\n"
                '\tEscribir "Primera linea"\n'
                '\tEscribir "Segunda linea"\n'
                '\tEscribir "Tercera linea"\n'
                "FinProceso\n"
            ),
            task="Anade un cuarto Escribir con el nombre de tu colegio y ejecuta otra vez.",
            tip="Un Escribir por linea es lo habitual; la coma solo se usa para juntar textos cortos.",
        ),
        Step(
            title="Texto fijo o valor calculado",
            body=(
                "Aqui esta la diferencia que mas se confunde al empezar:\n\n"
                "- **Entre comillas** es un texto fijo: `Escribir \"7 + 5\"` imprime `7 + 5`.\n"
                "- **Sin comillas** es un valor o una operacion: `Escribir 7 + 5` calcula la "
                "suma e imprime `12`.\n\n"
                "Y las variables se escriben sin comillas, porque son valores, no textos."
            ),
            snippet=(
                "Proceso TextoyNumero\n"
                '\tEscribir "El resultado es:"\n'
                "\tEscribir 7 + 5\n"
                '\tEscribir "7 + 5"\n'
                "FinProceso\n"
            ),
            task="Cambia el + por un - en la tercera linea y comprueba como cambia la salida.",
            tip="Si una frase tiene acentos o espacios, siempre entre comillas.",
        ),
        Step(
            title="Reto: tu presentacion",
            body=(
                "Ahora te toca a ti. Escribe un programa que imprima **exactamente** estas "
                "tres lineas, en este orden:\n\n"
                "```\n"
                "PS Academy\n"
                "Aprende a programar\n"
                "Es muy facil\n"
                "```\n\n"
                "La comparacion es exacta: un espacio de mas o una linea de menos y no cuenta."
            ),
            snippet=(
                "Proceso MiPrimerPrograma\n"
                '\tEscribir "PS Academy"\n'
                '\tEscribir "Aprende a programar"\n'
                '\tEscribir "Es muy facil"\n'
                "FinProceso\n"
            ),
            task="Imprime las tres lineas exactamente iguales a las del enunciado.",
            tip="Tres Escribir, tres lineas. Copia el enunciado sin anadir puntos ni comas.",
        ),
    ],
)

# =====================================================================
# LECCION 2 - Leer: pedirle un dato a quien usa el programa.
# =====================================================================

add(
    key="tut-02",
    category="Tutorial",
    title="Leer: pedir datos",
    description=(
        "Hasta ahora tus programas solo hablan: el mensaje estaba escrito dentro del codigo.\n\n"
        "`Leer` cambia eso: el programa se detiene, espera a que alguien escriba una "
        "respuesta y la guarda en una variable. Asi nasce un programa que se adapta al "
        "que lo usa.\n\n"
        "En esta leccion ves que es `Definir`, como se combinan `Leer` y `Escribir` y por "
        "que el orden de las preguntas importa."
    ),
    difficulty="Easy",
    inputs=[i_text("Ana"), i_text("Medellin")],
    stdin="Ana\nMedellin\n",
    starter={
        "python": (
            "# Lee el nombre y la ciudad (pista: input())\n"
            "# y muestra las dos lineas que pide el enunciado.\n"
            "nombre = input()\n"
            "ciudad = input()\n"
        ),
        "java": java_program(
            "String nombre = sc.nextLine();",
            "String ciudad = sc.nextLine();",
            "// Muestra aqui las dos lineas que pide el enunciado.",
        ),
        "pseint": (
            "Proceso Presentacion\n"
            "\tDefinir nombre Como Cadena\n"
            "\tDefinir ciudad Como Cadena\n"
            "\tLeer nombre\n"
            "\tLeer ciudad\n"
            "\t// Muestra aqui las dos lineas que pide el enunciado.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            'nombre = input()\n'
            'ciudad = input()\n'
            'print("Hola, " + nombre)\n'
            'print("Bienvenido a PS Academy desde " + ciudad)\n'
        ),
        "java": java_program(
            "String nombre = sc.nextLine();",
            "String ciudad = sc.nextLine();",
            'System.out.println("Hola, " + nombre);',
            'System.out.println("Bienvenido a PS Academy desde " + ciudad);',
        ),
        "pseint": (
            "Proceso Presentacion\n"
            "\tDefinir nombre Como Cadena\n"
            "\tDefinir ciudad Como Cadena\n"
            "\tLeer nombre\n"
            "\tLeer ciudad\n"
            '\tEscribir "Hola, ", nombre\n'
            '\tEscribir "Bienvenido a PS Academy desde ", ciudad\n'
            "FinProceso\n"
        ),
    },
    tutorial=[
        Step(
            title="Leer espera una respuesta",
            body=(
                "`Leer` detiene el programa y espera a que alguien escriba. Lo que se escriba "
                "se guarda en la variable que aparece detras.\n\n"
                "- `Definir nombre Como Cadena` prepara la variable: le dice que va a guardar texto.\n"
                "- `Leer nombre` recibe la respuesta.\n"
                "- `Escribir \"Hola, \", nombre, \"!\"` la muestra usando la coma como separador.\n\n"
                "Ojo con el espacio final de `\"Hola, \"`: sin el, el saludo saldria pegado."
            ),
            snippet=(
                "Proceso PreguntarNombre\n"
                "\tDefinir nombre Como Cadena\n"
                "\tLeer nombre\n"
                '\tEscribir "Hola, ", nombre, "!"\n'
                "FinProceso\n"
            ),
            stdin="Ana\n",
            task="Ejecuta y despues responde con otro nombre cualquiera.",
            tip="Si escribes Ana y sale Hola,  Ana! te falta el espacio final en el primer texto.",
        ),
        Step(
            title="Definir declara el tipo",
            body=(
                "`Definir` no es un adorno: le dice a PSeint que tipo de dato va a guardarse.\n\n"
                "- `Entero` para numeros sin decimales.\n"
                "- `Real` para numeros con decimales.\n"
                "- `Cadena` para texto.\n"
                "- `Logico` para Verdadero o Falso.\n\n"
                "Asi el programa sabe como convertir lo que escribe la persona y avisa antes de "
                "continuar si el dato no encaja."
            ),
            snippet=(
                "Proceso Tipos\n"
                "\tDefinir nombre Como Cadena\n"
                "\tDefinir anios Como Entero\n"
                "\tLeer nombre\n"
                "\tLeer anios\n"
                '\tEscribir nombre, " nacio hace ", anios, " años"\n'
                "FinProceso\n"
            ),
            stdin="Luis\n20\n",
            task="Cambia Entero por Real y vuelve a ejecutar con 20: fíjate en el .0 final.",
            tip="Un Real con valor entero se muestra 20.0. Asi se ve por que conviene Entero.",
        ),
        Step(
            title="El orden de las preguntas",
            body=(
                "Cada `Leer` se queda con **la siguiente respuesta**, en el mismo orden en que "
                "aparecen en el codigo. Por eso el primer `Leer` es la primera pregunta que ve "
                "la persona.\n\n"
                "Si cambias el orden de las lineas, tambien cambia el orden en el que se leen "
                "los datos: por eso las variables tienen que seguir correspondiendo con su "
                "respuesta."
            ),
            snippet=(
                "Proceso Orden\n"
                "\tDefinir primero Como Cadena\n"
                "\tDefinir segundo Como Cadena\n"
                "\tLeer primero\n"
                "\tLeer segundo\n"
                '\tEscribir "1: ", primero\n'
                '\tEscribir "2: ", segundo\n'
                "FinProceso\n"
            ),
            stdin="Ana\nMedellin\n",
            task="Intercambia las dos lineas Leer y responde de nuevo: los datos se cruzan.",
            tip="Puedes escribir los dos Leer en una sola linea: Leer primero, segundo.",
        ),
        Step(
            title="Reto: tu presentacion",
            body=(
                "Escribe un programa que lea el nombre y la ciudad de la persona y muestre "
                "exactamente estas dos lineas:\n\n"
                "```\n"
                "Hola, Ana\n"
                "Bienvenido a PS Academy desde Medellin\n"
                "```\n\n"
                "Los valores de ejemplo son los que veras en la consola; con otros datos el "
                "programa cambia la primera linea y el final de la segunda."
            ),
            snippet=(
                "Proceso Presentacion\n"
                "\tDefinir nombre Como Cadena\n"
                "\tDefinir ciudad Como Cadena\n"
                "\tLeer nombre\n"
                "\tLeer ciudad\n"
                '\tEscribir "Hola, ", nombre\n'
                '\tEscribir "Bienvenido a PS Academy desde ", ciudad\n'
                "FinProceso\n"
            ),
            stdin="Ana\nMedellin\n",
            task="Muestra las dos lineas usando el nombre y la ciudad leidos.",
            tip="El segundo Escribir lleva texto fijo al principio y al final, y la ciudad en medio.",
        ),
    ],
)

# =====================================================================
# LECCION 3 - Variables: cajas con nombre donde guardamos valores.
# =====================================================================

add(
    key="tut-03",
    category="Tutorial",
    title="Variables: cajas con nombre",
    description=(
        "Una variable es una caja con nombre. Le guardas un valor, luego puedes leerlo, "
        "sumarlo o cambiarlo tantas veces como quieras.\n\n"
        "En esta leccion ves como se declara, como se le guarda un valor con la flecha "
        "`<-` y como se usan varias variables juntas en una cuenta.\n\n"
        "Las variables son el alma de la programacion: sin ellas, un programa solo puede "
        "repetir lo mismo que esta escrito."
    ),
    difficulty="Easy",
    inputs=[],
    starter={
        "python": (
            "# Declara tres notas (4, 8 y 7) y muestra su suma.\n"
            "# Pista: una variable es un nombre y un valor, por ejemplo precio = 1250.\n"
        ),
        "java": java_program(
            "// Declara aqui las tres notas y muestra su suma.",
        ),
        "pseint": (
            "Proceso SumaNotas\n"
            "\tDefinir nota1 Como Entero\n"
            "\t// Declara nota2 y nota3, guardales 8 y 7, y muestra la suma.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": "nota1 = 4\nnota2 = 8\nnota3 = 7\nprint(nota1 + nota2 + nota3)\n",
        "java": java_program(
            "int nota1 = 4;",
            "int nota2 = 8;",
            "int nota3 = 7;",
            "System.out.println(nota1 + nota2 + nota3);",
        ),
        "pseint": (
            "Proceso SumaNotas\n"
            "\tDefinir nota1 Como Entero\n"
            "\tDefinir nota2 Como Entero\n"
            "\tDefinir nota3 Como Entero\n"
            "\tnota1 <- 4\n"
            "\tnota2 <- 8\n"
            "\tnota3 <- 7\n"
            "\tEscribir nota1 + nota2 + nota3\n"
            "FinProceso\n"
        ),
    },
    tutorial=[
        Step(
            title="Crear una variable y guardarle un valor",
            body=(
                "Hay dos pasos que conviene no mezclar:\n\n"
                "- `Definir edad Como Entero` **crea** la caja y dice que guarda un numero entero.\n"
                "- `edad <- 15` **guarda** el valor 15 dentro.\n\n"
                "Despues, `Escribir edad` muestra lo que hay dentro. Cuando escribes el nombre "
                "de la variable, sin comillas, el ordenador busca que hay dentro."
            ),
            snippet=(
                "Proceso Variables\n"
                "\tDefinir edad Como Entero\n"
                "\tedad <- 15\n"
                "\tEscribir edad\n"
                "FinProceso\n"
            ),
            task="Cambia el 15 por otro numero y ejecuta: la salida cambia sin tocar nada mas.",
            tip="Si escribes Escribir \"edad\" saldra la palabra edad, no su valor.",
        ),
        Step(
            title="La flecha guarda, el igual compara",
            body=(
                "La flecha `<-` se lee como un **empuja**: lo de la derecha entra en la caja de "
                "la izquierda.\n\n"
                "Por eso `precio <- precio + 50` funciona: primero calcula el precio + 50 y "
                "despues guarda ese resultado en la misma variable. El valor anterior se "
                "reemplaza.\n\n"
                "El signo `=` sirve para comparar, no para guardar. Ya lo veras en las "
                "condicionales."
            ),
            snippet=(
                "Proceso LaFlecha\n"
                "\tDefinir precio Como Entero\n"
                "\tprecio <- 1250\n"
                "\tprecio <- precio + 50\n"
                "\tEscribir precio\n"
                "FinProceso\n"
            ),
            task="Anade una linea mas que reste 200 y comprueba el nuevo precio.",
            tip="El orden importa: guarda primero, despues opera sobre lo guardado.",
        ),
        Step(
            title="Dos variables en la misma cuenta",
            body=(
                "Aqui es donde las variables empiezan a ahorrar trabajo: en vez de escribir "
                "1300 * 4 en el papel, guardamos cada dato con su nombre.\n\n"
                "`Escribir precio * cantidad` usa las dos variables, hace la cuenta y muestra "
                "solo el resultado. Si mañana el precio cambia, no tocas la cuenta."
            ),
            snippet=(
                "Proceso Calculo\n"
                "\tDefinir precio Como Entero\n"
                "\tDefinir cantidad Como Entero\n"
                "\tprecio <- 1300\n"
                "\tcantidad <- 4\n"
                "\tEscribir precio * cantidad\n"
                "FinProceso\n"
            ),
            task="Cambia la cantidad a 5 y ejecuta otra vez.",
            tip="Los nombres de variable se escriben sin comillas y sin tildes.",
        ),
        Step(
            title="Reto: suma de notas",
            body=(
                "Crea tres variables `nota1`, `nota2` y `nota3`, guardales los valores 4, 8 y "
                "7, y muestra su suma:\n\n"
                "```\n"
                "19\n"
                "```\n\n"
                "El enunciado pide el resultado en una sola linea, asi que tambien hace falta "
                "una sola linea de Escribir."
            ),
            snippet=(
                "Proceso SumaNotas\n"
                "\tDefinir nota1 Como Entero\n"
                "\tDefinir nota2 Como Entero\n"
                "\tDefinir nota3 Como Entero\n"
                "\tnota1 <- 4\n"
                "\tnota2 <- 8\n"
                "\tnota3 <- 7\n"
                "\tEscribir nota1 + nota2 + nota3\n"
                "FinProceso\n"
            ),
            task="Muestra 19 usando las tres variables.",
            tip="Un Escribir con tres variables sumadas entre si: Escribir nota1 + nota2 + nota3.",
        ),
    ],
)

# =====================================================================
# LECCION 4 - Unir todo: leer, calcular e imprimir.
# =====================================================================

add(
    key="tut-04",
    category="Tutorial",
    title="Combinar: leer, calcular e imprimir",
    description=(
        "Ya sabes pedir un dato, guardarlo en una variable y mostrar un resultado. Este es "
        "precisamente el esqueleto de un programa.\n\n"
        "En esta leccion los tres ingredientes a la vez: leer, operar con `+ - * /` e "
        "imprimir el resultado. Al final dareas con un programa de verdad: el promedio de "
        "dos notas."
    ),
    difficulty="Easy",
    inputs=[i_number(12), i_number(8), i_number(5)],
    stdin="12\n8\n5\n",
    starter={
        "python": (
            "# Lee los tres numeros (pista: int(input()))\n"
            "a = int(input())\n"
            "b = int(input())\n"
            "c = int(input())\n"
            "# Muestra la suma y despues el doble de la suma.\n"
        ),
        "java": java_program(
            "int a = sc.nextInt();",
            "int b = sc.nextInt();",
            "int c = sc.nextInt();",
            "// Muestra aqui la suma y despues el doble de la suma.",
        ),
        "pseint": (
            "Proceso SumaTotal\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir c Como Entero\n"
            "\tLeer a\n"
            "\tLeer b\n"
            "\tLeer c\n"
            "\t// Muestra aqui la suma y despues el doble de la suma.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": "a = int(input())\nb = int(input())\nc = int(input())\nprint(a + b + c)\nprint((a + b + c) * 2)\n",
        "java": java_program(
            "int a = sc.nextInt();",
            "int b = sc.nextInt();",
            "int c = sc.nextInt();",
            "System.out.println(a + b + c);",
            "System.out.println((a + b + c) * 2);",
        ),
        "pseint": (
            "Proceso SumaTotal\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir c Como Entero\n"
            "\tLeer a\n"
            "\tLeer b\n"
            "\tLeer c\n"
            "\tEscribir a + b + c\n"
            "\tEscribir (a + b + c) * 2\n"
            "FinProceso\n"
        ),
    },
    tutorial=[
        Step(
            title="Pedir dos numeros y sumarlos",
            body=(
                "Este es el programa mas frecuente de la programacion: pedir dos datos, "
                "sumarlos y mostrar el resultado.\n\n"
                "Fijate en el orden: primero las dos variables, despues los dos `Leer`, y "
                "al final el `Escribir` con la cuenta. Cada dato se guarda en la variable "
                "correcta."
            ),
            snippet=(
                "Proceso DosNumeros\n"
                "\tDefinir a Como Entero\n"
                "\tDefinir b Como Entero\n"
                "\tLeer a\n"
                "\tLeer b\n"
                "\tEscribir a + b\n"
                "FinProceso\n"
            ),
            stdin="15\n25\n",
            task="Ejecuta con 15 y 25. Despues prueba con otros dos numeros.",
            tip="Sin Definir tambien funciona, pero declararlas documenta el tipo y evita errores.",
        ),
        Step(
            title="Los cuatro operadores",
            body=(
                "- `+` suma\n"
                "- `-` resta\n"
                "- `*` multiplica\n"
                "- `/` divide\n\n"
                "Sobre el signo de la division hay que tener cuidado: si divides dos enteros y "
                "el resultado no es exacto, `Trunc(...)` se queda con la parte entera "
                "(10 entre 3 son 3, no 3 con decimales). Asi el resultado es el mismo que en "
                "la division en la escuela."
            ),
            snippet=(
                "Proceso Operadores\n"
                "\tDefinir a Como Entero\n"
                "\ta <- 12\n"
                "\tEscribir a + 3\n"
                "\tEscribir a - 3\n"
                "\tEscribir a * 3\n"
                "\tEscribir Trunc(a / 3)\n"
                "FinProceso\n"
            ),
            task="Cambia a <- 12 por a <- 20 y ejecuta: los cuatro resultados cambian.",
            tip="Los parentesis mandan: Escribir (a + b) * 2 no es lo mismo que Escribir a + b * 2.",
        ),
        Step(
            title="Un programa completo",
            body=(
                "Aqui juntas texto y numeros en el mismo `Escribir`. La coma separa las "
                "partes y el ordenador las pega sin dejar espacios, asi que los textos fijos "
                "llevan el espacio justo donde lo necesitas.\n\n"
                "`Trunc((nota1 + nota2) / 2)` calcula el promedio sin los decimales, "
                "que es justo lo que se espera de una nota."
            ),
            snippet=(
                "Proceso Promedio\n"
                "\tDefinir nombre Como Cadena\n"
                "\tDefinir nota1 Como Entero\n"
                "\tDefinir nota2 Como Entero\n"
                "\tLeer nombre\n"
                "\tLeer nota1\n"
                "\tLeer nota2\n"
                '\tEscribir nombre, " obtuvo ", Trunc((nota1 + nota2) / 2), " puntos"\n'
                "FinProceso\n"
            ),
            stdin="Ana\n8\n6\n",
            task="Ejecuta con Ana, 8 y 6. Luego prueba con otras dos notas.",
            tip="Si sale Ana obtuvo 7.0 puntos, el problema es que nota1 se guarde como Real.",
        ),
        Step(
            title="Reto: total y doble",
            body=(
                "Lee tres numeros enteros y muestra dos lineas: su suma y el doble de esa "
                "suma.\n\n"
                "```\n"
                "25\n"
                "50\n"
                "```\n\n"
                "Puedes calcular la suma dos veces en los dos Escribir: el ordenador la resuelve "
                "al instante."
            ),
            snippet=(
                "Proceso SumaTotal\n"
                "\tDefinir a Como Entero\n"
                "\tDefinir b Como Entero\n"
                "\tDefinir c Como Entero\n"
                "\tLeer a\n"
                "\tLeer b\n"
                "\tLeer c\n"
                "\tEscribir a + b + c\n"
                "\tEscribir (a + b + c) * 2\n"
                "FinProceso\n"
            ),
            stdin="12\n8\n5\n",
            task="Muestra la suma y el doble, cada uno en su linea.",
            tip="El doble de la suma necesita parentesis: (a + b + c) * 2.",
        ),
    ],
)

# =====================================================================
# LECCION 5 - Repaso final y despedida: ya puedes empezar el curso.
# =====================================================================

add(
    key="tut-05",
    category="Tutorial",
    title="Ya estas listo para empezar tu camino",
    description=(
        "Ultima leccion del tutorial: un repaso rapido de todo lo visto y un primer "
        "programa completo hecho por ti.\n\n"
        "Si has llegado aqui ya sabes declarar una variable, pedir un dato con `Leer`, "
        "guardarlo y mostrarlo con `Escribir`. Ese es el esqueleto de **todos** los "
        "ejercicios del curso.\n\n"
        "Cuando superes el reto, el siguiente paso son los ejercicios de secuenciales: "
        "empezaran a pedirte cosas, pero nada que no sepas ya hacer."
    ),
    difficulty="Easy",
    inputs=[i_text("Ana"), i_number(15)],
    stdin="Ana\n15\n",
    starter={
        "python": (
            "# Lee el nombre y la edad, y muestra los dos saludos del enunciado.\n"
            "nombre = input()\n"
            "edad = input()\n"
            "# Pista: convierte la edad con int() para poder sumarla.\n"
        ),
        "java": java_program(
            "String nombre = sc.nextLine();",
            "int edad = sc.nextInt();",
            "// Muestra aqui los dos saludos del enunciado.",
        ),
        "pseint": (
            "Proceso MiPrimerProgramaCompleto\n"
            "\tDefinir nombre Como Cadena\n"
            "\t// Lee la edad y muestra los dos saludos del enunciado.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            'nombre = input()\n'
            'edad = int(input())\n'
            'print("Hola, " + nombre)\n'
            'print("Tienes " + str(edad) + " años")\n'
        ),
        "java": java_program(
            "String nombre = sc.nextLine();",
            "int edad = sc.nextInt();",
            'System.out.println("Hola, " + nombre);',
            'System.out.println("Tienes " + edad + " años");',
        ),
        "pseint": (
            "Proceso MiPrimerProgramaCompleto\n"
            "\tDefinir nombre Como Cadena\n"
            "\tDefinir edad Como Entero\n"
            "\tLeer nombre\n"
            "\tLeer edad\n"
            '\tEscribir "Hola, ", nombre\n'
            '\tEscribir "Tienes ", edad, " años"\n'
            "FinProceso\n"
        ),
    },
    tutorial=[
        Step(
            title="Repaso: imprimir mensajes",
            body=(
                "Antes de pedir nada, recuerda como se imprime: `Escribir` con el texto entre "
                "comillas.\n\n"
                "Tambien puedes guardar un texto en una variable de tipo `Cadena` y usarla "
                "despues, como hace este paso. Las dos formas sirven; la primera es mas corta "
                "y la segunda mas comoda si el texto se repite."
            ),
            snippet=(
                "Proceso Repaso1\n"
                "\tDefinir mensaje Como Cadena\n"
                '\tmensaje <- "Bienvenido"\n'
                '\tEscribir mensaje, " a PS Academy"\n'
                '\tEscribir "Este programa solo muestra texto."\n'
                "FinProceso\n"
            ),
            task="Ejecuta el programa y despues cambia el texto que aparece.",
            tip="Sin Leer ni variables complicadas: aqui solo hay Escribir.",
        ),
        Step(
            title="Repaso: leer datos",
            body=(
                "`Leer` se encarga de la parte aburrida: esperar a que la persona escriba.\n\n"
                "El programa no avanza hasta que hay una respuesta, y cada `Leer` se lleva una "
                "linea de lo que se escribio. Esa respuesta queda guardada en la variable "
                "indicada, lista para usarla cuando quieras."
            ),
            snippet=(
                "Proceso Repaso2\n"
                "\tDefinir nombre Como Cadena\n"
                "\tDefinir ciudad Como Cadena\n"
                "\tLeer nombre\n"
                "\tLeer ciudad\n"
                '\tEscribir "Hola, ", nombre\n'
                '\tEscribir "Bienvenido a ", ciudad\n'
                "FinProceso\n"
            ),
            stdin="Ana\nMedellin\n",
            task="Ejecuta y responde con un nombre y una ciudad distintos.",
            tip="Dos Leer, dos lineas de respuesta: la primera para nombre y la segunda para ciudad.",
        ),
        Step(
            title="Repaso: calcular con lo leido",
            body=(
                "Cuando el dato leido es un numero, se declara como `Entero` y se puede "
                "operar con el.\n\n"
                "Aqui el programa calcula la edad que tendra el ano que viene. Es el primer "
                "ejercicio donde el resultado depende de algo que escribio la persona, y "
                "tambien donde se ve que una variable puede guardar el resultado de una cuenta."
            ),
            snippet=(
                "Proceso Repaso3\n"
                "\tDefinir nombre Como Cadena\n"
                "\tDefinir edad Como Entero\n"
                "\tDefinir edadProxima Como Entero\n"
                "\tLeer nombre\n"
                "\tLeer edad\n"
                "\tedadProxima <- edad + 1\n"
                '\tEscribir nombre, " cumple ", edad, " años"\n'
                '\tEscribir "El año que viene cumplir ", edadProxima\n'
                "FinProceso\n"
            ),
            stdin="Ana\n15\n",
            task="Ejecuta con otra edad y comprueba como cambia el calculo.",
            tip="La flecha guarda el resultado de la cuenta en edadProxima, que luego se muestra.",
        ),
        Step(
            title="Ya estas listo para empezar tu camino",
            body=(
                "En una sola leccion has aprendido a escribir un programa completo: declarar "
                "variables, pedir datos, guardarlos y mostrar resultados.\n\n"
                "Eso es **todo** lo que necesita un programa secuencial. A partir de aqui los "
                "ejercicios ya no te ensenan comandos nuevos: te dan un problema y tu escribes "
                "el codigo.\n\n"
                "Cuando superes este paso, se desbloqueara el siguiente bloque del curso "
                "(Fundamentos, donde empiezan los ejercicios de secuenciales). Mucho animo, "
                "y a escribir codigo."
            ),
            snippet=(
                "Proceso MiPrimerProgramaCompleto\n"
                "\tDefinir nombre Como Cadena\n"
                "\tDefinir edad Como Entero\n"
                "\tLeer nombre\n"
                "\tLeer edad\n"
                '\tEscribir "Hola, ", nombre\n'
                '\tEscribir "Tienes ", edad, " años"\n'
                "FinProceso\n"
            ),
            stdin="Ana\n15\n",
            task="Reune todo lo aprendido en un programa que imprima los dos saludos.",
            tip="Es el mismo esquema del paso anterior: definir, leer, calcular si hace falta y escribir.",
        ),
    ],
)