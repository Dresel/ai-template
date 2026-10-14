// Regenerates all projects by running tsp compile for each tspconfig.yaml under src/ and tests/.
// A project inside a vertical, src/<vertical>/FocusTemplate.<Vertical>.*, and a test project compile that vertical's spec at
// src/<vertical>/spec/api.tsp. A project of a package with a spec of its own, a feature (src/features/<feature>/*) or a
// part of a vertical (src/admin/user-management/*), compiles the package's spec/api.tsp. A project of the shared spine,
// src/FocusTemplate.<Name>, compiles
// src/spec/<name>.tsp: FocusTemplate.Primitives over primitives.tsp, the typed ids every vertical imports, and
// FocusTemplate.Http over http.tsp, the library types. The tspconfig.yaml in a spec/ folder is the editor's project
// (kind: project), which names the file the language server compiles, and generates nothing.
// Temporary TypeSpec output goes to tsp-output/, generated C# to each project's generated/ folder.
import { execFileSync } from "node:child_process";
import { existsSync, readdirSync } from "node:fs";
import { join } from "node:path";

const projectDirectories = (parent) =>
  existsSync(parent)
    ? readdirSync(parent, { withFileTypes: true })
        .filter((d) => d.isDirectory() && d.name !== "spec" && existsSync(join(parent, d.name, "tspconfig.yaml")))
        .map((d) => join(parent, d.name))
    : [];

const specDirectories = (parent) =>
  existsSync(parent)
    ? readdirSync(parent, { withFileTypes: true })
        .filter((d) => d.isDirectory() && existsSync(join(parent, d.name, "spec", "api.tsp")))
        .map((d) => join(parent, d.name))
    : [];

const spine = projectDirectories("src");
const verticals = specDirectories("src");
const packageSpecs = new Map(
  [join("src", "features"), ...verticals]
    .flatMap((parent) => specDirectories(parent))
    .flatMap((spec) => projectDirectories(spec).map((project) => [project, join(spec, "spec", "api.tsp")])),
);
const projects = [
  ...spine,
  ...verticals.flatMap((vertical) => projectDirectories(vertical)),
  ...packageSpecs.keys(),
  ...projectDirectories("tests"),
].sort();
if (projects.length === 0) {
  console.error("No tspconfig.yaml found under src/, src/<vertical>/*/, src/<vertical>/<package>/*/, src/features/<feature>/*/ or tests/*/.");
  process.exit(1);
}

for (const project of projects) {
  const name = project.split(/[\/]/).at(-1);
  const segment = name.split(".")[1]?.toLowerCase();
  const spec =
    packageSpecs.get(project) ??
    (spine.includes(project) ? join("src", "spec", `${segment ?? ""}.tsp`) : join("src", segment ?? "", "spec", "api.tsp"));
  if (!segment || !existsSync(spec)) {
    console.error(`${project}: cannot derive the spec from the project name (FocusTemplate.<Vertical>.* compiles src/<vertical>/spec/api.tsp, FocusTemplate.<Name> compiles src/spec/<name>.tsp; looked for ${spec}).`);
    process.exit(1);
  }

  console.log(`\n=== ${project}`);
  execFileSync("tsp", ["compile", spec, "--config", join(project, "tspconfig.yaml"), "--output-dir", join("tsp-output", project)], { stdio: "inherit", shell: true });
}
