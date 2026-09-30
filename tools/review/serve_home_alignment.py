#!/usr/bin/env python3
"""Loopback-only, read-only server for the Home alignment tool and its exact art inputs."""
import argparse
import json
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[2]
RESOURCES = ROOT / "app/Assets/SoloGym/Resources"

def allowed_files():
    paths = {"/", "/tools/review/home-alignment-editor.html", "/tools/review/home-alignment-editor.css",
             "/tools/review/home-alignment-editor.mjs", "/tools/review/home-layout.mjs",
             "/design/fantasy-home-r2/home-approved.png"}
    def add_resource(path):
        paths.add("/app/Assets/SoloGym/Resources/" + path)
    for path in ["Rooms/RefugeR1/room.json", "Rooms/RefugeR1/objects.json", "Characters/BarbarianR1/catalog.json"]:
        add_resource(path)
    room = json.loads((RESOURCES / "Rooms/RefugeR1/room.json").read_text())
    for key in ("architecture", "exterior"):
        add_resource(room[key] + ".png")
    catalog = json.loads((RESOURCES / "Rooms/RefugeR1/objects.json").read_text())
    for item in catalog["objects"]:
        add_resource(item["resource"] + ".json")
        definition = json.loads((RESOURCES / (item["resource"] + ".json")).read_text())
        for variant in definition["variants"]:
            add_resource(variant["resource"] + ".png")
    characters = json.loads((RESOURCES / "Characters/BarbarianR1/catalog.json").read_text())
    for character in characters["characters"]:
        add_resource(character["resource"] + ".png")
    return paths

class Handler(SimpleHTTPRequestHandler):
    allowed = allowed_files()
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)
    def do_GET(self):
        path = unquote(urlsplit(self.path).path)
        if path not in self.allowed:
            self.send_error(404)
            return
        self.path = "/tools/review/home-alignment-editor.html" if path == "/" else path
        super().do_GET()
    def do_HEAD(self):
        path = unquote(urlsplit(self.path).path)
        if path not in self.allowed:
            self.send_error(404)
            return
        self.path = "/tools/review/home-alignment-editor.html" if path == "/" else path
        super().do_HEAD()
    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        super().end_headers()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8903)
    args = parser.parse_args()
    Handler.extensions_map = {**Handler.extensions_map, ".mjs": "text/javascript"}
    print(f"Home alignment: http://127.0.0.1:{args.port}/", flush=True)
    ThreadingHTTPServer(("127.0.0.1", args.port), Handler).serve_forever()
