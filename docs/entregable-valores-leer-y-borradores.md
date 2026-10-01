# Frontend integration guide: read values, code drafts and allowed languages

This document describes the API and behaviour changes already implemented and tested in
the PSAcademyBack backend, and what the frontend needs to consume.

## 1. Summary

Three changes are included:

1. **Read values (standard input).** For each exercise the admin defines an ordered list of
   values that the student's program receives on standard input. Students only see them.
2. **Code drafts.** The student's code is saved automatically per language. When they come
   back to the exercise it is restored where they left it, even if they never ran it.
3. **Allowed languages.** The catalogue is limited to **Java**, **Python** and
   **Pseudocode (PSeint)**. PSeint is executed server-side by translating it to Python.

> Enums are serialised as **strings**: `difficulty` (`"Easy" | "Medium" | "Hard"`) and
> `valueType` (`"Number" | "Text"`). JSON uses `camelCase`.

## 2. Languages

`GET /api/languages` now returns only the active languages:

```json
[
  { "id": 1, "name": "Python", "slug": "python", "isActive": true },
  { "id": 2, "name": "Java",   "slug": "java",   "isActive": true },
  { "id": 7, "name": "PSeint", "slug": "pseint", "isActive": true }
]
```

- `pseint` runs on the backend by translating it to Python. The frontend does nothing
  special: send `languageSlug: "pseint"` like any other language.
- Every other language is **inactive**. Executing with an inactive one returns `400`.

## 3. Read values (`inputs`)

### 3.1 Shape

```ts
type InputValueType = 'Number' | 'Text'

interface ExerciseInput {
  orderIndex: number
  value: string
  valueType: InputValueType
}
```

- `orderIndex` is **0-based** and defines the read order: index 0 is the first input line,
  index 1 the second, and so on.
- `valueType` is used for display and validation in the admin panel. The text travels the
  same on `stdin`; the actual type (integer/decimal) is decided by the program's `read`.

### 3.2 What the student receives

`GET /api/exercises/{id}` (public) always includes the array, already sorted:

```json
{
  "id": 1,
  "title": "Suma de dos números",
  "expectedOutput": "12",
  "inputs": [
    { "orderIndex": 0, "value": "5", "valueType": "Number" },
    { "orderIndex": 1, "value": "7", "valueType": "Number" }
  ]
}
```

On execution the backend joins the values with `\n`, adds a trailing `\n` and sends them as
`stdin`. Students do nothing: they just press "Run".

> Because the platform feeds the input automatically, **the program must not print prompts**
> (`Escribir "Ingresa tu numero:"` / `input("Enter a number: ")`), otherwise that extra line
> breaks the output comparison.

### 3.3 How the admin configures it

`POST /api/admin/exercises` and `PUT /api/admin/exercises/{id}` accept `inputs`. The PUT
**replaces the whole collection**; the array order is the read order.

```json
{
  "categoryId": 1,
  "title": "Suma de dos números",
  "description": "...",
  "difficulty": "Easy",
  "expectedOutput": "12",
  "isActive": true,
  "templates": [{ "languageId": 1, "starterCode": "..." }],
  "inputs": [
    { "value": "5", "valueType": "Number" },
    { "value": "7", "valueType": "Number" }
  ]
}
```

`GET /api/admin/exercises` and `GET /api/admin/exercises/{id}` return `inputs` with the same
shape as the public detail (`orderIndex`, `value`, `valueType`).

**Backend validation (400):**

- `value` is required, max 500 characters.
- `valueType` must be `Number` or `Text`.
- When `valueType` is `Number`, `value` must parse as a number (integer or decimal).

## 4. Code drafts (`drafts`)

### 4.1 Saving

`PUT /api/exercises/{id}/draft` — **JWT required**.

```json
// Request
{ "languageSlug": "python", "code": "print(1 + 1)" }

// 200 OK
{ "languageSlug": "python", "code": "print(1 + 1)", "updatedAt": "2026-10-01T03:55:45Z" }
```

It is an upsert on `(user, exercise, language)`: first call creates, later calls update.
`code` may legitimately be empty (the student cleared the editor).

Responses: `401` without a token, `404` if the exercise does not exist or is inactive,
`400` if the language does not exist or is inactive.

### 4.2 Restoring

`GET /api/exercises/{id}` includes `drafts` **only when the request carries a valid JWT**:

```json
{
  "id": 1,
  "inputs": [ ... ],
  "userStatus": "attempted",
  "drafts": [
    { "languageSlug": "python", "code": "print(1 + 1)", "updatedAt": "2026-10-01T03:55:45Z" }
  ]
}
```

Without a token `drafts` comes back as an empty array. The frontend should prefer the draft
over the template's `starterCode`, and fall back to the starter code when a language has no
draft.

## 5. Seeded exercises

The database ships **30 exercises**, 10 per category, each with 4 Easy, 4 Medium and 2 Hard:

| Category | Easy | Medium | Hard |
| --- | --- | --- | --- |
| Fundamentos | 4 | 4 | 2 |
| Estructuras de control | 4 | 4 | 2 |
| Funciones | 4 | 4 | 2 |

Every exercise has read values (42 in total) and templates for the three active languages.
The `starterCode` reads the values and leaves a `TODO` for the student.

## 6. PSeint notes

The translator supports a subset of the pseudocode:

- `Proceso`/`Algoritmo` … `FinProceso`/`FinAlgoritmo`.
- `Definir`, `Leer` (with cast), `Escribir` (including `Sin Saltar`).
- Assignment with `<-` or `=`.
- `Si` / `Sino` / `FinSi`, `Mientras` / `FinMientras`, `Repetir` / `Hasta Que`,
  `Para` / `Con Paso` / `FinPara`.
- Operators `Y`, `O`, `No`, `Mod`, `^`, `<>`.
- Functions `Raiz`, `Abs`, `Trunc`, `Redon`, `Longitud`, `Mayusculas`, `Minusculas`,
  `Aleatorio`.

`Escribir` with several values concatenates them **without a separator**
(`Escribir "Hola, ", nombre` → `Hola, Ana`), so add explicit spaces if you need them.

If the student uses something unsupported (`Dimension`, `Segun`, `SubProceso`, `Funcion`, or
an instruction on the same line as a block header), the backend does **not** fail with 502:
it returns `200` with `hasError: true` and a message in `errorOutput` including the line
number. The frontend already surfaces that message.

## 7. Operational notes

- Exercises without read values are executed with no `stdin`.
- An executor infrastructure failure (Piston down, network timeout) returns `502` and does
  **not** record progress.
- Progress remains monotonic: a later failed attempt never downgrades a `completed`
  exercise.