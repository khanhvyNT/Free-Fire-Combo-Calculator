#!/usr/bin/env python3
"""
combo_codec.py - encoder/decoder cho ma combo nhan vat "#FF#<base64>".

DA KIEM CHUNG: chay `python3 combo_codec.py --selftest` phai bao PASS toan bo
(cac ma that do nguoi dung cung cap). Moi ma duoc IN RA TU CODE - KHONG BAO GIO
go/chep tay chuoi ma (lan dau da tung chep sai).

Dung nhanh:
  python3 combo_codec.py --selftest
  python3 combo_codec.py --encode Kepo Elena Kelly Dasha --shop --pet 2
  python3 combo_codec.py --encode Pre none none none --pet 1
  python3 combo_codec.py --decode "#FF#fmJweHxpfG1ucnR/..."
"""
import argparse
import base64
import json
import os
import sys

# ---------------------------------------------------------------- khoa (chu ky 22)
# byte_ma[i] = ban_ro[i] XOR KEY[(n-1-i) % 22],  n = so byte, danh so tu PHAI sang trai.
KEY = [126, 112, 117, 126, 100, 101, 110, 98, 121, 112, 99,
       116, 110, 114, 126, 117, 116, 110, 122, 116, 104, 125]

# ---------------------------------------------------------------- khoi nhan vat
# Khoi la day so sau dau phan cach 23 cua slot. Khoi 4 byte hoac 5 byte.
BLOCKS = {
    "Xayne": [5, 5, 1, 7],
    "Kelly": [3, 4, 1, 0],
    "Rin":   [6, 4, 1, 7],
    "Ignis": [7, 6, 1, 7],
    "Dasha": [2, 4, 1, 7],
    "Alok":  [3, 3, 1, 0, 7],
    "Tatsuya":     [9, 4, 1, 1, 4],
    "Tat (Pre)":   [9, 4, 1, 1, 4],
    "Alok (Pre)":  [7, 5, 1, 1, 5],
    "BHXK":        [0, 6, 1, 1, 8],
    "HoaCau":      [0, 6, 1, 0, 1],
    "Phun lửa":    [0, 6, 1, 0, 1],
    "Chidori":     [0, 6, 1, 1, 9],
}
NONE = "none"


