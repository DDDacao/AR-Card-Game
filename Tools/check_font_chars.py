# -*- coding: utf-8 -*-
import re
import glob
import os

root = r"F:/AR-Card-Game"

def collect_game_chars():
    chars = set()
    for p in glob.glob(root + r"/Assets/Game Data/**/*.asset", recursive=True):
        t = open(p, encoding="utf-8", errors="ignore").read()
        for m in re.findall(r"\\u([0-9a-fA-F]{4})", t):
            chars.add(chr(int(m, 16)))
    for p in glob.glob(root + r"/Assets/Scripts/**/*.cs", recursive=True):
        t = open(p, encoding="utf-8", errors="ignore").read()
        for ch in t:
            if "\u4e00" <= ch <= "\u9fff":
                chars.add(ch)
    # common UI digits/punct used with Chinese
    for ch in "0123456789·：；，。！？/()（）":
        chars.add(ch)
    return chars

def chars_in_font_asset(path):
    t = open(path, encoding="utf-8", errors="ignore").read()
    found = set()
    for m in re.findall(r"m_Unicode: (\d+)", t):
        found.add(chr(int(m)))
    return found

def main():
    need = collect_game_chars()
    print("Game CJK/needed count:", len(need))
    print("Chars:", "".join(sorted(need)))
    for name in ["2.asset", "3.asset", "ziti.asset"]:
        path = os.path.join(root, "Assets/Scripts/Utilities", name)
        have = chars_in_font_asset(path)
        missing = sorted(ch for ch in need if ch not in have and ("\u4e00" <= ch <= "\u9fff" or ch.isalnum()))
        print(f"\n{name}: baked={len(have)} missing_from_need={len(missing)}")
        print(" missing:", "".join(missing[:80]))
        print(" have sample:", "".join(sorted(have)[:60]))

if __name__ == "__main__":
    main()
