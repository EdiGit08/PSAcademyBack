"""
Ejercicios 1-15 de PS Academy (parte 1 de 2).

Cada ejercicio define:
  key         identificador unico (para depurar)
  category    nombre de la categoria
  title       titulo
  description enunciado en espanol
  difficulty  Easy | Medium | Hard
  inputs      valores del "Leer" (Value + ValueType)
  starter     codigo inicial por lenguaje (python, java, pseint)
  solutions   solucion de referencia por lenguaje (se ejecuta para verificar)
  stdin       texto exacto que Piston entregara por stdin

Regla de diseno importante: los ejercicios evitan la division que produce decimales.
En Python 3, 10/2 da 5.0 mientras que en Java da 5 y en PSeint da 5. Cuando un
ejercicio necesita dividir, las tres soluciones fuerzan entero de forma explicita
(// en Python, division entera en Java, Trunc en PSeint) para que la salida coincida.
"""

from __future__ import annotations

from build_exercises import Exercise, i_number, i_text

# --------------------------------------------------------------- Plantillas


def java_stub(result: str, *body: str) -> str:
    """Envoltura de main() en Java que al final imprime el resultado indicado."""
    inner = "\n".join(("        " + l.strip()) if l.strip() else "" for l in body)
    return (
        "import java.util.Scanner;\n\n"
        "public class Main {\n\n"
        "    static Scanner sc = new Scanner(System.in);\n\n"
        "    public static void main(String[] args) {\n"
        f"{inner}\n"
        f"        System.out.println({result});\n"
        "    }\n"
        "}\n"
    )


EXERCISES: list[Exercise] = []


def add(**kwargs) -> None:
    EXERCISES.append(Exercise(**kwargs))


# =========================================================== FUNDAMENTOS
# Variables, tipos, operadores y lectura/escritura.

