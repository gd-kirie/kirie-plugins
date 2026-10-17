import { execFileSync } from "node:child_process";
import { copyFileSync, cpSync, mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import process from "node:process";

import packageJson from "../packages/tray/package.json" with { type: "json" };

const nativeLibraries: Record<string, string> = {
  "aarch64-apple-darwin": "libkirie_tray_native.dylib",
  "x86_64-apple-darwin": "libkirie_tray_native.dylib",
  "aarch64-unknown-linux-gnu": "libkirie_tray_native.so",
  "x86_64-unknown-linux-gnu": "libkirie_tray_native.so",
  "aarch64-pc-windows-msvc": "kirie_tray_native.dll",
  "x86_64-pc-windows-msvc": "kirie_tray_native.dll",
};

const rootDir = process.cwd();
const target = execFileSync("rustc", ["--print", "host-tuple"], {
  cwd: rootDir,
  encoding: "utf8",
}).trim();
const fileName = nativeLibraries[target];
if (!fileName) {
  throw new Error(`Unsupported tray native build target: ${target}`);
}

execFileSync(
  "cargo",
  [
    "build",
    "--package",
    "kirie-tray-native",
    "--target",
    target,
    "--target-dir",
    path.join(rootDir, "target"),
    "--locked",
    "--release",
  ],
  { cwd: rootDir, stdio: "inherit" },
);

const source = path.join(rootDir, "target", target, "release", fileName);
const addonRoot = path.join(rootDir, "packages/tray/addon");
const addonDir = path.join(addonRoot, "kirie_tray");
const destinationDir = path.join(addonDir, "bin", target);
const destination = path.join(destinationDir, fileName);

mkdirSync(destinationDir, { recursive: true });
copyFileSync(source, destination);

writeFileSync(path.join(addonRoot, ".gdignore"), "");
cpSync(path.join(rootDir, "packages/tray/gdextension"), addonDir, { recursive: true });
writeFileSync(
  path.join(addonDir, "plugin.cfg"),
  `[plugin]\nname="Kirie Tray"\ndescription="Native Tray GDExtension"\nauthor="Moeru AI"\nversion="${packageJson.version}"\n`,
);

console.log(`Assembled Tray GDExtension: ${path.relative(rootDir, destination)}`);
