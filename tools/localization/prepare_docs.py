#!/usr/bin/env python3
"""Keep translated routes complete with clearly marked English fallback pages."""
from pathlib import Path
import re
import shutil

docs = Path(__file__).resolve().parents[2] / "docs"
sources = [p for p in docs.rglob("*.md") if not {"l10n", ".vuepress", "node_modules"} & set(p.relative_to(docs).parts)]
marker = "---\nmccEnglishFallback: true\n---"
root = docs / "l10n"
if root.exists():
    for locale in root.iterdir():
        if not locale.is_dir() or not (locale / "index.md").exists():
            continue
        for source in sources:
            destination = locale / source.relative_to(docs)
            if destination.exists() and marker not in destination.read_text():
                continue
            destination.parent.mkdir(parents=True, exist_ok=True)
            text = source.read_text()
            text = re.sub(r"(\]\()/(?!/)", r"\1/l10n/" + locale.name + "/", text)
            destination.write_text(marker + "\n\n> This page is not translated yet. It uses the current English instructions.\n\n" + text)
print("Documentation locale routes prepared.")

examples = docs / "examples"
if examples.exists():
    for source in examples.rglob("*"):
        if source.is_file() and source.suffix != ".md":
            destination = docs / ".vuepress/public/examples" / source.relative_to(examples)
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
            if root.exists():
                for locale in root.iterdir():
                    if locale.is_dir() and (locale / "index.md").exists():
                        localized = docs / ".vuepress/public/l10n" / locale.name / "examples" / source.relative_to(examples)
                        localized.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copy2(source, localized)
                        local_source = locale / "examples" / source.relative_to(examples)
                        local_source.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copy2(source, local_source)
print("Downloadable example files prepared.")
