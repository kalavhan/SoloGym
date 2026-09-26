#!/usr/bin/env python3
"""Install the pinned, official Firebase Auth Unity package, preserving vendor metadata.

No credentials or cloud API calls. Run with --archive to reuse a downloaded ZIP.
Vendor assets remain ignored; this script is the reproducible dependency manifest.
"""
import argparse
import hashlib
import io
from pathlib import Path, PurePosixPath
import tarfile
import urllib.request
import zipfile

VERSION = "13.17.0"
URL = f"https://dl.google.com/firebase/sdk/unity/firebase_unity_sdk_{VERSION}.zip"
SHA256 = "4fba6b1315eb24987dcf5a391c9f88518ea9c3c0eba6c7e45d88a991a034f7c0"
ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path)
    args = parser.parse_args()
    archive = args.archive or ROOT / "artifacts/local" / f"firebase_unity_sdk_{VERSION}.zip"
    if not archive.exists():
        archive.parent.mkdir(parents=True, exist_ok=True)
        print(f"Downloading official Firebase {VERSION} (~726 MiB)")
        urllib.request.urlretrieve(URL, archive)
    with archive.open("rb") as stream:
        checksum = hashlib.file_digest(stream, "sha256").hexdigest()
    if checksum != SHA256:
        raise SystemExit("Firebase archive checksum mismatch; refusing to install.")
    with zipfile.ZipFile(archive) as bundle:
        payload = bundle.read("firebase_unity_sdk/FirebaseAuth.unitypackage")
        license_dir = ROOT / "app/Assets/Firebase"
        license_dir.mkdir(parents=True, exist_ok=True)
        (license_dir / "SDK-LICENSE.txt").write_bytes(bundle.read("firebase_unity_sdk/LICENSE"))
    count = 0
    with tarfile.open(fileobj=io.BytesIO(payload), mode="r:gz") as package:
        members = {member.name: member for member in package.getmembers()}
        for name, member in members.items():
            if not name.endswith("/pathname"):
                continue
            raw_path = package.extractfile(member).read().decode("utf-8").strip()
            relative = PurePosixPath(raw_path)
            if relative.is_absolute() or ".." in relative.parts or relative.parts[0] != "Assets":
                raise SystemExit("Unsafe path in Firebase package.")
            destination = ROOT / "app" / str(relative)
            prefix = name.rsplit("/", 1)[0]
            asset = members.get(prefix + "/asset")
            meta = members.get(prefix + "/asset.meta")
            if asset:
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(package.extractfile(asset).read())
                count += 1
            else:
                destination.mkdir(parents=True, exist_ok=True)
            if meta:
                Path(str(destination) + ".meta").write_bytes(package.extractfile(meta).read())
    print(f"Installed {count} Firebase/EDM assets from SHA256 {checksum}.")
    print("Open Unity to import. SoloGym player builds select SOLOGYM_FIREBASE_AUTH when the SDK is installed.")


if __name__ == "__main__":
    main()
