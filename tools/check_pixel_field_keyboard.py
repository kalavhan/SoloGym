#!/usr/bin/env python3
"""Drive fictional keyboard input in a dedicated Xvfb Unity review player."""
import ctypes as c
import os
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]


def run():
    actions = "--actions" in sys.argv
    icons = "--icons" in sys.argv
    choices = "--choices" in sys.argv
    characters = "--characters" in sys.argv
    navigation = "--navigation" in sys.argv
    login = "--login" in sys.argv
    account = "--account" in sys.argv
    if sum((actions, icons, choices, characters, navigation, login, account)) > 1:
        raise ValueError("Choose one keyboard fixture, including --login or --account.")
    fixture = "account" if account else "login" if login else "navigation" if navigation else "character" if characters else "choice" if choices else "icon" if icons else "action" if actions else "field"
    # Always create an isolated display; never inject events into the user's desktop.
    if "--isolated-child" not in sys.argv:
        return subprocess.call(["xvfb-run", "-a", "-s", "-screen 0 1920x1080x24",
                                sys.executable, str(Path(__file__).resolve()), "--isolated-child",
                                *(["--account"] if account else ["--login"] if login else ["--navigation"] if navigation else ["--characters"] if characters else ["--choices"] if choices else ["--icons"] if icons else ["--actions"] if actions else [])],
                               env={**os.environ, "SOLOGYM_ISOLATED_FIELD_TEST": "1"})
    if os.environ.get("SOLOGYM_ISOLATED_FIELD_TEST") != "1":
        raise RuntimeError("Run without --isolated-child to create a private display.")
    local = ROOT / "artifacts/local"
    local.mkdir(parents=True, exist_ok=True)
    marker = local / (fixture + "-keyboard")
    for suffix in (".ready", ".done"):
        Path(str(marker) + suffix).unlink(missing_ok=True)
    x11 = c.CDLL("libX11.so.6")
    xtst = c.CDLL("libXtst.so.6")
    x11.XOpenDisplay.restype = c.c_void_p
    x11.XOpenDisplay.argtypes = [c.c_char_p]
    x11.XStringToKeysym.restype = c.c_ulong
    x11.XStringToKeysym.argtypes = [c.c_char_p]
    x11.XKeysymToKeycode.restype = c.c_uint
    x11.XKeysymToKeycode.argtypes = [c.c_void_p, c.c_ulong]
    x11.XFlush.argtypes = [c.c_void_p]
    x11.XCloseDisplay.argtypes = [c.c_void_p]
    x11.XDefaultRootWindow.argtypes = [c.c_void_p]
    x11.XDefaultRootWindow.restype = c.c_ulong
    x11.XQueryTree.argtypes = [c.c_void_p, c.c_ulong, c.POINTER(c.c_ulong), c.POINTER(c.c_ulong),
                              c.POINTER(c.POINTER(c.c_ulong)), c.POINTER(c.c_uint)]
    x11.XFetchName.argtypes = [c.c_void_p, c.c_ulong, c.POINTER(c.c_char_p)]
    x11.XSetInputFocus.argtypes = [c.c_void_p, c.c_ulong, c.c_int, c.c_ulong]
    x11.XRaiseWindow.argtypes = [c.c_void_p, c.c_ulong]
    x11.XFree.argtypes = [c.c_void_p]
    xtst.XTestFakeKeyEvent.argtypes = [c.c_void_p, c.c_uint, c.c_int, c.c_ulong]
    display = x11.XOpenDisplay(None)
    if not display:
        raise RuntimeError("Could not open the private Xvfb display.")

    def event(name, down):
        key = x11.XKeysymToKeycode(display, x11.XStringToKeysym(name.encode("ascii")))
        if not key:
            raise RuntimeError("Unknown test key: " + name)
        xtst.XTestFakeKeyEvent(display, key, int(down), 0)
        x11.XFlush(display)
        time.sleep(.045)

    def key(name, shift=False):
        if shift:
            event("Shift_L", True)
        event(name, True)
        event(name, False)
        if shift:
            event("Shift_L", False)

    def text(value):
        symbols = {"+": ("equal", True), "@": ("2", True), ".": ("period", False)}
        for char in value:
            name, shift = symbols.get(char, (char.lower(), char.isupper()))
            key(name, shift)

    env = {**os.environ, "XDG_CONFIG_HOME": str(local / (fixture + "-keyboard-prefs"))}
    binary = {"account": "Login/SoloGymLogin", "login": "Login/SoloGymLogin", "navigation": "FantasyNavigation/SoloGymNavigation", "character": "FantasyCharacter/SoloGymCharacter", "choice": "FantasyChoice/SoloGymChoice", "icon": "FantasyIcon/SoloGymIcon",
              "action": "FantasyAction/SoloGymAction", "field": "FantasyField/SoloGymField"}[fixture]
    command = [str(ROOT / ("app/Builds/" + binary + ".x86_64")),
               "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720",
               "-sologym-locale", "en", "-sologym-keyboard-probe", str(marker),
               "-sologym-capture", str(local / (fixture + "-keyboard.png")),
               "-logFile", str(local / (fixture + "-keyboard-player.log"))]
    if account:
        command += ["-sologym-window", "account"]
    with (local / (fixture + "-keyboard-stdout.log")).open("w") as output:
        player = subprocess.Popen(command, cwd=ROOT, env=env, stdout=output, stderr=output)
        try:
            deadline = time.monotonic() + 20
            while not Path(str(marker) + ".ready").exists():
                if player.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError("Unity keyboard probe did not become ready.")
                time.sleep(.1)
            # Xvfb has no window manager to focus a newly mapped Unity window.
            root, parent, count = c.c_ulong(), c.c_ulong(), c.c_uint()
            children = c.POINTER(c.c_ulong)()
            x11.XQueryTree(display, x11.XDefaultRootWindow(display), c.byref(root), c.byref(parent), c.byref(children), c.byref(count))
            window = None
            try:
                for i in range(count.value):
                    name = c.c_char_p()
                    if x11.XFetchName(display, children[i], c.byref(name)) and name.value:
                        window = children[i]
                        x11.XFree(name)
            finally:
                if children:
                    x11.XFree(children)
            if window is None:
                raise RuntimeError("The private display has no named Unity window.")
            x11.XRaiseWindow(display, window)
            x11.XSetInputFocus(display, window, 2, 0)
            x11.XFlush(display)
            time.sleep(.4)
            if account:
                text("hero+fit@example.com")
                key("Return"); time.sleep(.4)
                text("Trial9pass")
                key("Return"); time.sleep(.4)
                text("Trial9pass")
                key("Tab"); time.sleep(.3)
                key("Return"); time.sleep(.3)
                key("Tab", shift=True); time.sleep(.3)
                key("Return"); time.sleep(.4)
            elif login:
                text("hero+fit@example.com")
                key("Return"); time.sleep(.4)
                text("Trial9pass")
                key("Tab"); time.sleep(.3)
                key("Return"); time.sleep(.3)
                key("Tab", shift=True); time.sleep(.3)
                key("Return"); time.sleep(.4)
            elif navigation:
                # Adult opt-in, three accepted routes, locale, then teen removes the current fasting route.
                sequence = [(name, False) for name in ["Right", "Return", "Tab", "Right", "Return", "Right", "Return", "Right", "Return", "Tab", "Return"]]
                sequence += [("Tab", True)] * 5 + [("Left", False), ("Left", False), ("Return", False)]
                for name, shift in sequence:
                    key(name, shift); time.sleep(.3)
            elif characters:
                # Male, Skinny, Muscular; then locale. Focus alone never changes the sprite.
                for name in ["Right", "Return", "Tab", "Return", "Right", "Right", "Right", "Return", "Tab", "Return"]:
                    key(name); time.sleep(.3)
            elif choices:
                # Choose Hard, advance to rest, change appearance, advance to pause,
                # then choose Medium and Easy without restarting the fictional routine.
                sequence = [("Right", False), ("Return", False), ("Tab", False), ("Return", False),
                            ("Tab", False), ("Return", False), ("Right", False), ("Right", False), ("Return", False),
                            ("Tab", True), ("Tab", True), ("Tab", True), ("Return", False),
                            ("Tab", True), ("Return", False), ("Left", False), ("Return", False),
                            ("Left", False), ("Return", False)]
                for name, shift in sequence:
                    key(name, shift); time.sleep(.3)
            elif icons:
                # Back -> Settings -> Normal/Back, reverse to locale, then skip Disabled to Large.
                for name, shift in [("Tab", False), ("Return", False), ("Tab", False), ("Return", False),
                                    ("Tab", True), ("Return", False), ("Tab", False), ("Tab", False),
                                    ("Tab", False), ("Tab", False), ("Return", False)]:
                    key(name, shift); time.sleep(.3)
            elif actions:
                # Back -> primary -> recovery -> email -> Back -> recovery.
                # Then reverse once, skip the two unavailable samples, and change locale.
                for name, shift in [("Tab", False), ("Tab", False), ("Return", False),
                                    ("Tab", False), ("Return", False), ("Tab", True),
                                    ("Tab", False), ("Tab", False), ("Tab", False), ("Return", False)]:
                    key(name, shift); time.sleep(.3)
            else:
                text("hero+fit@example.com")
                key("Tab"); time.sleep(.3)
                text("Trial9pass")
                key("Tab"); time.sleep(.3)
                key("Return"); time.sleep(.3)
                key("Tab", shift=True); time.sleep(.3)
                key("Return"); time.sleep(.3)
            Path(str(marker) + ".done").write_text("done")
            result = player.wait(timeout=15)
            print("OS keyboard probe:", "PASS" if result == 0 else "FAIL")
            return result
        finally:
            if player.poll() is None:
                player.terminate()
                player.wait(timeout=5)
            x11.XCloseDisplay(display)


if __name__ == "__main__":
    raise SystemExit(run())
