"""
Ejercicios 16-30 de PS Academy (parte 2 de 2).

Misma estructura y mismas reglas que exercises_part1.py:
  - Los ejercicios evitan la division con decimales. Cuando hace falta dividir, las
    tres soluciones fuerzan entero de forma explicita (// en Python, division entera
    en Java, Trunc en PSeint) para que la salida coincida exactamente.
  - El traductor de PSeint no soporta arreglos, Segun ni SubProceso/Funcion, asi que
    ninguno de estos ejercicios los usa.
"""

from __future__ import annotations

from build_exercises import Exercise, i_number, i_text

EXERCISES: list[Exercise] = []


def add(**kwargs) -> None:
    EXERCISES.append(Exercise(**kwargs))


def java_stub(*body: str) -> str:
    """Plantilla Java: lee lo que le pasa el cuerpo y no imprime nada todavia."""
    return (
        "import java.util.Scanner;\n\n"
        "public class Main {\n\n"
        "    static Scanner sc = new Scanner(System.in);\n\n"
        "    public static void main(String[] args) {\n"
        + "".join(("        " + l + "\n") for l in body)
        + "    }\n}\n"
    )


def java_stub_ret(result: str, *body: str) -> str:
    """Plantilla Java que ademas imprime el resultado indicado."""
    return (
        "import java.util.Scanner;\n\n"
        "public class Main {\n\n"
        "    static Scanner sc = new Scanner(System.in);\n\n"
        "    public static void main(String[] args) {\n"
        + "".join(("        " + l + "\n") for l in body)
        + f"        System.out.println({result});\n"
        + "    }\n}\n"
    )


def ps_stub(*body: str) -> str:
    return "Proceso Ejercicio\n" + "".join("\t" + l + "\n" for l in body) + "FinProceso\n"


# ================================================ ESTRUCTURAS DE CONTROL

