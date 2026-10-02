/**
 * Siembra el catalogo de ejercicios desde exercises.json contra la API de
 * administracion.
 *
 * El JSON lo genera build_exercises.py, que ya ejecuto las soluciones en Python,
 * Java y PSeint y fijo el ExpectedOutput a partir de la salida real. Aqui solo se
 * traducen los nombres de categoria y lenguaje a los ids que exige la API.
 *
 * Es idempotente: si un ejercicio con el mismo titulo ya existe, lo actualiza con
 * PUT en lugar de duplicarlo. Asi se puede relanzar sin miedo.
 *
 * Uso:
 *   PS_API_URL=https://psacademyback.onrender.com \
 *   PS_ADMIN_EMAIL=admin@psacademy.com \
 *   PS_ADMIN_PASSWORD=**** \
 *   node seed-exercises.mjs
 *
 * Variables opcionales:
 *   PS_DRY_RUN=1   solo muestra lo que haria, sin enviar nada
 *   PS_FILE=...    ruta alternativa a exercises.json
 */

import { readFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));

const apiUrl = (process.env.PS_API_URL ?? "http://localhost:5000").replace(/\/+$/, "");
const email = process.env.PS_ADMIN_EMAIL;
const password = process.env.PS_ADMIN_PASSWORD;
const dryRun = process.env.PS_DRY_RUN === "1";
const sourceFile = process.env.PS_FILE ?? join(here, "exercises.json");

if (!email || !password) {
  console.error("Faltan PS_ADMIN_EMAIL y PS_ADMIN_PASSWORD.");
  process.exit(1);
}

async function api(path, { token, method = "GET", body } = {}) {
  const response = await fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      ...(body ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...(body ? { body: JSON.stringify(body) } : {}),
  });

  const text = await response.text();
  const payload = text ? JSON.parse(text) : null;

  if (!response.ok) {
    const detail = payload?.detail ?? payload?.title ?? text;
    throw new Error(`${method} ${path} -> ${response.status}: ${detail}`);
  }

  return payload;
}

const token = (await api("/api/auth/login", { method: "POST", body: { email, password } })).token;

const categories = await api("/api/admin/categories", { token });
const languages = await api("/api/languages", { token });

const categoryIdByName = new Map(categories.map((c) => [c.name.toLowerCase(), c.id]));
const languageIdBySlug = new Map(languages.map((l) => [l.slug.toLowerCase(), l.id]));

const { exercises } = JSON.parse(await readFile(sourceFile, "utf8"));

const existing = await api("/api/admin/exercises", { token });
const existingByTitle = new Map(existing.map((e) => [e.title.trim().toLowerCase(), e]));

let created = 0;
let updated = 0;
let skipped = 0;

for (const exercise of exercises) {
  const categoryId = categoryIdByName.get(exercise.Category.toLowerCase());
  if (!categoryId) {
    throw new Error(`La categoría '${exercise.Category}' no existe en la API.`);
  }

  const templates = exercise.Templates.map((template) => {
    const languageId = languageIdBySlug.get(template.LanguageSlug.toLowerCase());
    if (!languageId) {
      throw new Error(`El lenguaje '${template.LanguageSlug}' no existe o esta inactivo.`);
    }
    return { languageId, starterCode: template.StarterCode };
  });

  const payload = {
    categoryId,
    title: exercise.Title,
    description: exercise.Description,
    difficulty: exercise.Difficulty,
    expectedOutput: exercise.ExpectedOutput,
    isActive: exercise.IsActive,
    inputs: exercise.Inputs,
    templates,
  };

  const current = existingByTitle.get(exercise.Title.trim().toLowerCase());
  const verb = current ? "PUT " : "POST";

  if (dryRun) {
    console.log(`${verb} /api/admin/exercises -> ${exercise.Title}`);
    continue;
  }

  await api(current ? `/api/admin/exercises/${current.id}` : "/api/admin/exercises", {
    token,
    method: current ? "PUT" : "POST",
    body: payload,
  });

  if (current) {
    updated += 1;
    console.log(`actualizado  ${exercise.Title}`);
  } else {
    created += 1;
    console.log(`creado       ${exercise.Title}`);
  }
}

// Los ejercicios que ya no estan en el JSON se desactivan, no se borran: borrar
// en cascada llevaria por delante el progreso de los alumnos que los resolvieron.
for (const stale of existing.filter(
  (e) => !exercises.some((x) => x.Title.trim().toLowerCase() === e.title.trim().toLowerCase()),
)) {
  if (dryRun) {
    console.log(`DELETE /api/admin/exercises/${stale.id} -> ${stale.title}`);
    continue;
  }

  await api(`/api/admin/exercises/${stale.id}`, { token, method: "DELETE" });
  skipped += 1;
  console.log(`desactivado  ${stale.title}`);
}

console.log(`\n${created} creados, ${updated} actualizados, ${skipped} desactivados.`);

const total = (await api("/api/admin/exercises", { token })).length;
console.log(`La API tiene ahora ${total} ejercicios.`);