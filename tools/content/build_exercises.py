"""
Generador del catalogo de ejercicios de PS Academy.

Cada ejercicio trae codigo de inicio (plantilla) y una solucion de referencia en
Python, Java y PSeint. Este script EJECUTA las tres soluciones con los mismos datos
de entrada y comprueba que producen exactamente la misma salida. Solo si las tres
coinciden se acepta el ejercicio y se toma esa salida como ExpectedOutput.

Motivo: el backend compara con StringComparison.Ordinal, es decir, exige coincidencia
exacta. Un ExpectedOutput escrito a mano fallaria en cuanto la solucion real de un
alumno difiere en un espacio o en un salto de linea.

Ademas valida las plantillas que veran los alumnos: el codigo de inicio de Java debe
compilar y el de PSeint debe traducirse. Una plantilla que no compila ensucia el
primer "Ejecutar" del alumno.

Uso:
    python build_exercises.py --out exercises.json
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field

LANGUAGES = ("python", "java", "pseint")

# Reparto pedido: 10 ejercicios por categoria, 4 faciles, 4 medios y 2 dificil.
EXPECTED_DISTRIBUTION = {"Easy": 4, "Medium": 4, "Hard": 2}
EXPECTED_PER_CATEGORY = 10

# El tutorial es la unica categoria con otro reparto: 5 lecciones guiadas, todas
# faciles, y cada una con sus pasos.
TUTORIAL_CATEGORY = "Tutorial"
COURSE_CATEGORIES = ("Fundamentos", "Estructuras de control", "Funciones")
EXPECTED_TUTORIAL_COUNT = 5
MIN_TUTORIAL_STEPS = 3


# --------------------------------------------------------------------- Modelo


@dataclass
class Step:
    """Un paso guiado del tutorial.

    A diferencia del ejercicio, el paso solo existe en PSeint: es el lenguaje en el
    que se explica todo el curso y el unico que el traductor acepta como entrada
    del alumno. `snippet` es un programa completo y ejecutable, no un fragmento, para
    que el alumno pueda ejecutarlo tal cual en el paso.
    """

    title: str
    body: str
    snippet: str
    task: str | None = None
    stdin: str = ""
    tip: str | None = None
    expected: str | None = None


@dataclass
class Exercise:
    """Un ejercicio del catalogo.

    `starter` es lo que ve el alumno; `solutions` es la referencia que este script
    ejecuta para fijar el ExpectedOutput. El enunciado nunca publica la solucion,
    asi que el campo solutions no llega a la base de datos: solo se usa aqui.
    """

    key: str
    category: str
    title: str
    description: str
    difficulty: str
    inputs: list[dict]
    starter: dict[str, str]
    solutions: dict[str, str]
    stdin: str = ""
    expected: str | None = None
    tutorial: list[Step] = field(default_factory=list)


def i_number(value) -> dict:
    """Valor numerico para los 'valores del leer'."""
    return {"Value": str(value), "ValueType": "Number"}


def i_text(value) -> dict:
    """Valor de texto para los 'valores del leer'."""
    return {"Value": str(value), "ValueType": "Text"}


# ------------------------------------------------------------ Ejecucion


def run_python(code: str, stdin: str = "") -> tuple[int, str, str]:
    """Ejecuta codigo Python y devuelve (codigo_salida, stdout, stderr)."""
    proc = subprocess.run(
        [sys.executable, "-c", code],
        input=stdin,
        capture_output=True,
        text=True,
        timeout=15,
    )
    return proc.returncode, proc.stdout, proc.stderr


def compile_java_in(code: str, directory: str) -> tuple[bool, str]:
    """Escribe Main.java en `directory` y lo compila. Devuelve (ok, error)."""
    source = os.path.join(directory, "Main.java")
    with open(source, "w", encoding="utf-8") as handle:
        handle.write(code)

    proc = subprocess.run(
        ["javac", "-d", directory, source],
        capture_output=True,
        text=True,
        timeout=120,
    )
    return proc.returncode == 0, proc.stderr


def compile_java(code: str) -> tuple[bool, str]:
    """Solo comprueba que el codigo compile, como hace Piston con la clase Main."""
    with tempfile.TemporaryDirectory() as tmp:
        return compile_java_in(code, tmp)


def run_java(code: str, stdin: str = "") -> tuple[int, str, str]:
    """Compila y ejecuta codigo Java del mismo modo que Piston (clase Main)."""
    with tempfile.TemporaryDirectory() as tmp:
        ok, error = compile_java_in(code, tmp)
        if not ok:
            return 1, "", error

        proc = subprocess.run(
            ["java", "-cp", tmp, "Main"],
            input=stdin,
            capture_output=True,
            text=True,
            timeout=30,
        )
        return proc.returncode, proc.stdout, proc.stderr


def pseint_to_python(pseint_code: str, tool_dll: str) -> tuple[bool, str]:
    """Traduce PSeint a Python con el traductor real del backend.

    Usar el traductor de verdad es lo que garantiza que una solucion PSeint sea
    ejecutable en Piston: si el generador acepta el PSeint, el alumno lo vera
    aceptado por ese mismo traductor.
    """
    proc = subprocess.run(
        ["dotnet", tool_dll],
        input=pseint_code,
        capture_output=True,
        text=True,
        timeout=180,
    )
    if proc.returncode != 0:
        return False, proc.stderr.strip()
    return True, proc.stdout


# ------------------------------------------------------------------ Utils


def normalize(value: str) -> str:
    """Replica OutputNormalizer.Normalize del backend (.NET).

    El backend normaliza CRLF y recorta extremos antes de comparar, asi que el
    generador aplica exactamente la misma regla o rechazaria como distintos dos
    resultados que la API consideraria iguales.
    """
    return value.replace("\r\n", "\n").replace("\r", "\n").strip()


def first_line(text: str) -> str:
    lines = text.splitlines()
    return lines[0] if lines else ""


# ------------------------------------------------------------- Validacion


class Verifier:
    def __init__(self, tool_dll: str) -> None:
        self.tool_dll = tool_dll
        self.failures: list[str] = []

    # ------------------------------------------------------------ soluciones

    def verify_solutions(self, exercise: Exercise) -> bool:
        outputs: dict[str, str] = {}

        code, stdout, stderr = run_python(exercise.solutions["python"], exercise.stdin)
        if code != 0:
            return self.fail(exercise, f"la solucion python fallo: {first_line(stderr)}")
        outputs["python"] = normalize(stdout)

        code, stdout, stderr = run_java(exercise.solutions["java"], exercise.stdin)
        if code != 0:
            return self.fail(exercise, f"la solucion java fallo: {first_line(stderr)}")
        outputs["java"] = normalize(stdout)

        ok, translated = pseint_to_python(exercise.solutions["pseint"], self.tool_dll)
        if not ok:
            return self.fail(exercise, f"la solucion pseint no tradujo: {first_line(translated)}")

        code, stdout, stderr = run_python(translated, exercise.stdin)
        if code != 0:
            return self.fail(exercise, f"la solucion pseint (ya traducida) fallo: {first_line(stderr)}")
        outputs["pseint"] = normalize(stdout)

        if len(set(outputs.values())) != 1:
            detail = ", ".join(f"{k}={v!r}" for k, v in outputs.items())
            return self.fail(exercise, f"las tres salidas no coinciden: {detail}")

        exercise.expected = outputs["python"]
        return True

    # ------------------------------------------------------------- tutorial

    def verify_steps(self, exercise: Exercise) -> bool:
        """Ejecuta cada snippet del tutorial y fija su salida esperada.

        Un paso del tutorial no se valida contra la salida del ejercicio sino contra
        la suya, asi que tambien se ejecuta de verdad: si el snippet esta roto, el
        alumno no podria avanzar nunca por ese paso.
        """
        if not exercise.tutorial:
            return True

        if len(exercise.tutorial) < MIN_TUTORIAL_STEPS:
            return self.fail(
                exercise,
                f"el tutorial tiene {len(exercise.tutorial)} pasos, se esperaban al menos {MIN_TUTORIAL_STEPS}.")

        failed = False

        for index, step in enumerate(exercise.tutorial):
            ok, translated = pseint_to_python(step.snippet, self.tool_dll)
            if not ok:
                self.fail(exercise, f"el snippet del paso {index + 1} ('{step.title}') no tradujo: {first_line(translated)}")
                failed = True
                continue

            # Un paso sin stdin propio se ejecuta con los valores del leer del
            # ejercicio: es lo que hara el backend cuando StudentStep no traiga datos.
            # Verificarlo aqui con la cadena vacia daria una salida esperada distinta de
            # la real, y el alumno se quedaria atascado en un paso que el generador dio
            # por bueno.
            code, stdout, stderr = run_python(translated, step.stdin or exercise.stdin)
            if code != 0:
                self.fail(exercise, f"el snippet del paso {index + 1} ('{step.title}') fallo: {first_line(stderr)}")
                failed = True
                continue

            step.expected = normalize(stdout)

            # El ultimo paso del tutorial es el reto del ejercicio: si su salida no
            # coincide con la del ejercicio, el alumno nunca podria completarlo.
            if index == len(exercise.tutorial) - 1 and step.expected != exercise.expected:
                self.fail(
                    exercise,
                    f"el ultimo paso produce {step.expected!r} y el ejercicio {exercise.expected!r}: no son el mismo reto.")
                failed = True

        return not failed

    # ------------------------------------------------------------ plantillas

    def verify_starters(self, exercise: Exercise) -> bool:
        """El codigo de inicio debe ser valido, aunque este incompleto.

        Solo se comprueba que no rompa: una plantilla puede no imprimir nada todavia.
        Lo que no se permite es que el alumno reciba un error de compilacion o una
        traduccion fallida en su primer intento.

        La plantilla de Python se ejecuta con el mismo stdin del ejercicio: si el
        codigo de inicio ya pide un dato con input(), sin el dato abortaria con
        EOFError y la plantilla pareceria rota cuando no lo esta.
        """
        failed = False

        def report(reason: str) -> None:
            nonlocal failed
            self.fail(exercise, reason)
            failed = True

        code, _, stderr = run_python(exercise.starter["python"], exercise.stdin)
        if code != 0:
            report(f"la plantilla python no es valida: {first_line(stderr)}")

        compiled, error = compile_java(exercise.starter["java"])
        if not compiled:
            report(f"la plantilla java no compila: {first_line(error)}")

        translated_ok, translated = pseint_to_python(exercise.starter["pseint"], self.tool_dll)
        if not translated_ok:
            report(f"la plantilla pseint no tradujo: {first_line(translated)}")

        return not failed

    def fail(self, exercise: Exercise, reason: str) -> bool:
        self.failures.append(f"{exercise.key} ({exercise.title}): {reason}")
        return False


def check_distribution(accepted: list[Exercise]) -> list[str]:
    """Comprueba el reparto 10 por categoria con 4/4/2, y el tutorial aparte."""
    problems: list[str] = []

    by_category: dict[str, dict[str, int]] = {}
    for exercise in accepted:
        counts = by_category.setdefault(exercise.category, {})
        counts[exercise.difficulty] = counts.get(exercise.difficulty, 0) + 1

    for category, counts in sorted(by_category.items()):
        total = sum(counts.values())

        if category == TUTORIAL_CATEGORY:
            # El tutorial no sigue el 4/4/2: son 5 lecciones guiadas, todas faciles.
            if total != EXPECTED_TUTORIAL_COUNT:
                problems.append(
                    f"{category}: {total} lecciones, se esperaban {EXPECTED_TUTORIAL_COUNT}.")
            if counts.get("Easy", 0) != total:
                problems.append(f"{category}: todas las lecciones deben ser Easy.")
            continue

        if category not in COURSE_CATEGORIES:
            problems.append(f"{category}: categoria sin reparto definido.")
            continue

        if total != EXPECTED_PER_CATEGORY:
            problems.append(f"{category}: {total} ejercicios, se esperaban {EXPECTED_PER_CATEGORY}.")

        for difficulty, wanted in EXPECTED_DISTRIBUTION.items():
            found = counts.get(difficulty, 0)
            if found != wanted:
                problems.append(f"{category}: {found} de dificultad {difficulty}, se esperaban {wanted}.")

    return problems


# ------------------------------------------------------------------- Main


def default_tool_dll() -> str:
    """Ruta por defecto del traductor: tools/pseint-tool/out dentro del repositorio."""
    repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    return os.path.join(repo_root, "pseint-tool", "out", "pseint-tool.dll")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default="exercises.json", help="Ruta del JSON de salida")
    parser.add_argument(
        "--tool",
        default=os.environ.get("PSEINT_TOOL_DLL", default_tool_dll()),
        help="DLL del traductor PSeint usado para verificar soluciones y plantillas",
    )
    args = parser.parse_args()

    if not os.path.exists(args.tool):
        print(
            f"ERROR: no se encuentra el traductor PSeint en {args.tool}\n"
            "Compilalo con:\n"
            "    dotnet build tools/pseint-tool -c Release -o tools/pseint-tool/out",
            file=sys.stderr,
        )
        return 1

    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import exercises_part1
    import exercises_part2
    import tutorial

    all_exercises = (
        exercises_part1.EXERCISES
        + exercises_part2.EXERCISES
        + tutorial.TUTORIAL_EXERCISES
    )

    seen: set[str] = set()
    duplicates = [e.key for e in all_exercises if e.key in seen or seen.add(e.key)]
    if duplicates:
        print(f"ERROR: claves duplicadas: {duplicates}", file=sys.stderr)
        return 1

    print(f"Verificando {len(all_exercises)} ejercicios (solucion y plantilla en 3 lenguajes)...")

    verifier = Verifier(args.tool)
    accepted: list[Exercise] = []
    total = len(all_exercises)

    for index, exercise in enumerate(all_exercises, start=1):
        label = f"[{index:2d}/{total}] {exercise.key:<9} {exercise.title[:36]:<36}"
        solutions_ok = verifier.verify_solutions(exercise)
        starters_ok = verifier.verify_starters(exercise)
        steps_ok = verifier.verify_steps(exercise)

        if solutions_ok and starters_ok and steps_ok:
            accepted.append(exercise)
            suffix = f" ({len(exercise.tutorial)} pasos)" if exercise.tutorial else ""
            print(f"{label} OK{suffix} -> {exercise.expected!r}")
        else:
            if not solutions_ok:
                state = "solucion"
            elif not starters_ok:
                state = "plantilla"
            else:
                state = "tutorial"
            print(f"{label} FALLO ({state})")

    if verifier.failures:
        print("\n" + "=" * 74)
        print(f"{len(verifier.failures)} problema(s):")
        for failure in verifier.failures:
            print(f"  - {failure}")
        print("=" * 74)
        return 1

    distribution = check_distribution(accepted)
    if distribution:
        print("\n" + "=" * 74)
        print("El reparto de ejercicios no cumple lo pedido:")
        for problem in distribution:
            print(f"  - {problem}")
        print("=" * 74)
        return 1

    payload = {
        "exercises": [
            {
                "Category": e.category,
                "Title": e.title,
                "Description": e.description,
                "Difficulty": e.difficulty,
                "ExpectedOutput": e.expected,
                "IsActive": True,
                "Inputs": e.inputs,
                "TutorialSteps": [
                    {
                        "Title": s.title,
                        "Body": s.body,
                        "Task": s.task,
                        "CodeSnippet": s.snippet,
                        "ExpectedOutput": s.expected,
                        "Stdin": s.stdin or None,
                        "Tip": s.tip,
                    }
                    for s in e.tutorial
                ],
                "Templates": [
                    {"LanguageSlug": lang, "StarterCode": e.starter[lang]} for lang in LANGUAGES
                ],
            }
            for e in accepted
        ]
    }

    with open(args.out, "w", encoding="utf-8") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
    print(f"\nJSON escrito en {args.out}")

    by_category: dict[str, dict[str, int]] = {}
    for exercise in accepted:
        counts = by_category.setdefault(exercise.category, {})
        counts[exercise.difficulty] = counts.get(exercise.difficulty, 0) + 1

    print(f"{len(accepted)} ejercicios listos para sembrar:")
    for category, counts in sorted(by_category.items()):
        breakdown = ", ".join(f"{d}={counts.get(d, 0)}" for d in ("Easy", "Medium", "Hard"))
        print(f"  {category}: {sum(counts.values())} ({breakdown})")

    total_steps = sum(len(e.tutorial) for e in accepted)
    if total_steps:
        print(f"{total_steps} pasos de tutorial verificados.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())