add(
    key="ctrl-05",
    category="Estructuras de control",
    title="Numero mayor entre tres",
    description=(
        "Pide tres numeros y muestra el mayor de los tres.\n\n"
        "Ejemplo: con 12, 47 y 8 el programa muestra:\n"
        "47"
    ),
    difficulty="Medium",
    inputs=[i_number(12), i_number(47), i_number(8)],
    stdin="12\n47\n8\n",
    starter={
        "python": "# Compara los tres numeros con if / elif / else\n",
        "java": java_stub("int a = sc.nextInt();", "int b = sc.nextInt();", "int c = sc.nextInt();"),
        "pseint": ps_stub(
            "Definir a Como Entero",
            "Definir b Como Entero",
            "Definir c Como Entero",
            "Definir mayor Como Entero",
            "Leer a, b, c",
            "// Compara los tres con Si / Sino Si / Sino",
        ),
    },
    solutions={
        "python": (
            "a = int(input())\nb = int(input())\nc = int(input())\n"
            "mayor = a\n"
            "if b > mayor:\n    mayor = b\n"
            "if c > mayor:\n    mayor = c\n"
            "print(mayor)\n"
        ),
        "java": java_stub_ret(
            "mayor",
            "int a = sc.nextInt();",
            "int b = sc.nextInt();",
            "int c = sc.nextInt();",
            "int mayor = a;",
            "if (b > mayor) {",
            "    mayor = b;",
            "}",
            "if (c > mayor) {",
            "    mayor = c;",
            "}",
        ),
        "pseint": (
            "Proceso MayorTres\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir c Como Entero\n"
            "\tDefinir mayor Como Entero\n"
            "\tLeer a, b, c\n"
            "\tmayor <- a\n"
            "\tSi b > mayor Entonces\n"
            "\t\tmayor <- b\n"
            "\tFinSi\n"
            "\tSi c > mayor Entonces\n"
            "\t\tmayor <- c\n"
            "\tFinSi\n"
            "\tEscribir mayor\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-06",
    category="Estructuras de control",
    title="Tabla de multiplicar",
    description=(
        "Pide un numero y muestra su tabla de multiplicar del 1 al 5, una linea por\n"
        "multiplicacion y con el formato '3 x 4 = 12'.\n\n"
        "Ejemplo con N = 4:\n"
        "4 x 1 = 4\n"
        "4 x 2 = 8\n"
        "4 x 3 = 12\n"
        "4 x 4 = 16\n"
        "4 x 5 = 20"
    ),
    difficulty="Medium",
    inputs=[i_number(4)],
    stdin="4\n",
    starter={
        "python": (
            "# Pista: for i in range(1, 6) recorre del 1 al 5.\n"
            "# Imprime con print(n, 'x', i, '=', n * i)\n"
        ),
        "java": java_stub("int n = sc.nextInt();", "// for (int i = 1; i <= 5; i++)"),
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir i Como Entero",
            "Leer n",
            "// Para i <- 1 Hasta 5 Con Paso 1 Hacer",
        ),
    },
    solutions={
        "python": (
            "n = int(input())\n"
            "for i in range(1, 6):\n"
            "    print(n, 'x', i, '=', n * i)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        for (int i = 1; i <= 5; i++) {\n"
            "            System.out.println(n + \" x \" + i + \" = \" + (n * i));\n"
            "        }\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso TablaMultiplicar\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tLeer n\n"
            "\tPara i <- 1 Hasta 5 Con Paso 1 Hacer\n"
            "\t\tEscribir n, \" x \", i, \" = \", n * i\n"
            "\tFinPara\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-07",
    category="Estructuras de control",
    title="Contar hacia atras",
    description=(
        "Pide un numero N y muestra los numeros desde N hasta 1, separados por un\n"
        "espacio y en una sola linea.\n\n"
        "Ejemplo: con N = 5 el programa muestra:\n"
        "5 4 3 2 1"
    ),
    difficulty="Medium",
    inputs=[i_number(5)],
    stdin="5\n",
    starter={
        "python": (
            "# Pista: range(n, 0, -1) cuenta desde n hasta 1 hacia atras.\n"
            "# Imprime sin salto de linea: print(i, end=' ')\n"
        ),
        "java": java_stub("int n = sc.nextInt();", "// for (int i = n; i >= 1; i--)"),
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir i Como Entero",
            "Leer n",
            "// Para i <- n Hasta 1 Con Paso -1 Hacer",
        ),
    },
    solutions={
        "python": (
            "n = int(input())\n"
            "for i in range(n, 0, -1):\n"
            "    print(i, end=' ')\nprint()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        for (int i = n; i >= 1; i--) {\n"
            "            System.out.print(i + \" \");\n"
            "        }\n"
            "        System.out.println();\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso ContarAtras\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tLeer n\n"
            "\tPara i <- n Hasta 1 Con Paso -1 Hacer\n"
            "\t\tEscribir i, \" \" Sin Saltar\n"
            "\tFinPara\n"
            "\tEscribir \"\"\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-08",
    category="Estructuras de control",
    title="Validar una contrasena",
    description=(
        "Pide una contrasena y decide si es valida.\n\n"
        "Una contrasena es valida si tiene al menos 8 caracteres.\n\n"
        "Si es valida muestra 'Acceso permitido', si no 'Contrasena demasiado corta'.\n\n"
        "Ejemplo: con 'abc123' muestra:\n"
        "Contrasena demasiado corta"
    ),
    difficulty="Medium",
    inputs=[i_text("abc123")],
    stdin="abc123\n",
    starter={
        "python": (
            "# Pista: len(contrasena) >= 8\n"
            "# print('Acceso permitido' if valida else 'Contrasena demasiado corta')\n"
        ),
        "java": java_stub("String contrasena = sc.nextLine();", "// contrasena.length() >= 8"),
        "pseint": ps_stub(
            "Definir contrasena Como Cadena",
            "Definir mensaje Como Cadena",
            "Leer contrasena",
            "// Si Longitud(contrasena) >= 8 Entonces ...",
        ),
    },
    solutions={
        "python": (
            "contrasena = input()\n"
            "if len(contrasena) >= 8:\n"
            "    print('Acceso permitido')\n"
            "else:\n"
            "    print('Contrasena demasiado corta')\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        String contrasena = sc.nextLine();\n"
            "        if (contrasena.length() >= 8) {\n"
            "            System.out.println(\"Acceso permitido\");\n"
            "        } else {\n"
            "            System.out.println(\"Contrasena demasiado corta\");\n"
            "        }\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso ValidarContrasena\n"
            "\tDefinir contrasena Como Cadena\n"
            "\tDefinir mensaje Como Cadena\n"
            "\tLeer contrasena\n"
            "\tSi Longitud(contrasena) >= 8 Entonces\n"
            "\t\tmensaje <- \"Acceso permitido\"\n"
            "\tSino\n"
            "\t\tmensaje <- \"Contrasena demasiado corta\"\n"
            "\tFinSi\n"
            "\tEscribir mensaje\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-09",
    category="Estructuras de control",
    title="Cuantos digitos tiene un numero",
    description=(
        "Pide un numero entero positivo y muestra cuantos digitos tiene.\n\n"
        "Pista: divide el numero entre 10 una vez por cada digito y cuenta las veces que\n"
        "puedes hacerlo antes de que llegue a 0.\n\n"
        "Ejemplo: con 4092 el programa muestra:\n"
        "4"
    ),
    difficulty="Hard",
    inputs=[i_number(4092)],
    stdin="4092\n",
    starter={
        "python": (
            "# while n > 0:\n"
            "#     n = n // 10   # en Python la division entera es //\n"
            "#     digitos = digitos + 1\n"
        ),
        "java": java_stub(
            "int n = sc.nextInt();",
            "// while (n > 0) { n = n / 10; digitos++; }",
        ),
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir digitos Como Entero",
            "Leer n",
            "// Pista: Mientras n > 0 Hacer, y dentro n <- Trunc(n / 10)",
            "// Muestra aqui los digitos con Escribir digitos",
        ),
    },
    solutions={
        "python": (
            "n = int(input())\ndigitos = 0\n"
            "while n > 0:\n"
            "    n = n // 10\n"
            "    digitos = digitos + 1\n"
            "print(digitos)\n"
        ),
        "java": java_stub_ret(
            "digitos",
            "int n = sc.nextInt();",
            "int digitos = 0;",
            "while (n > 0) {",
            "    n = n / 10;",
            "    digitos = digitos + 1;",
            "}",
        ),
        "pseint": (
            "Proceso Digitos\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir digitos Como Entero\n"
            "\tLeer n\n"
            "\tdigitos <- 0\n"
            "\tMientras n > 0 Hacer\n"
            "\t\tn <- Trunc(n / 10)\n"
            "\t\tdigitos <- digitos + 1\n"
            "\tFinMientras\n"
            "\tEscribir digitos\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="ctrl-10",
    category="Estructuras de control",
    title="Mayor y menor de N numeros",
    description=(
        "Pide primero cuantos numeros hay (N) y despues esos N numeros, uno por linea.\n"
        "Al final muestra, en dos lineas, el mayor y el menor de todos ellos.\n\n"
        "Pista: guarda el primer numero como mayor y menor, y despues compara cada\n"
        "numero con los dos.\n\n"
        "Ejemplo: N = 5 y luego 8, 3, 12, 7 y 5 produce:\n"
        "12\n"
        "3"
    ),
    difficulty="Hard",
    inputs=[i_number(5), i_number(8), i_number(3), i_number(12), i_number(7), i_number(5)],
    stdin="5\n8\n3\n12\n7\n5\n",
    starter={
        "python": (
            "# El primer numero es el punto de partida:\n"
            "# mayor = menor = primer\n"
            "# for _ in range(n - 1):\n"
            "#     x = int(input())\n"
            "#     if x > mayor: mayor = x\n"
            "#     if x < menor: menor = x\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        int x = sc.nextInt();\n"
            "        int mayor = x;\n"
            "        int menor = x;\n"
            "        // Lee los n - 1 numeros que faltan y actualiza mayor y menor\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir i Como Entero",
            "Definir x Como Entero",
            "Definir mayor Como Entero",
            "Definir menor Como Entero",
            "Leer n, x",
            "// El primer numero (x) es el punto de partida de mayor y menor",
            "// Para i <- 2 Hasta n Con Paso 1 Hacer, lee x y actualiza ambos",
            "// Muestra primero mayor y despues menor",
        ),
    },
    solutions={
        "python": (
            "n = int(input())\nprimero = int(input())\n"
            "mayor = primero\nmenor = primero\n"
            "for _ in range(n - 1):\n"
            "    x = int(input())\n"
            "    if x > mayor:\n        mayor = x\n"
            "    if x < menor:\n        menor = x\n"
            "print(mayor)\nprint(menor)\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        int primero = sc.nextInt();\n"
            "        int mayor = primero;\n"
            "        int menor = primero;\n"
            "        for (int i = 1; i < n; i++) {\n"
            "            int x = sc.nextInt();\n"
            "            if (x > mayor) {\n"
            "                mayor = x;\n"
            "            }\n"
            "            if (x < menor) {\n"
            "                menor = x;\n"
            "            }\n"
            "        }\n"
            "        System.out.println(mayor);\n"
            "        System.out.println(menor);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso MayorMenor\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tDefinir x Como Entero\n"
            "\tDefinir mayor Como Entero\n"
            "\tDefinir menor Como Entero\n"
            "\tLeer n, x\n"
            "\tmayor <- x\n"
            "\tmenor <- x\n"
            "\tPara i <- 2 Hasta n Con Paso 1 Hacer\n"
            "\t\tLeer x\n"
            "\t\tSi x > mayor Entonces\n"
            "\t\t\tmayor <- x\n"
            "\t\tFinSi\n"
            "\t\tSi x < menor Entonces\n"
            "\t\t\tmenor <- x\n"
            "\t\tFinSi\n"
            "\tFinPara\n"
            "\tEscribir mayor\n"
            "\tEscribir menor\n"
            "FinProceso\n"
        ),
    },
)

# =========================================================== FUNCIONES
# El traductor de PSeint no soporta SubProceso/Funcion, asi que en estos ejercicios
# la plantilla PSeint pide resolver la logica directamente y la solucion PSeint lo
# hace en un solo bloque. El enunciado indica el objetivo pedagogico sin prometer
# funciones en PSeint.

add(
    key="fn-01",
    category="Funciones",
    title="Funcion que suma dos numeros",
    description=(
        "Crea una funcion llamada sumar que reciba dos numeros y devuelva su suma.\n\n"
        "Despues pide dos numeros por pantalla y muestra el resultado.\n\n"
        "En PSeint no hay funciones: resuelve la suma directamente en el bloque.\n\n"
        "Ejemplo: si la persona escribe 12 y 8, el programa muestra:\n"
        "20"
    ),
    difficulty="Easy",
    inputs=[i_number(12), i_number(8)],
    stdin="12\n8\n",
    starter={
        "python": (
            "# 1. Define la funcion:  def sumar(a, b):  return a + b\n"
            "# 2. Lee los numeros\n"
            "# 3. Llama a la funcion y muestra el resultado\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // 1. Define aqui el metodo: static int sumar(int a, int b)\n\n"
            "    public static void main(String[] args) {\n"
            "        int x = sc.nextInt();\n"
            "        int y = sc.nextInt();\n"
            "        // 2. Llama a sumar y muestra el resultado\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir x Como Entero",
            "Definir y Como Entero",
            "Definir suma Como Entero",
            "Leer x, y",
            "// PSeint no tiene funciones: calcula la suma directamente",
        ),
    },
    solutions={
        "python": (
            "def sumar(a, b):\n"
            "    return a + b\n\n\n"
            "def main():\n"
            "    x = int(input())\n"
            "    y = int(input())\n"
            "    print(sumar(x, y))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int sumar(int a, int b) {\n"
            "        return a + b;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int x = sc.nextInt();\n"
            "        int y = sc.nextInt();\n"
            "        System.out.println(sumar(x, y));\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Suma\n"
            "\tDefinir x Como Entero\n"
            "\tDefinir y Como Entero\n"
            "\tDefinir suma Como Entero\n"
            "\tLeer x, y\n"
            "\tsuma <- x + y\n"
            "\tEscribir suma\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-02",
    category="Funciones",
    title="Funcion que dice si un numero es par",
    description=(
        "Crea una funcion que reciba un numero y devuelva si es par (Verdadero) o no\n"
        "(Falso).\n\n"
        "Despues pide un numero y muestra 'Par' o 'Impar' segun la respuesta.\n\n"
        "Ejemplo: con 10 el programa muestra:\n"
        "Par"
    ),
    difficulty="Easy",
    inputs=[i_number(10)],
    stdin="10\n",
    starter={
        "python": (
            "# Pista:\n"
            "# def es_par(n):\n"
            "#     return n % 2 == 0\n"
            "# if es_par(n): print('Par') else: print('Impar')\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // static boolean esPar(int n) { return n % 2 == 0; }\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        // Muestra 'Par' o 'Impar' segun esPar(n)\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir resultado Como Cadena",
            "Leer n",
            "// PSeint no tiene funciones: comprueba Mod(n, 2) == 0 directamente",
        ),
    },
    solutions={
        "python": (
            "def es_par(n):\n"
            "    return n % 2 == 0\n\n\n"
            "def main():\n"
            "    n = int(input())\n"
            "    if es_par(n):\n"
            "        print('Par')\n"
            "    else:\n"
            "        print('Impar')\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static boolean esPar(int n) {\n"
            "        return n % 2 == 0;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        if (esPar(n)) {\n"
            "            System.out.println(\"Par\");\n"
            "        } else {\n"
            "            System.out.println(\"Impar\");\n"
            "        }\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso ParImpar\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir resultado Como Cadena\n"
            "\tLeer n\n"
            "\tSi Mod(n, 2) == 0 Entonces\n"
            "\t\tresultado <- \"Par\"\n"
            "\tSino\n"
            "\t\tresultado <- \"Impar\"\n"
            "\tFinSi\n"
            "\tEscribir resultado\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-03",
    category="Funciones",
    title="Funcion que calcule el doble",
    description=(
        "Crea una funcion que reciba un numero y devuelva el doble de ese numero.\n\n"
        "Despues pide tres numeros y muestra el doble de cada uno, uno por linea.\n\n"
        "Ejemplo con 1, 2 y 3:\n"
        "2\n4\n6"
    ),
    difficulty="Easy",
    inputs=[i_number(1), i_number(2), i_number(3)],
    stdin="1\n2\n3\n",
    starter={
        "python": (
            "# Pista:\n"
            "# def doble(n): return n * 2\n"
            "# Llama a doble() con cada numero leido y muestra el resultado.\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // static int doble(int n) { return n * 2; }\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        int c = sc.nextInt();\n"
            "        // Muestra el doble de cada uno, uno por linea\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir a Como Entero",
            "Definir b Como Entero",
            "Definir c Como Entero",
            "Leer a, b, c",
            "// PSeint no tiene funciones: muestra a*2, b*2 y c*2",
        ),
    },
    solutions={
        "python": (
            "def doble(n):\n"
            "    return n * 2\n\n\n"
            "def main():\n"
            "    a = int(input())\n"
            "    b = int(input())\n"
            "    c = int(input())\n"
            "    print(doble(a))\n"
            "    print(doble(b))\n"
            "    print(doble(c))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int doble(int n) {\n"
            "        return n * 2;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        int c = sc.nextInt();\n"
            "        System.out.println(doble(a));\n"
            "        System.out.println(doble(b));\n"
            "        System.out.println(doble(c));\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Dobles\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir c Como Entero\n"
            "\tLeer a, b, c\n"
            "\tEscribir a * 2\n"
            "\tEscribir b * 2\n"
            "\tEscribir c * 2\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-04",
    category="Funciones",
    title="Funcion que salude",
    description=(
        "Crea una funcion llamada saludar que reciba un nombre y muestre un saludo\n"
        "personalizado con ese nombre.\n\n"
        "Despues pide un nombre y llama a la funcion.\n\n"
        "Ejemplo: si la persona escribe 'Luis', el programa muestra:\n"
        "Hola, Luis!\n\n"
        "En PSeint no hay funciones: escribe el saludo directamente."
    ),
    difficulty="Easy",
    inputs=[i_text("Luis")],
    stdin="Luis\n",
    starter={
        "python": (
            "# Pista:\n"
            "# def saludar(nombre):\n"
            "#     print('Hola, ' + nombre + '!')\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // static void saludar(String nombre)\n\n"
            "    public static void main(String[] args) {\n"
            "        String nombre = sc.nextLine();\n"
            "        // Llama a saludar(nombre)\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir nombre Como Cadena",
            "Leer nombre",
            "// PSeint no tiene funciones: escribe el saludo directamente",
        ),
    },
    solutions={
        "python": (
            "def saludar(nombre):\n"
            "    print('Hola, ' + nombre + '!')\n\n\n"
            "def main():\n"
            "    nombre = input()\n"
            "    saludar(nombre)\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static void saludar(String nombre) {\n"
            "        System.out.println(\"Hola, \" + nombre + \"!\");\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        String nombre = sc.nextLine();\n"
            "        saludar(nombre);\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Saludar\n"
            "\tDefinir nombre Como Cadena\n"
            "\tLeer nombre\n"
            "\tEscribir \"Hola, \", nombre, \"!\"\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-05",
    category="Funciones",
    title="Funcion que calcule el area de un rectangulo",
    description=(
        "Crea una funcion que reciba la base y la altura y devuelva el area.\n\n"
        "Despues pide los dos valores y muestra el area calculada con la funcion.\n\n"
        "Ejemplo: base 5 y altura 4 dan un area de 20."
    ),
    difficulty="Medium",
    inputs=[i_number(5), i_number(4)],
    stdin="5\n4\n",
    starter={
        "python": (
            "# Pista:\n"
            "# def area(base, altura): return base * altura\n"
        ),
        "java": "import java.util.Scanner;\n\npublic class Main {\n\n    static Scanner sc = new Scanner(System.in);\n\n    // static int area(int base, int altura)\n\n    public static void main(String[] args) {\n        int base = sc.nextInt();\n        int altura = sc.nextInt();\n        // Muestra area(base, altura)\n    }\n}\n",
        "pseint": ps_stub(
            "Definir base Como Entero",
            "Definir altura Como Entero",
            "Leer base, altura",
            "// PSeint no tiene funciones: calcula base * altura",
        ),
    },
    solutions={
        "python": (
            "def area(base, altura):\n"
            "    return base * altura\n\n\n"
            "def main():\n"
            "    base = int(input())\n"
            "    altura = int(input())\n"
            "    print(area(base, altura))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int area(int base, int altura) {\n"
            "        return base * altura;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int base = sc.nextInt();\n"
            "        int altura = sc.nextInt();\n"
            "        System.out.println(area(base, altura));\n"
            "    }\n}\n"
        ),
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
    key="fn-06",
    category="Funciones",
    title="Funcion que calcule el factorial",
    description=(
        "Crea una funcion que calcule el factorial de un numero (1 x 2 x 3 x ... x N).\n\n"
        "El factorial de 0 es 1. Pide un numero y muestra su factorial.\n\n"
        "Ejemplo: con 5 el programa muestra:\n"
        "120"
    ),
    difficulty="Medium",
    inputs=[i_number(5)],
    stdin="5\n",
    starter={
        "python": (
            "# Pista: empieza en 1 y ve multiplicando.\n"
            "# def factorial(n):\n"
            "#     total = 1\n"
            "#     for i in range(1, n + 1):\n"
            "#         total = total * i\n"
            "#     return total\n"
        ),
        "java": "import java.util.Scanner;\n\npublic class Main {\n\n    static Scanner sc = new Scanner(System.in);\n\n    // static int factorial(int n)\n\n    public static void main(String[] args) {\n        int n = sc.nextInt();\n        // Muestra factorial(n)\n    }\n}\n",
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir i Como Entero",
            "Definir total Como Entero",
            "Leer n",
            "// PSeint no tiene funciones: multiplica con un Para",
        ),
    },
    solutions={
        "python": (
            "def factorial(n):\n"
            "    total = 1\n"
            "    for i in range(1, n + 1):\n"
            "        total = total * i\n"
            "    return total\n\n\n"
            "def main():\n"
            "    n = int(input())\n"
            "    print(factorial(n))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int factorial(int n) {\n"
            "        int total = 1;\n"
            "        for (int i = 1; i <= n; i++) {\n"
            "            total = total * i;\n"
            "        }\n"
            "        return total;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        System.out.println(factorial(n));\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso Factorial\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tDefinir total Como Entero\n"
            "\tLeer n\n"
            "\ttotal <- 1\n"
            "\tPara i <- 1 Hasta n Con Paso 1 Hacer\n"
            "\t\ttotal <- total * i\n"
            "\tFinPara\n"
            "\tEscribir total\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-07",
    category="Funciones",
    title="Funcion que calcule el promedio de una lista de numeros",
    description=(
        "Crea una funcion que calcule el promedio de una lista de numeros, sin decimales.\n\n"
        "Despues pide 4 numeros, guardalos en una lista y muestra el promedio.\n\n"
        "Ejemplo: 10, 20, 30 y 40 dan un promedio de 25.\n\n"
        "Recordatorio: en Python // es la division entera. En Java la division entre\n"
        "enteros ya es entera."
    ),
    difficulty="Medium",
    inputs=[i_number(10), i_number(20), i_number(30), i_number(40)],
    stdin="10\n20\n30\n40\n",
    starter={
        "python": (
            "# Pista: una lista se crea asi:  numeros = [10, 20, 30, 40]\n"
            "# def promedio(lista): return sum(lista) // len(lista)\n"
            "# Lee 4 numeros y anadelos a la lista con append().\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // Pista: int[] lista = new int[4]; y luego lista[i] = sc.nextInt();\n\n"
            "    public static void main(String[] args) {\n"
            "        // Crea el arreglo, leelo y muestra el promedio sin decimales\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir a Como Entero",
            "Definir b Como Entero",
            "Definir c Como Entero",
            "Definir d Como Entero",
            "Leer a, b, c, d",
            "// PSeint no tiene arreglos ni funciones: promedio <- Trunc((a+b+c+d)/4)",
        ),
    },
    solutions={
        "python": (
            "def promedio(lista):\n"
            "    return sum(lista) // len(lista)\n\n\n"
            "def main():\n"
            "    numeros = []\n"
            "    for _ in range(4):\n"
            "        numeros.append(int(input()))\n"
            "    print(promedio(numeros))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int promedio(int[] lista) {\n"
            "        int suma = 0;\n"
            "        for (int i = 0; i < lista.length; i++) {\n"
            "            suma += lista[i];\n"
            "        }\n"
            "        return suma / lista.length;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int[] lista = new int[4];\n"
            "        for (int i = 0; i < 4; i++) {\n"
            "            lista[i] = sc.nextInt();\n"
            "        }\n"
            "        System.out.println(promedio(lista));\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso PromedioLista\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir c Como Entero\n"
            "\tDefinir d Como Entero\n"
            "\tDefinir promedio Como Entero\n"
            "\tLeer a, b, c, d\n"
            "\tpromedio <- Trunc((a + b + c + d) / 4)\n"
            "\tEscribir promedio\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-08",
    category="Funciones",
    title="Funcion que calcule el area de un triangulo",
    description=(
        "Crea una funcion que calcule el area de un triangulo a partir de su base y su\n"
        "altura, y devuelva el resultado sin decimales (base * altura / 2).\n\n"
        "Despues pide los dos valores y muestra el area.\n\n"
        "Ejemplo: base 7 y altura 6 dan un area de 21.\n\n"
        "En PSeint no hay funciones: calcula el area directamente en el bloque."
    ),
    difficulty="Medium",
    inputs=[i_number(7), i_number(6)],
    stdin="7\n6\n",
    starter={
        "python": (
            "# Pista: def area(base, altura): return base * altura // 2\n"
            "# Ojo: // es la division entera en Python.\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // static int area(int base, int altura) { return base * altura / 2; }\n\n"
            "    public static void main(String[] args) {\n"
            "        int base = sc.nextInt();\n"
            "        int altura = sc.nextInt();\n"
            "        // Muestra area(base, altura)\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir base Como Entero",
            "Definir altura Como Entero",
            "Definir area Como Entero",
            "Leer base, altura",
            "// PSeint no tiene funciones: area <- Trunc(base * altura / 2)",
        ),
    },
    solutions={
        "python": (
            "def area(base, altura):\n"
            "    return base * altura // 2\n\n\n"
            "def main():\n"
            "    base = int(input())\n"
            "    altura = int(input())\n"
            "    print(area(base, altura))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int area(int base, int altura) {\n"
            "        return base * altura / 2;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int base = sc.nextInt();\n"
            "        int altura = sc.nextInt();\n"
            "        System.out.println(area(base, altura));\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso AreaTriangulo\n"
            "\tDefinir base Como Entero\n"
            "\tDefinir altura Como Entero\n"
            "\tDefinir area Como Entero\n"
            "\tLeer base, altura\n"
            "\tarea <- Trunc(base * altura / 2)\n"
            "\tEscribir area\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-09",
    category="Funciones",
    title="Funcion que diga si un numero es primo",
    description=(
        "Crea una funcion que diga si un numero es primo (unico primo es el 1).\n\n"
        "Un numero es primo si solo se divide entre 1 y entre si mismo.\n\n"
        "Pide un numero y muestra 'Es primo' o 'No es primo'.\n\n"
        "Ejemplo: con 7 el programa muestra:\n"
        "Es primo\n\n"
        "En PSeint no hay funciones: recorre los divisores con un Mientras."
    ),
    difficulty="Hard",
    inputs=[i_number(7)],
    stdin="7\n",
    starter={
        "python": (
            "# Pista: recorre desde 2 hasta n - 1 y busca un divisor.\n"
            "# Si n < 2 no es primo.\n"
            "# def es_primo(n):\n"
            "#     ...\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // static boolean esPrimo(int n)\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        // Muestra 'Es primo' o 'No es primo'\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir n Como Entero",
            "Definir i Como Entero",
            "Definir esprimo Como Logico",
            "Leer n",
            "// Empieza esprimo en Verdadero y busca un divisor con Mientras",
        ),
    },
    solutions={
        "python": (
            "def es_primo(n):\n"
            "    if n < 2:\n"
            "        return False\n"
            "    for i in range(2, n):\n"
            "        if n % i == 0:\n"
            "            return False\n"
            "    return True\n\n\n"
            "def main():\n"
            "    n = int(input())\n"
            "    print('Es primo' if es_primo(n) else 'No es primo')\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static boolean esPrimo(int n) {\n"
            "        if (n < 2) {\n"
            "            return false;\n"
            "        }\n"
            "        for (int i = 2; i < n; i++) {\n"
            "            if (n % i == 0) {\n"
            "                return false;\n"
            "            }\n"
            "        }\n"
            "        return true;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int n = sc.nextInt();\n"
            "        if (esPrimo(n)) {\n"
            "            System.out.println(\"Es primo\");\n"
            "        } else {\n"
            "            System.out.println(\"No es primo\");\n"
            "        }\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso EsPrimo\n"
            "\tDefinir n Como Entero\n"
            "\tDefinir i Como Entero\n"
            "\tDefinir esprimo Como Logico\n"
            "\tLeer n\n"
            "\tesprimo <- Verdadero\n"
            "\tSi n < 2 Entonces\n"
            "\t\tesprimo <- Falso\n"
            "\tSino\n"
            "\t\ti <- 2\n"
            "\t\tMientras i < n Y esprimo Hacer\n"
            "\t\t\tSi Mod(n, i) == 0 Entonces\n"
            "\t\t\t\tesprimo <- Falso\n"
            "\t\t\tFinSi\n"
            "\t\t\ti <- i + 1\n"
            "\t\tFinMientras\n"
            "\tFinSi\n"
            "\tSi esprimo Entonces\n"
            "\t\tEscribir \"Es primo\"\n"
            "\tSino\n"
            "\t\tEscribir \"No es primo\"\n"
            "\tFinSi\n"
            "FinProceso\n"
        ),
    },
)

add(
    key="fn-10",
    category="Funciones",
    title="Funcion que calcule el MCD",
    description=(
        "Crea una funcion que calcule el maximo comun divisor (MCD) de dos numeros.\n\n"
        "Usa el algoritmo de Euclides: mientras B no sea 0, el MCD de A y B es el MCD\n"
        "de B y el resto de A entre B.\n\n"
        "Pide dos numeros positivos y muestra el MCD.\n\n"
        "Ejemplo: A = 48 y B = 18 dan un MCD de 6."
    ),
    difficulty="Hard",
    inputs=[i_number(48), i_number(18)],
    stdin="48\n18\n",
    starter={
        "python": (
            "# Pista:\n"
            "# def mcd(a, b):\n"
            "#     while b != 0:\n"
            "#         a, b = b, a % b\n"
            "#     return a\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    // static int mcd(int a, int b) con el algoritmo de Euclides\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        // Muestra mcd(a, b)\n"
            "    }\n}\n"
        ),
        "pseint": ps_stub(
            "Definir a Como Entero",
            "Definir b Como Entero",
            "Definir resto Como Entero",
            "Leer a, b",
            "// PSeint no tiene funciones: Mientras b <> 0 Hacer y guarda el resto",
        ),
    },
    solutions={
        "python": (
            "def mcd(a, b):\n"
            "    while b != 0:\n"
            "        a, b = b, a % b\n"
            "    return a\n\n\n"
            "def main():\n"
            "    a = int(input())\n"
            "    b = int(input())\n"
            "    print(mcd(a, b))\n\n\n"
            "main()\n"
        ),
        "java": (
            "import java.util.Scanner;\n\n"
            "public class Main {\n\n"
            "    static Scanner sc = new Scanner(System.in);\n\n"
            "    static int mcd(int a, int b) {\n"
            "        while (b != 0) {\n"
            "            int resto = a % b;\n"
            "            a = b;\n"
            "            b = resto;\n"
            "        }\n"
            "        return a;\n"
            "    }\n\n"
            "    public static void main(String[] args) {\n"
            "        int a = sc.nextInt();\n"
            "        int b = sc.nextInt();\n"
            "        System.out.println(mcd(a, b));\n"
            "    }\n}\n"
        ),
        "pseint": (
            "Proceso MCD\n"
            "\tDefinir a Como Entero\n"
            "\tDefinir b Como Entero\n"
            "\tDefinir resto Como Entero\n"
            "\tLeer a, b\n"
            "\tMientras b <> 0 Hacer\n"
            "\t\tresto <- Mod(a, b)\n"
            "\t\ta <- b\n"
            "\t\tb <- resto\n"
            "\tFinMientras\n"
            "\tEscribir a\n"
            "FinProceso\n"
        ),
    },
)
