"""Adds a plugin version to a Jellyfin repository manifest.json (creating it if needed)."""
import json
import os
import sys
from datetime import datetime, timezone

PLUGIN = {
    "guid": "b6f3c2a4-7d1e-4f8a-9c3b-2e5d8a1f4c70",
    "name": "No Season Parsing",
    "description": "Disables filename-based season detection for selected folders.",
    "overview": "Assigns seasons by year or a fixed number instead of parsing file names.",
    "owner": "Tabisch",
    "category": "Metadata",
}


def main(manifest_path, version, target_abi, source_url, checksum, changelog):
    manifest = []
    if os.path.exists(manifest_path):
        with open(manifest_path, encoding="utf-8") as f:
            manifest = json.load(f)

    entry = next((p for p in manifest if p.get("guid") == PLUGIN["guid"]), None)
    if entry is None:
        entry = {**PLUGIN, "versions": []}
        manifest.append(entry)
    else:
        entry.update(PLUGIN)

    # Replace an existing entry for the same version, newest first.
    versions = [v for v in entry.get("versions", []) if v.get("version") != version]
    versions.insert(0, {
        "version": version,
        "changelog": changelog,
        "targetAbi": target_abi,
        "sourceUrl": source_url,
        "checksum": checksum,
        "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    })
    entry["versions"] = versions

    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
        f.write("\n")


if __name__ == "__main__":
    main(*sys.argv[1:7])
