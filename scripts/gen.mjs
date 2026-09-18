// Regenerates all projects by running tsp compile for each tspconfig.yaml under src/ and tests/.
// Each project uses its vertical's spec at src/<vertical>/spec/api.tsp.
// Temporary TypeSpec output goes to tsp-output/; generated C# goes to each project's generated/ folder.
import { execFileSync } from "node:child_process";
import { existsSync, readdirSync } from "node:fs";
import { join } from "node:path";

const projectDirectories = (parent) =>
  existsSync(parent)
    ? readdirSync(parent, { withFileTypes: true })
        .filter((d) => d.isDirectory() && existsSync(join(parent, d.name, "tspconfig.yaml")))
        .map((d) => join(parent, d.name))
    : [];

const verticals = readdirSync("src", { withFileTypes: true }).filter((d) => d.isDirectory() && existsSync(join("src", d.name, "spec", "api.tsp")));
const projects = [...verticals.flatMap((v) => projectDirectories(join("src", v.name))), ...projectDirectories("tests")].sort();
if (projects.length === 0) {
  console.error("No tspconfig.yaml found under src/<vertical>/*/ or tests/*/.");
  process.exit(1);
}

for (const project of projects) {
  const name = project.split(/[\\/]/).at(-1);
  const vertical = name.split(".")[1]?.toLowerCase();
  const spec = join("src", vertical ?? "", "spec", "api.tsp");
  if (!vertical || !existsSync(spec)) {
    console.error(`${project}: cannot derive the vertical from the project name (expected FocusTemplate.<Vertical>.*, spec at ${spec}).`);
    process.exit(1);
  }

  console.log(`\n=== ${project}`);
  execFileSync("tsp", ["compile", spec, "--config", join(project, "tspconfig.yaml"), "--output-dir", join("tsp-output", project)], { stdio: "inherit", shell: true });
}
