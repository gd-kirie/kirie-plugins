# AGENTS

Use README.md for repository setup and package READMEs for public APIs.

- Keep WebView, IPC, and CLI implementation in gd-kirie.
- Depend on published core packages. Do not add implicit sibling-repository dependencies.
- Plugins borrow the application's Eventa context and WebView owner.
- Keep TypeScript and C# contracts aligned. Preserve public APIs when moving code.
- Run tools through mise and run the relevant lint, build, and test tasks after changes.
- Edit notifications/src-godot TypeScript and regenerate GDScript. Do not hand-edit generated output.
- Use snake_case and explicit object keys in src-godot TypeScript.
- Keep native artifacts, installed dependencies, and generated output out of Git.
- Use English for documentation and apply the simple-english skill managed by apm.yml.
- Use Conventional Commits subjects and the user's Git author and committer identity.
- Change this file only when the user explicitly requests it.
