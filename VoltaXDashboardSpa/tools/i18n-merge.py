"""Merges translation fragments into src/assets/i18n/{en,fr,ar}.json.

Usage: python tools/i18n-merge.py fragment.json [fragment2.json ...]
A fragment is {"en": {...}, "fr": {...}, "ar": {...}} with the same key tree in each language.
Existing keys are overwritten, others kept; files are written sorted by insertion, UTF-8, 2 spaces.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "src" / "assets" / "i18n"


def merge(target: dict, source: dict) -> None:
    for key, value in source.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            merge(target[key], value)
        else:
            target[key] = value


def keys(tree: dict, prefix: str = "") -> set:
    out = set()
    for key, value in tree.items():
        path = f"{prefix}.{key}" if prefix else key
        out |= keys(value, path) if isinstance(value, dict) else {path}
    return out


def main(paths: list) -> None:
    for path in paths:
        fragment = json.loads(Path(path).read_text(encoding="utf-8"))
        languages = list(fragment)
        reference = keys(fragment[languages[0]])
        for language in languages[1:]:
            missing = reference ^ keys(fragment[language])
            if missing:
                raise SystemExit(f"{path}: {language} differs from {languages[0]}: {sorted(missing)[:20]}")
        for language, tree in fragment.items():
            file = ROOT / f"{language}.json"
            data = json.loads(file.read_text(encoding="utf-8"))
            merge(data, tree)
            file.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    sets = {f.stem: keys(json.loads(f.read_text(encoding="utf-8"))) for f in ROOT.glob("*.json")}
    base = sets.get("en", set())
    for name, found in sets.items():
        if found != base:
            print(f"warning: {name}.json differs from en.json: {sorted(found ^ base)[:20]}")
    print("merged", ", ".join(paths))


if __name__ == "__main__":
    main(sys.argv[1:])