add(
    key="fund-01",
    category="Fundamentos",
    title="Saludo con tu nombre",
    description=(
        "Pide el nombre de la persona por pantalla y muestra un saludo personalized.\n\n"
        "Ejemplo: si la persona escribe 'Ana', el programa muestra:\n"
        "Hola, Ana!"
    ),
    difficulty="Easy",
    inputs=[i_text("Ana")],
    stdin="Ana\n",
    starter={
        "python": "# Lee el nombre y muestra el saludo.\n# Pista: input() lee lo que la persona escribe.\n",
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        String nombre = sc.nextLine();\n"
            "        // Escribe aqui el saludo: System.out.println(\"Hola, \" + nombre + \"!\");\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Saludo\n"
            "\tDefinir nombre Como Cadena\n"
            "\tLeer nombre\n"
            "\t// Muestra aqui el saludo, sin comillas dobles para las variables:\n"
            "\t// Escribir \"Hola, \", nombre, \"!\"\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": 'nombre = input()\nprint("Hola, " + nombre + "!")\n',
        "java": java_stub('"Hola, " + nombre + "!"', "String nombre = sc.nextLine();"),
        "pseint": (
            "Proceso Saludo\n"
            "\tDefinir nombre Como Cadena\n"
            "\tLeer nombre\n"
            "\tEscribir \"Hola, \", nombre, \"!\"\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-02",
    category="Fundamentos",
    title="Suma de dos numeros",
    description=(
        "Pide dos numeros enteros y muestra su suma.\n\n"
        "Ejemplo: si la persona escribe 15 y luego 25, el programa muestra:\n"
        "40"
    ),
    difficulty="Easy",
    inputs=[i_number(15), i_number(25)],
    stdin="15\n25\n",
    starter={
        "python": "# Lee los dos numeros (pista: input() devuelve texto, conviertela con int())\n# y muestra la suma.\n",
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        int suma = a + b;\n"
            "        // Muestra aqui la suma: System.out.println(suma);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Suma\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir suma Como Entero\n"
            "\tLeer a, b\n"
            "\t// Calcula aqui la suma y muestrala con Escribir suma\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": "a = int(input())\nb = int(input())\nresultado = a + b\nprint(resultado)\n",
        "java": java_stub("a + b", "int a = sc.nextInt();", "int b = sc.nextInt();"),
        "pseint": (
            "Proceso Suma\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir suma Como Entero\n"
            "\tLeer a, b\n"
            "\tsuma <- a + b\n"
            "\tEscribir suma\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-03",
    category="Fundamentos",
    title="Area de un rectangulo",
    description=(
        "El area de un rectangulo es base por altura.\n\n"
        "Pide la base y la altura, y muestra el area.\n\n"
        "Ejemplo: base 4 y altura 7 dan un area de 28."
    ),
    difficulty="Easy",
    inputs=[i_number(4), i_number(7)],
    stdin="4\n7\n",
    starter={
        "python": "# Pide la base y la altura, y muestra base * altura.\n",
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int base = sc.nextInt();\n"
            "        int altura = sc.nextInt();\n"
            "        // Calcula aqui el area y muestrala con System.out.println\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso AreaRectangulo\n"
            "\tDefinir base Como Entero\n"
            "\tDefinir altura Como Entero\n"
            "\tDefinir area Como Entero\n"
            "\tLeer base, altura\n"
            "\t// Calcula aqui el area y muestrala con Escribir area\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": "base = int(input())\naltura = int(input())\narea = base * altura\nprint(area)\n",
        "java": java_stub("base * altura", "int base = sc.nextInt();", "int altura = sc.nextInt();"),
        "pseint": (
            "Proceso AreaRectangulo\n"
            "\tDefinir base Como Entero\n"
            "\tDefinir altura Como Entero\n"
            "\tDefinir area Como Entero\n"
            "\tLeer base, altura\n"
            "\tarea <- base * altura\n"
            "\tEscribir area\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-04",
    category="Fundamentos",
    title="Perimetro de un rectangulo",
    description=(
        "El perimetro de un rectangulo es el resultado de sumar base + altura dos veces.\n\n"
        "Ejemplo: base 6 y altura 9 dan un perimetro de 30."
    ),
    difficulty="Easy",
    inputs=[i_number(6), i_number(9)],
    stdin="6\n9\n",
    starter={
        "python": "# El perimetro es (base + altura) * 2\n",
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int base = sc.nextInt();\n"
            "        int altura = sc.nextInt();\n"
            "        // El perimetro es (base + altura) * 2. Calculalo y muestralo.\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso PerimetroRectangulo\n"
            "\tDefinir base Como Entero\n"
            "\tDefinir altura Como Entero\n"
            "\tDefinir perimetro Como Entero\n"
            "\tLeer base, altura\n"
            "\t// Calcula aqui el perimetro y muestralo con Escribir perimetro\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": "base = int(input())\naltura = int(input())\nperimetro = (base + altura) * 2\nprint(perimetro)\n",
        "java": java_stub("(base + altura) * 2", "int base = sc.nextInt();", "int altura = sc.nextInt();"),
        "pseint": (
            "Proceso PerimetroRectangulo\n"
            "\tDefinir base Como Entero\n"
            "\tDefinir altura Como Entero\n"
            "\tDefinir perimetro Como Entero\n"
            "\tLeer base, altura\n"
            "\tperimetro <- (base + altura) * 2\n"
            "\tEscribir perimetro\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-05",
    category="Fundamentos",
    title="Cambiar el contenido de dos variables",
    description=(
        "Pide dos palabras y muestra como quedarian despues de intercambiarlas.\n\n"
        "Si la persona escribe 'hola' y 'adios', el programa muestra:\n"
        "Primero: hola, Segundo: adios\n\n"
        "En Python basta con a, b = b, a. En Java y PSeint guarda el valor de a en una\n"
        "variable auxiliar antes de sobrescribirla."
    ),
    difficulty="Medium",
    inputs=[i_text("hola"), i_text("adios")],
    stdin="hola\nadios\n",
    starter={
        "python": (
            "# Truco: a, b = b, a intercambia las dos variables.\n"
            "# Muestra el resultado con print(f\"Primero: {b}, Segundo: {a}\")\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        String a = sc.nextLine();\n"
            "        String b = sc.nextLine();\n"
            "        // Intercambia a y b guardando antes el valor de a en una auxiliar.\n"
            "        System.out.println(\"Primero: \" + b + \", Segundo: \" + a);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Intercambio\n"
            "\tDefinir a Como Cadena\n"
            "\tDefinir b Como Cadena\n"
            "\tDefinir aux Como Cadena\n"
            "\tLeer a, b\n"
            "\t// Intercambia el contenido de a y b usando aux (tres asignaciones)\n"
            "\tEscribir \"Primero: \", b, \", Segundo: \", a\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": 'a = input()\nb = input()\na, b = b, a\nprint(f"Primero: {b}, Segundo: {a}")\n',
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        String a = sc.nextLine();\n"
            "        String b = sc.nextLine();\n"
            "        String aux = a;\n"
            "        a = b;\n"
            "        b = aux;\n"
            "        System.out.println(\"Primero: \" + b + \", Segundo: \" + a);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Intercambio\n"
            "\tDefinir a Como Cadena\n"
            "\tDefinir b Como Cadena\n"
            "\tDefinir aux Como Cadena\n"
            "\tLeer a, b\n"
            "\taux <- a\n"
            "\ta <- b\n"
            "\tb <- aux\n"
            "\tEscribir \"Primero: \", b, \", Segundo: \", a\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-06",
    category="Fundamentos",
    title="Promedio de tres notas",
    description=(
        "Pide tres notas enteras y muestra su promedio, sin decimales.\n\n"
        "Ejemplo: 15, 18 y 12 dan un promedio de 15.\n\n"
        "Atencion: si la division te da decimales, muestra solo la parte entera."
    ),
    difficulty="Medium",
    inputs=[i_number(15), i_number(18), i_number(12)],
    stdin="15\n18\n12\n",
    starter={
        "python": "# La division entera en Python es //\n# Ejemplo: 45 // 3 da 15\n",
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n1 = sc.nextInt();\n"
            "        int n2 = sc.nextInt();\n"
            "        int n3 = sc.nextInt();\n"
            "        // Con enteros, en Java la division ya es entera\n"
            "        // Calcula el promedio y muestralo con System.out.println\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Promedio\n"
            "\tDefinir n1 Como Entero\n"
            "\tDefinir n2 Como Entero\n"
            "\tDefinir n3 Como Entero\n"
            "\tDefinir promedio Como Entero\n"
            "\tLeer n1, n2, n3\n"
            "\t// Calcula aqui el promedio (puedes usar Trunc) y muestralo\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "n1 = int(input())\nn2 = int(input())\nn3 = int(input())\n"
            "promedio = (n1 + n2 + n3) // 3\nprint(promedio)\n"
        ),
        "java": java_stub("(n1 + n2 + n3) / 3", "int n1 = sc.nextInt();", "int n2 = sc.nextInt();", "int n3 = sc.nextInt();"),
        "pseint": (
            "Proceso Promedio\n"
            "\tDefinir n1 Como Entero\n"
            "\tDefinir n2 Como Entero\n"
            "\tDefinir n3 Como Entero\n"
            "\tDefinir promedio Como Entero\n"
            "\tLeer n1, n2, n3\n"
            "\tpromedio <- Trunc((n1 + n2 + n3) / 3)\n"
            "\tEscribir promedio\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-07",
    category="Fundamentos",
    title="Mayusculas y minusculas de un texto",
    description=(
        "Pide un texto y muestralo dos veces: primero en mayusculas y luego en minusculas,\n"
        "cada version en su propia linea.\n\n"
        "Ejemplo: con 'Hola Mundo' el programa muestra:\n"
        "HOLA MUNDO\n"
        "hola mundo"
    ),
    difficulty="Medium",
    inputs=[i_text("Hola Mundo")],
    stdin="Hola Mundo\n",
    starter={
        "python": (
            "# Pista: upper() convierte a mayusculas y lower() a minusculas.\n"
            "# Muestra cada version con un print.\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        String texto = sc.nextLine();\n"
            "        // Pista: texto.toUpperCase() y texto.toLowerCase()\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso MayusMinus\n"
            "\tDefinir texto Como Cadena\n"
            "\tLeer texto\n"
            "\t// Muestra aqui Mayusculas(texto) y luego Minusculas(texto)\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "texto = input()\nprint(texto.upper())\nprint(texto.lower())\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        String texto = sc.nextLine();\n"
            "        System.out.println(texto.toUpperCase());\n"
            "        System.out.println(texto.toLowerCase());\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso MayusMinus\n"
            "\tDefinir texto Como Cadena\n"
            "\tLeer texto\n"
            "\tEscribir Mayusculas(texto)\n"
            "\tEscribir Minusculas(texto)\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-08",
    category="Fundamentos",
    title="Cociente y resto de una division",
    description=(
        "Pide dos numeros enteros y muestra, en dos lineas, el cociente y el resto de\n"
        "la division entre ellos.\n\n"
        "Ejemplo: al dividir 17 entre 5 el programa muestra:\n"
        "3\n"
        "2\n\n"
        "El cociente se muestra sin decimales: 3 y no 3.4."
    ),
    difficulty="Medium",
    inputs=[i_number(17), i_number(5)],
    stdin="17\n5\n",
    starter={
        "python": (
            "# Pista: la division entera es // y el resto es %.\n"
            "# cociente = a // b   y   resto = a % b\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        // Con enteros a / b ya es el cociente y a % b el resto\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Division\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir cociente Como Entero\n"
            "\tDefinir resto Como Entero\n"
            "\tLeer a, b\n"
            "\t// Pista: Trunc(a / b) da el cociente y Mod(a, b) el resto\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "a = int(input())\nb = int(input())\n"
            "cociente = a // b\nresto = a % b\nprint(cociente)\nprint(resto)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        int cociente = a / b;\n"
            "        int resto = a % b;\n"
            "        System.out.println(cociente);\n"
            "        System.out.println(resto);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Division\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir cociente Como Entero\n"
            "\tDefinir resto Como Entero\n"
            "\tLeer a, b\n"
            "\tcociente <- Trunc(a / b)\n"
            "\tresto <- Mod(a, b)\n"
            "\tEscribir cociente\n"
            "\tEscribir resto\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-09",
    category="Fundamentos",
    title="Costo total de una compra",
    description=(
        "Pide el precio de un producto y el porcentaje de descuento, y muestra el precio final.\n\n"
        "Ejemplo: un producto de 5000 con 20 de descuento se queda en 4000.\n\n"
        "El resultado debe mostrarse como numero entero."
    ),
    difficulty="Hard",
    inputs=[i_number(5000), i_number(20)],
    stdin="5000\n20\n",
starter={
        "python": (
            "# descuento = precio * porcentaje / 100\n"
            "# Ojo: en Python / da decimales. Usa // para quedarte con la parte entera.\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int precio = sc.nextInt();\n"
            "        int porcentaje = sc.nextInt();\n"
            "        // Calcula el descuento y el precio final, y muestralos\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Compra\n"
            "\tDefinir precio Como Entero\n"
            "\tDefinir porcentaje Como Entero\n"
            "\tDefinir descuento Como Entero\n"
            "\tDefinir total Como Entero\n"
            "\tLeer precio, porcentaje\n"
            "\t// Calcula aqui el descuento y el total, y muestralos\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "precio = int(input())\nporcentaje = int(input())\n"
            "descuento = precio * porcentaje // 100\ntotal = precio - descuento\nprint(total)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int precio = sc.nextInt();\n"
            "        int porcentaje = sc.nextInt();\n"
            "        int descuento = precio * porcentaje / 100;\n"
            "        int total = precio - descuento;\n"
            "        System.out.println(total);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Compra\n"
            "\tDefinir precio Como Entero\n"
            "\tDefinir porcentaje Como Entero\n"
            "\tDefinir descuento Como Entero\n"
            "\tDefinir total Como Entero\n"
            "\tLeer precio, porcentaje\n"
            "\tdescuento <- Trunc(precio * porcentaje / 100)\n"
            "\ttotal <- precio - descuento\n"
            "\tEscribir total\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fund-10",
    category="Fundamentos",
    title="Conversion de dolares a pesos",
    description=(
        "Pide una cantidad en dolares y la tasa de cambio, y muestra el equivalente en pesos.\n\n"
        "Ejemplo: 50 dolares con una tasa de 420 dan 21000 pesos.\n\n"
        "Muestra el resultado como numero entero."
    ),
    difficulty="Hard",
    inputs=[i_number(50), i_number(420)],
    stdin="50\n420\n",
    starter={
        "python": (
            "# pesos = dolares * tasa\n"
            "# Recuerda usar // si necesitas la parte entera.\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int dolares = sc.nextInt();\n"
            "        int tasa = sc.nextInt();\n"
            "        // Calcula aqui los pesos y muestralos con System.out.println\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Conversion\n"
            "\tDefinir dolares Como Entero\n"
            "\tDefinir tasa Como Entero\n"
            "\tDefinir pesos Como Entero\n"
            "\tLeer dolares, tasa\n"
            "\t// Calcula aqui los pesos y muestralos con Escribir pesos\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "dolares = int(input())\ntasa = int(input())\n"
            "pesos = dolares * tasa\nprint(pesos)\n"
        ),
        "java": java_stub(
            "pesos",
            "int dolares = sc.nextInt();",
            "int tasa = sc.nextInt();",
            "int pesos = dolares * tasa;",
        ),
        "pseint": (
            "Proceso Conversion\n"
            "\tDefinir dolares Como Entero\n"
            "\tDefinir tasa Como Entero\n"
            "\tDefinir pesos Como Entero\n"
            "\tLeer dolares, tasa\n"
            "\tpesos <- dolares * tasa\n"
            "\tEscribir pesos\n"
            "FinProceso\n"
        ),
    },
)

# ================================================ ESTRUCTURAS DE CONTROL
# Condicionales, bucles y contadores.

add(
    key="ctrl-01",
    category="Estructuras de control",
    title="Mayor de dos numeros",
    description=(
        "Pide dos numeros y muestra el mayor de los dos.\n\n"
        "Ejemplo: con 10 y 4 el programa muestra:\n"
        "10"
    ),
    difficulty="Easy",
    inputs=[i_number(10), i_number(4)],
    stdin="10\n4\n",
    starter={
        "python": (
            "# Pista: if / elif / else\n"
            "# if a > b:\n"
            "#     mayor = a\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        int mayor = 0;\n"
            "        // Pista: if / else. Guarda en mayor el valor mas grande y muestralo\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Mayor\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir mayor Como Entero\n"
            "\tLeer a, b\n"
            "\t// Compara con Si / Sino y guarda el mayor. Cada bloque va en su linea.\n"
            "\t// Muestra el resultado con Escribir mayor\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "a = int(input())\nb = int(input())\n"
            "if a > b:\n    mayor = a\nelse:\n    mayor = b\nprint(mayor)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        int mayor;\n"
            "        if (a > b) {\n"
            "            mayor = a;\n"
            "        } else {\n"
            "            mayor = b;\n"
            "        }\n"
            "        System.out.println(mayor);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Mayor\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir mayor Como Entero\n"
            "\tLeer a, b\n"
            "\tSi a > b Entonces\n"
            "\t\tmayor <- a\n"
            "\tSino\n"
            "\t\tmayor <- b\n"
            "\tFinSi\n"
            "\tEscribir mayor\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-02",
    category="Estructuras de control",
    title="Numero positivo o negativo",
    description=(
        "Pide un numero y muestra si es Positivo, Negativo o Cero.\n\n"
        "Ejemplo: con -8 el programa muestra:\n"
        "Negativo"
    ),
    difficulty="Easy",
    inputs=[i_number(-8)],
    stdin="-8\n",
    starter={
        "python": (
            "# if numero > 0: ... elif numero < 0: ... else: ...\n"
            "# remember que en Python la comparacion se escribe == y no =\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int numero = sc.nextInt();\n"
            "        String mensaje = \"\";\n"
            "        // Pista: if / else if / else\n"
            "        // Muestra aqui el mensaje con System.out.println(mensaje)\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Signo\n"
            "\tDefinir numero Como Entero\n"
            "\tDefinir mensaje Como Cadena\n"
            "\tLeer numero\n"
            "\t// Anida dos Si: primero mira si es positivo y, en el Sino,\n"
            "\t// comprueba si es negativo. Al final, muestra el mensaje.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "numero = int(input())\n"
            "if numero > 0:\n    mensaje = 'Positivo'\n"
            "elif numero < 0:\n    mensaje = 'Negativo'\n"
            "else:\n    mensaje = 'Cero'\nprint(mensaje)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int numero = sc.nextInt();\n"
            "        String mensaje;\n"
            "        if (numero > 0) {\n"
            "            mensaje = \"Positivo\";\n"
            "        } else if (numero < 0) {\n"
            "            mensaje = \"Negativo\";\n"
            "        } else {\n"
            "            mensaje = \"Cero\";\n"
            "        }\n"
            "        System.out.println(mensaje);\n"
            "    }\n}\n"
        ),
        # El traductor de PSeint no admite 'Sino Si', asi que el Si va anidado.
        "pseint": (
            "Proceso Signo\n"
            "\tDefinir numero Como Entero\n"
            "\tDefinir mensaje Como Cadena\n"
            "\tLeer numero\n"
            "\tSi numero > 0 Entonces\n"
            "\t\tmensaje <- \"Positivo\"\n"
            "\tSino\n"
            "\t\tSi numero < 0 Entonces\n"
            "\t\t\tmensaje <- \"Negativo\"\n"
            "\t\tSino\n"
            "\t\t\tmensaje <- \"Cero\"\n"
            "\t\tFinSi\n"
            "\tFinSi\n"
            "\tEscribir mensaje\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-03",
    category="Estructuras de control",
    title="Numero par o impar",
    description=(
        "Pide un numero y muestra Par si es par, o Impar si es impar.\n\n"
        "Ejemplo: con 7 el programa muestra:\n"
        "Impar"
    ),
    difficulty="Easy",
    inputs=[i_number(7)],
    stdin="7\n",
    starter={
        "python": (
            "# Pista: numero % 2 da 0 cuando el numero es par.\n"
            "# if numero % 2 == 0:\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int numero = sc.nextInt();\n"
            "        String mensaje = \"\";\n"
            "        // Pista: numero % 2 == 0\n"
            "        // Asigna el mensaje y muestralo con System.out.println(mensaje)\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso ParImpar\n"
            "\tDefinir numero Como Entero\n"
            "\tDefinir mensaje Como Cadena\n"
            "\tLeer numero\n"
            "\t// Pista: Mod(numero, 2) da 0 cuando el numero es par.\n"
            "\t// Asigna el mensaje con Si / Sino y muestralo con Escribir mensaje\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "numero = int(input())\n"
            "if numero % 2 == 0:\n    mensaje = 'Par'\nelse:\n    mensaje = 'Impar'\n"
            "print(mensaje)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int numero = sc.nextInt();\n"
            "        String mensaje;\n"
            "        if (numero % 2 == 0) {\n"
            "            mensaje = \"Par\";\n"
            "        } else {\n"
            "            mensaje = \"Impar\";\n"
            "        }\n"
            "        System.out.println(mensaje);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso ParImpar\n"
            "\tDefinir numero Como Entero\n"
            "\tDefinir mensaje Como Cadena\n"
            "\tLeer numero\n"
            "\tSi Mod(numero, 2) == 0 Entonces\n"
            "\t\tmensaje <- \"Par\"\n"
            "\tSino\n"
            "\t\tmensaje <- \"Impar\"\n"
            "\tFinSi\n"
            "\tEscribir mensaje\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-04",
    category="Estructuras de control",
    title="Contar del 1 al N",
    description=(
        "Pide un numero N y muestra los numeros del 1 al N en una sola linea, separados por un espacio.\n\n"
        "Ejemplo: con N = 5 el programa muestra:\n"
        "1 2 3 4 5"
    ),
    difficulty="Easy",
    inputs=[i_number(5)],
    stdin="5\n",
    starter={
        "python": (
            "# Pista: range(1, n + 1) recorre del 1 al n.\n"
            "# Imprime sin salto de linea: print(i, end=' ')\n"
            "# Al final, con un print() a secas, saltas de linea.\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        // Pista: for (int i = 1; i <= n; i++) con System.out.print(i + \" \")\n"
            "        // Al final, un System.out.println() a secas salta de linea.\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Contar\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tLeer n\n"
            "\t// Pista: Para i <- 1 Hasta n Con Paso 1 Hacer\n"
            "\t//     Escribir i, \" \" Sin Saltar\n"
            "\t// FinPara\n"
            "\t// Al final, un Escribir \"\" a secas salta de linea.\n"
            "FinProceso\n"
        ),
    },
    solutions={
        "python": (
            "n = int(input())\n"
            "for i in range(1, n + 1):\n"
            "    print(i, end=' ')\nprint()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        for (int i = 1; i <= n; i++) {\n"
            "            System.out.print(i + \" \");\n"
            "        }\n"
            "        System.out.println();\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Contar\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tLeer n\n"
            "\tPara i <- 1 Hasta n Con Paso 1 Hacer\n"
            "\t\tEscribir i, \" \" Sin Saltar\n"
            "\tFinPara\n"
            "\tEscribir \"\"\n"
            "FinProceso\n"
        ),
    },
)