def load_characters():
    json_path = os.path.join(os.path.dirname(__file__), "characters.json")
    if os.path.exists(json_path):
        try:
            with open(json_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                if "characters" in data:
                    BLOCKS.update(data["characters"])
        except Exception:
            pass


def save_character(name, block):
    BLOCKS[name] = block
    json_path = os.path.join(os.path.dirname(__file__), "characters.json")
    data = {"characters": {}}
    if os.path.exists(json_path):
        try:
            with open(json_path, "r", encoding="utf-8") as f:
                data = json.load(f)
        except Exception:
            pass
    if "characters" not in data:
        data["characters"] = {}
    data["characters"][name] = block
    try:
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
    except Exception:
        pass


load_characters()

# ---------------------------------------------------------------- cac doan co dinh
SEP = 23                       # dau phan cach slot
NONE_SEG = [1]                 # slot none = [23, 1]
SHOP_SEG = [4, 1, 1, 1, 1, 1, 1, 1, 5]      # shop tool (chi thay 1 bien the)
FIXED_SEG = [0, 1, 4]          # doan co dinh truoc slot pet
PET_SEG = {
    0: [1],                                  # Không Pet
    1: [0, 2, 0, 4, 1, 1, 1, 1, 1, 8],       # Cánh cụt
    2: [0, 2, 0, 4, 1, 1, 1, 1, 0, 1],       # Ottero
}
PET_NAMES = {
    0: "Không Pet",
    1: "Cánh cụt",
    2: "Ottero",
}
PET_NAMES_EN = {
    0: "No Pet",
    1: "Mr. Waggor (Penguin)",
    2: "Ottero",
}


def build_plain(slots, shop=False, pet=0):
    """slots: list toi da 4 ten (hoac 'none'); thieu thi bu none."""
    slots = list(slots) + [NONE] * (4 - len(slots))
    if len(slots) != 4:
        raise ValueError("toi da 4 slot")
    p = [0]                                  # header
    for s in slots:
        p.append(SEP)
        p += NONE_SEG if s in (NONE, None) else BLOCKS[s]
    p.append(SEP)
    p += SHOP_SEG if shop else NONE_SEG      # slot shop
    p.append(SEP)
    p += FIXED_SEG
    p.append(SEP)
    p += PET_SEG[pet]                        # slot pet
    return p


def xor_key(data):
    n = len(data)
    return [data[i] ^ KEY[(n - 1 - i) % 22] for i in range(n)]


def encode(slots, shop=False, pet=0):
    raw = bytes(xor_key(build_plain(slots, shop, pet)))
    return "#FF#" + base64.b64encode(raw).decode()


def _b64(code):
    s = code.strip()
    if s.startswith("#FF#"):
        s = s[4:]
    return base64.b64decode(s + "=" * (-len(s) % 4))


def plain_of(code):
    return xor_key(list(_b64(code)))


def decode(code):
    """Tra ve dict: slots, shop, pet, hoac nem loi neu cau truc la."""
    p = plain_of(code)
    if p[0] != 0 or p[1] != SEP:
        raise ValueError("header sai - co the ma sai hoac khoa chua du")
    segs, cur = [], []
    for v in p[2:]:
        if v == SEP:
            segs.append(cur)
            cur = []
        else:
            cur.append(v)
    segs.append(cur)
    if len(segs) != 7:
        raise ValueError("so doan = %d (can 7): %s" % (len(segs), segs))
    rev = {}
    canonical = ("Tatsuya", "Alok (Pre)", "BHXK", "Phun lửa", "Chidori", "Alok", "Kelly", "Xayne", "Rin", "Ignis", "Dasha")
    for k, v in BLOCKS.items():
        tv = tuple(v)
        if tv not in rev or k in canonical:
            rev[tv] = k
    slots = []
    for s in segs[:4]:
        if s == NONE_SEG:
            slots.append(NONE)
        else:
            slots.append(rev.get(tuple(s), "UNKNOWN%s" % s))
    shop_seg, fixed, pet_seg = segs[4], segs[5], segs[6]
    if fixed != FIXED_SEG:
        raise ValueError("doan co dinh khac: %s" % fixed)
    shop = (shop_seg == SHOP_SEG) if shop_seg != NONE_SEG else False
    if shop_seg not in (NONE_SEG, SHOP_SEG):
        shop = "UNKNOWN%s" % shop_seg
    pet = {tuple(v): k for k, v in PET_SEG.items()}.get(tuple(pet_seg),
                                                        "UNKNOWN%s" % pet_seg)
    return {"slots": slots, "shop": shop, "pet": pet, "length": len(p)}


def encode_raw_blocks(slots_b, shop_b, pet_b):
    p = [0]
    for sb in slots_b:
        p.append(SEP)
        p += sb
    p.append(SEP)
    p += shop_b
    p.append(SEP)
    p += FIXED_SEG
    p.append(SEP)
    p += pet_b
    return "#FF#" + base64.b64encode(bytes(xor_key(p))).decode()


def analyze_combo(code, lang="vi"):
    p = plain_of(code)
    if p[0] != 0 or p[1] != SEP:
        raise ValueError("Header sai - mã không hợp lệ")
    segs, cur = [], []
    for v in p[2:]:
        if v == SEP:
            segs.append(cur)
            cur = []
        else:
            cur.append(v)
    segs.append(cur)
    if len(segs) != 7:
        raise ValueError("Số đoạn byte không đúng (cần 7 đoạn, nhận %d)" % len(segs))

    rev = {}
    canonical = ("Tatsuya", "Alok (Pre)", "BHXK", "Phun lửa", "Chidori", "Alok", "Kelly", "Xayne", "Rin", "Ignis", "Dasha")
    for k, v in BLOCKS.items():
        tv = tuple(v)
        if tv not in rev or k in canonical:
            rev[tv] = k

    slots_info = []
    for i, seg in enumerate(segs[:4]):
        if seg == NONE_SEG:
            name = "none"
            is_unknown = False
        else:
            tv = tuple(seg)
            if tv in rev:
                name = rev[tv]
                is_unknown = False
            else:
                name = "Unknown"
                is_unknown = True

        slot_codes = [
            encode_raw_blocks([seg, NONE_SEG, NONE_SEG, NONE_SEG], NONE_SEG, [1]),
            encode_raw_blocks([NONE_SEG, seg, NONE_SEG, NONE_SEG], NONE_SEG, [1]),
            encode_raw_blocks([NONE_SEG, NONE_SEG, seg, NONE_SEG], NONE_SEG, [1]),
            encode_raw_blocks([NONE_SEG, NONE_SEG, NONE_SEG, seg], NONE_SEG, [1]),
        ]
        slots_info.append({
            "slot": i + 1,
            "name": name,
            "block": seg,
            "is_unknown": is_unknown,
            "codes": slot_codes,
        })

    shop_seg = segs[4]
    if shop_seg == NONE_SEG:
        shop_status = "No Shop Tool" if lang == "en" else "Không Shop"
        shop_unknown = False
    elif shop_seg == SHOP_SEG:
        shop_status = "With Shop Tool" if lang == "en" else "Có Shop Tool"
        shop_unknown = False
    else:
        shop_status = "Unknown"
        shop_unknown = True
    shop_isolated = encode_raw_blocks([NONE_SEG, NONE_SEG, NONE_SEG, NONE_SEG], shop_seg, [1])

    pet_seg = segs[6]
    pet_rev = {tuple(v): k for k, v in PET_SEG.items()}
    tv_pet = tuple(pet_seg)
    if tv_pet in pet_rev:
        pet_id = pet_rev[tv_pet]
        pet_name = PET_NAMES_EN.get(pet_id, "No Pet") if lang == "en" else PET_NAMES.get(pet_id, "Không Pet")
        pet_unknown = False
    else:
        pet_name = "Unknown"
        pet_unknown = True
    pet_isolated = encode_raw_blocks([NONE_SEG, NONE_SEG, NONE_SEG, NONE_SEG], NONE_SEG, pet_seg)

    return {
        "code": code,
        "length": len(p),
        "slots": slots_info,
        "shop": {
            "status": shop_status,
            "block": shop_seg,
            "is_unknown": shop_unknown,
            "isolated_code": shop_isolated,
        },
        "pet": {
            "name": pet_name,
            "block": pet_seg,
            "is_unknown": pet_unknown,
            "isolated_code": pet_isolated,
        },
    }


# ---------------------------------------------------------------- ma that de kiem chung
# (slots, shop, pet, ma). 'U' = ma NGUOI DUNG DUA; 'C' = nguoi dung xac nhan dung.
VECTORS = [
    ("U", [], 0, 0, "dGJ/ZW9jYmd4dW9yZH9xZ38="),
    ("U", ["Alok"], 0, 0, "aGN5bXV1eWVvY2JneHVvcmR/cWd/"),
    ("U", [NONE, "Alok"], 0, 0, "aGN7eXd2f3JpY2JneHVvcmR/cWd/"),
    ("U", [NONE, NONE, "Alok"], 0, 0, "aGN7eXVifXFvdGRneHVvcmR/cWd/"),
    ("U", [NONE, NONE, NONE, "Alok"], 0, 0, "aGN7eXVif2Vtd2JwfnVvcmR/cWd/"),
    ("U", ["Xayne"], 0, 0, "dG1rcXR5ZW9jYmd4dW9yZH9xZ38="),
    ("U", [NONE, "Xayne"], 0, 0, "dG1vY3B7c2ljYmd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, "Xayne"], 0, 0, "dG1vY3Rpd2t1ZGd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, NONE, "Xayne"], 0, 0, "dG1vY3Rpc3lxZnF+dW9yZH9xZ38="),
    ("U", ["Kelly"], 0, 0, "dG1tcHR+ZW9jYmd4dW9yZH9xZ38="),
    ("U", [NONE, "Kelly"], 0, 0, "dG1vY3Z6c25jYmd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, "Kelly"], 0, 0, "dG1vY3RpcWp1Y2d4dW9yZH9xZ38="),
    ("U", [NONE, NONE, NONE, "Kelly"], 0, 0, "dG1vY3Rpc3l3Z3F5dW9yZH9xZ38="),
    ("U", ["Rin"], 0, 0, "dG1ocHR5ZW9jYmd4dW9yZH9xZ38="),
    ("U", [NONE, "Rin"], 0, 0, "dG1vY3N6c2ljYmd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, "Rin"], 0, 0, "dG1vY3RpdGp1ZGd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, NONE, "Rin"], 0, 0, "dG1vY3Rpc3lyZ3F+dW9yZH9xZ38="),
    ("U", ["Alok", "Kelly"], 0, 0, "cGl+a3V6aWN2enNuY2JneHVvcmR/cWd/"),
    ("U", ["Kelly", "Alok"], 0, 0, "cGl+bHV6eXd2f3JpY2JneHVvcmR/cWd/"),
    ("U", ["Xayne", "Kelly"], 0, 0, "fmptcXtpY3Z6c25jYmd4dW9yZH9xZ38="),
    ("U", ["Kelly", "Xayne"], 0, 0, "fmprcHtuY3B7c2ljYmd4dW9yZH9xZ38="),
    ("U", ["Kelly", "Rin"], 0, 0, "fmprcHtuY3N6c2ljYmd4dW9yZH9xZ38="),
    ("U", ["Rin", "Kelly"], 0, 0, "fmpucHtpY3Z6c25jYmd4dW9yZH9xZ38="),
    ("U", ["Alok", "Rin"], 0, 0, "cGl+a3V6aWNzenNpY2JneHVvcmR/cWd/"),
    ("U", ["Rin", "Alok"], 0, 0, "cGl7bHV9eXd2f3JpY2JneHVvcmR/cWd/"),
    ("U", ["Xayne", "Rin"], 0, 0, "fmptcXtpY3N6c2ljYmd4dW9yZH9xZ38="),
    ("U", ["Rin", "Xayne"], 0, 0, "fmpucHtpY3B7c2ljYmd4dW9yZH9xZ38="),
    ("U", ["Tatsuya"], 0, 1, "YnlsYH90dGl8f3Vtb2N0aXJvcHRwe2JqZGV/dHF2"),
    ("U", ["Alok (Pre)"], 0, 1, "YnliYX90dWl8f3Vtb2N0aXJvcHRwe2JqZGV/dHF2"),
    ("U", [NONE, "BHXK"], 0, 1, "Ynlkc35zcX91f3Vtb2N0aXJvcHRwe2JqZGV/dHF2"),
    ("U", ["Phun lửa"], 0, 1, "YnllYn91cWl8f3Vtb2N0aXJvcHRwe2JqZGV/dHF2"),
    ("U", ["Chidori"], 0, 2, "YnllYn90eWl8f3Vtb2N0aXJvcHRwe2JqZGV/dHB/"),
    ("U", ["Ignis"], 0, 0, "dG1pcnR5ZW9jYmd4dW9yZH9xZ38="),
    ("U", [NONE, "Ignis"], 0, 0, "dG1vY3J4c2ljYmd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, "Ignis"], 0, 0, "dG1vY3RpdWh1ZGd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, NONE, "Ignis"], 0, 0, "dG1vY3Rpc3lzZXF+dW9yZH9xZ38="),
    ("U", ["Dasha"], 0, 0, "dG1scHR5ZW9jYmd4dW9yZH9xZ38="),
    ("U", [NONE, "Dasha"], 0, 0, "dG1vY3d6c2ljYmd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, "Dasha"], 0, 0, "dG1vY3RpcGp1ZGd4dW9yZH9xZ38="),
    ("U", [NONE, NONE, NONE, "Dasha"], 0, 0, "dG1vY3Rpc3l2Z3F+dW9yZH9xZ38="),
    ("U", [], 0, 2, "fmJxaXx/dW1vY3Rpcm9wdHB7YmpkZX90cH8="),
    ("U", [], 1, 0, "dWd/amlje3l1Ynpzb3VicXhja3Jkf3Fnfw=="),
    ("U", ["BHXK", "Phun lửa", "Kelly", "Dasha"], 1, 0,
     "dGJ+dG91a2d5ZG9lZWl2dH99f3Z+b3NienNvdWJxeGNrcmR/cWd/"),
    ("U", ["BHXK", "Phun lửa", "Kelly", "Dasha"], 0, 0,
     "eXVuY2V/fWd+e2l0e3l3cX9yeXZncX51b3Jkf3Fnfw=="),
    # ma do AI suy ra roi nguoi dung xac nhan "chinh xac":
    ("C", ["Alok", "Xayne"], 0, 0, "cGl+a3V6aWNwe3NpY2JneHVvcmR/cWd/"),
    ("C", ["Tatsuya"], 0, 0, "aGNzanV0emVvY2JneHVvcmR/cWd/"),
]


def selftest():
    bad = 0
    for kind, slots, shop, pet, code in VECTORS:
        full = "#FF#" + code
        ok_enc = encode(slots, bool(shop), pet) == full
        d = decode(full)
        want = list(slots) + [NONE] * (4 - len(slots))
        ok_dec = d["slots"] == want and bool(d["shop"]) == bool(shop) and d["pet"] == pet
        if not (ok_enc and ok_dec):
            bad += 1
            print("FAIL", kind, slots, shop, pet)
    print("PASS %d/%d" % (len(VECTORS) - bad, len(VECTORS)))
    return bad == 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--selftest", action="store_true")
    ap.add_argument("--encode", nargs="+", metavar="SLOT")
    ap.add_argument("--shop", action="store_true")
    ap.add_argument("--pet", type=int, default=0, choices=[0, 1, 2])
    ap.add_argument("--decode", metavar="CODE")
    a = ap.parse_args()
    if a.selftest:
        sys.exit(0 if selftest() else 1)
    if a.encode:
        slots = [NONE if s.lower() == NONE else s.capitalize() for s in a.encode]
        print(encode(slots, a.shop, a.pet))
    elif a.decode:
        print(decode(a.decode))
    else:
        ap.print_help()


if __name__ == "__main__":
    main()
