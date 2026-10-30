#!/usr/bin/env python3
"""
combo_calculator_gui.py - Ứng dụng Win32 Native cổ điển tính toán và ghép Combo Free Fire.
Dựa trên nền tảng thuật toán combo_codec.py.
Hỗ trợ chuyển đổi song ngữ mượt mà: Tiếng Việt và English.
"""
import os
import re
import tkinter as tk
from tkinter import ttk, messagebox, simpledialog
from tkinter.scrolledtext import ScrolledText

import combo_codec as codec


class I18n:
    current_lang = "vi"

    @classmethod
    def t(cls, vi, en):
        return en if cls.current_lang == "en" else vi


class ComboCalculatorApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Free Fire Combo Calculator")
        self.geometry("820x725")
        self.minsize(800, 680)
        self.configure(bg="#f0f0f0")

        # Cài đặt style Win32 cổ điển
        self.style = ttk.Style(self)
        try:
            self.style.theme_use("winnative")
        except Exception:
            pass

        codec.load_characters()
        for del_name in ["Elena", "Alan", "Kepo", "Pre", "Tat", "Metra"]:
            codec.BLOCKS.pop(del_name, None)

        self._init_ui()
        self._refresh_choiceboxes()
        self.change_language("vi")

    def _init_ui(self):
        # -------------------------------------------------------------
        # 0. THANH CHỌN NGÔN NGỮ (LANGUAGE BAR)
        # -------------------------------------------------------------
        pnl_header = tk.Frame(self, bg="#f0f0f0")
        pnl_header.pack(fill="x", padx=12, pady=(6, 2))

        self.cb_lang = ttk.Combobox(
            pnl_header,
            state="readonly",
            width=13,
            font=("Segoe UI", 9),
            values=["Tiếng Việt", "English"]
        )
        self.cb_lang.current(0)
        self.cb_lang.pack(side="right")
        self.cb_lang.bind(
            "<<ComboboxSelected>>",
            lambda e: self.change_language("en" if self.cb_lang.current() == 1 else "vi")
        )

        self.lbl_lang = tk.Label(
            pnl_header,
            text="Ngôn ngữ / Language:",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0"
        )
        self.lbl_lang.pack(side="right", padx=(0, 6))

        # -------------------------------------------------------------
        # 1. NHẬP DỮ LIỆU NHÂN VẬT
        # -------------------------------------------------------------
        self.grp1 = tk.LabelFrame(
            self,
            text=" 1. Nhập dữ liệu nhân vật ",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=8
        )
        self.grp1.pack(fill="x", padx=12, pady=4)

        row1 = tk.Frame(self.grp1, bg="#f0f0f0")
        row1.pack(fill="x", pady=2)
        self.lbl_slot1 = tk.Label(
            row1,
            text="Slot 1 Code Combo:",
            width=18,
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 9, "bold")
        )
        self.lbl_slot1.pack(side="left")
        self.ent_slot1 = tk.Entry(row1, font=("Consolas", 9))
        self.ent_slot1.pack(side="left", fill="x", expand=True)

        btn_row1 = tk.Frame(self.grp1, bg="#f0f0f0")
        btn_row1.pack(fill="x", pady=(5, 0))
        self.btn_import = tk.Button(
            btn_row1,
            text="Đăng ký nhân vật",
            font=("Segoe UI", 9, "bold"),
            width=16,
            command=self.on_import_character
        )
        self.btn_import.pack(side="left", padx=(18, 6))

        self.btn_clear = tk.Button(
            btn_row1,
            text="Xóa trắng",
            font=("Segoe UI", 9),
            width=10,
            command=self.on_clear_inputs
        )
        self.btn_clear.pack(side="left")

        self.btn_guide = tk.Button(
            btn_row1,
            text="Hướng dẫn",
            font=("Segoe UI", 9),
            command=self.on_show_guide
        )
        self.btn_guide.pack(side="left", padx=6)

        self.lbl_guide_hint = tk.Label(
            btn_row1,
            text="(* Bấm 'Hướng dẫn' để xem các bước lấy mã từ game)",
            font=("Segoe UI", 8, "italic"),
            fg="#555555",
            bg="#f0f0f0"
        )
        self.lbl_guide_hint.pack(side="left", padx=4)

        # -------------------------------------------------------------
        # 2. GHÉP THÀNH COMBO
        # Kiểu: {Choicebox1} + {Choicebox2} + {Choicebox3} + {Choicebox4} + {Choicebox5} + {Choicebox6}
        # -------------------------------------------------------------
        self.grp2 = tk.LabelFrame(
            self,
            text=" 2. Ghép thành combo ",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=8
        )
        self.grp2.pack(fill="x", padx=12, pady=4)

        # Tiêu đề cột nhỏ
        header_row = tk.Frame(self.grp2, bg="#f0f0f0")
        header_row.pack(fill="x")
        self.headers_def = [
            ("Slot 1 (Chủ động)", "Slot 1 (Active)", 14),
            ("Slot 2 (Bị động)", "Slot 2 (Passive)", 14),
            ("Slot 3 (Bị động)", "Slot 3 (Passive)", 14),
            ("Slot 4 (Bị động)", "Slot 4 (Passive)", 14),
            ("Shop Tool", "Shop Tool", 13),
            ("Trợ Thủ (Pet)", "Pet Companion", 14)
        ]
        self.header_labels = []
        for vi_h, en_h, w in self.headers_def:
            lbl = tk.Label(header_row, text=vi_h, width=w, anchor="w", bg="#f0f0f0", font=("Segoe UI", 8))
            lbl.pack(side="left", padx=3)
            self.header_labels.append(lbl)

        # Hàng 6 Choicebox kèm dấu +
        choices_row = tk.Frame(self.grp2, bg="#f0f0f0")
        choices_row.pack(fill="x", pady=3)

        self.cb_choices = []
        for i in range(4):
            cb = ttk.Combobox(choices_row, state="readonly", width=13, font=("Segoe UI", 9))
            cb.pack(side="left", padx=2)
            self.cb_choices.append(cb)
            lbl_plus = tk.Label(choices_row, text="+", font=("Segoe UI", 10, "bold"), bg="#f0f0f0")
            lbl_plus.pack(side="left", padx=1)

        # Choicebox 5 (Shop)
        self.cb_shop = ttk.Combobox(choices_row, state="readonly", width=12, font=("Segoe UI", 9))
        self.cb_shop["values"] = ["Không Shop", "Có Shop Tool"]
        self.cb_shop.current(0)
        self.cb_shop.pack(side="left", padx=2)

        lbl_plus5 = tk.Label(choices_row, text="+", font=("Segoe UI", 10, "bold"), bg="#f0f0f0")
        lbl_plus5.pack(side="left", padx=1)

        # Choicebox 6 (Pet)
        self.cb_pet = ttk.Combobox(choices_row, state="readonly", width=14, font=("Segoe UI", 9))
        self.cb_pet["values"] = ["Không Pet (0)", "Cánh cụt", "Ottero"]
        self.cb_pet.current(0)
        self.cb_pet.pack(side="left", padx=2)

        # Nút Ghép Combo ở dưới
        btn_row2 = tk.Frame(self.grp2, bg="#f0f0f0")
        btn_row2.pack(fill="x", pady=(6, 0))

        self.btn_build = tk.Button(
            btn_row2,
            text="Ghép Combo",
            font=("Segoe UI", 9, "bold"),
            width=14,
            command=self.on_build_combo
        )
        self.btn_build.pack(side="left", padx=(2, 6))

        self.btn_reset = tk.Button(
            btn_row2,
            text="Đặt lại",
            font=("Segoe UI", 9),
            width=10,
            command=self.on_reset_combo
        )
        self.btn_reset.pack(side="left")

        # -------------------------------------------------------------
        # 3. THANH PROGRESS VÀ KẾT QUẢ TÍNH COMBO
        # -------------------------------------------------------------
        self.grp3 = tk.LabelFrame(
            self,
            text=" 3. Thanh progress và kết quả tính combo ",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=8
        )
        self.grp3.pack(fill="both", expand=True, padx=12, pady=4)

        self.lbl_progress_text = tk.Label(
            self.grp3,
            text="Tiến độ sẵn sàng",
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 8)
        )
        self.lbl_progress_text.pack(fill="x")

        self.progress_bar = ttk.Progressbar(self.grp3, orient="horizontal", mode="determinate")
        self.progress_bar.pack(fill="x", pady=2)

        self.lbl_res = tk.Label(
            self.grp3,
            text="Mã Combo Free Fire (#FF#...):",
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 9)
        )
        self.lbl_res.pack(fill="x", pady=(6, 1))

        res_row = tk.Frame(self.grp3, bg="#f0f0f0")
        res_row.pack(fill="x", pady=2)

        self.txt_result = tk.Entry(
            res_row,
            font=("Consolas", 10, "bold"),
            bg="white",
            relief="solid",
            bd=1
        )
        self.txt_result.pack(side="left", fill="x", expand=True, padx=(0, 6))

        self.btn_copy = tk.Button(
            res_row,
            text="Sao chép mã",
            font=("Segoe UI", 9, "bold"),
            width=12,
            command=self.on_copy_result
        )
        self.btn_copy.pack(side="left", padx=2)

        self.btn_verify = tk.Button(
            res_row,
            text="Giải Combo",
            font=("Segoe UI", 9, "bold"),
            width=11,
            command=self.on_verify_result
        )
        self.btn_verify.pack(side="left", padx=2)

        self.lbl_log = tk.Label(
            self.grp3,
            text="Chi tiết phân tích & Nhật ký:",
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 9)
        )
        self.lbl_log.pack(fill="x", pady=(6, 1))

        self.log_area = ScrolledText(
            self.grp3,
            font=("Consolas", 9),
            bg="white",
            relief="solid",
            bd=1,
            height=10
        )
        self.log_area.pack(fill="both", expand=True, pady=2)

    def change_language(self, lang):
        I18n.current_lang = lang
        self.cb_lang.current(1 if lang == "en" else 0)

        # Phần 1
        self.grp1.config(text=I18n.t(" 1. Nhập dữ liệu nhân vật ", " 1. Character Data Input "))
        self.lbl_slot1.config(text=I18n.t("Slot 1 Code Combo:", "Slot 1 Code Combo:"))
        self.btn_import.config(text=I18n.t("Đăng ký nhân vật", "Register Character"))
        self.btn_clear.config(text=I18n.t("Xóa trắng", "Clear"))
        self.btn_guide.config(text=I18n.t("Hướng dẫn", "Guide"))
        self.lbl_guide_hint.config(
            text=I18n.t(
                "(* Bấm 'Hướng dẫn' để xem các bước lấy mã từ game)",
                "(* Click 'Guide' to see instructions on getting code from game)"
            )
        )

        # Phần 2
        self.grp2.config(text=I18n.t(" 2. Ghép thành combo ", " 2. Assemble Combo "))
        for i, (vi_h, en_h, w) in enumerate(self.headers_def):
            self.header_labels[i].config(text=en_h if lang == "en" else vi_h)

        cur_shop = self.cb_shop.current()
        self.cb_shop["values"] = ["No Shop Tool", "With Shop Tool"] if lang == "en" else ["Không Shop", "Có Shop Tool"]
        self.cb_shop.current(cur_shop if 0 <= cur_shop < len(self.cb_shop["values"]) else 0)

        cur_pet = self.cb_pet.current()
        self.cb_pet["values"] = ["No Pet (0)", "Mr. Waggor (Penguin)", "Ottero"] if lang == "en" else ["Không Pet (0)", "Cánh cụt", "Ottero"]
        self.cb_pet.current(cur_pet if 0 <= cur_pet < len(self.cb_pet["values"]) else 0)

        self.btn_build.config(text=I18n.t("Ghép Combo", "Assemble Combo"))
        self.btn_reset.config(text=I18n.t("Đặt lại", "Reset"))

        # Phần 3
        self.grp3.config(text=I18n.t(" 3. Thanh progress và kết quả tính combo ", " 3. Progress & Calculation Result "))
        if self.progress_bar["value"] in (0, 100):
            self.lbl_progress_text.config(text=I18n.t("Tiến độ sẵn sàng", "Ready"))
        self.lbl_res.config(text=I18n.t("Mã Combo Free Fire (#FF#...):", "Free Fire Combo Code (#FF#...):"))
        self.btn_copy.config(text=I18n.t("Sao chép mã", "Copy Code"))
        self.btn_verify.config(text=I18n.t("Giải Combo", "Solve Combo"))
        self.lbl_log.config(text=I18n.t("Chi tiết phân tích & Nhật ký:", "Analysis Details & Activity Log:"))

    def _refresh_choiceboxes(self, select_name=None):
        chars = sorted(list(codec.BLOCKS.keys()), key=lambda s: s.lower())
        if "none" in chars:
            chars.remove("none")
        options = ["none"] + chars

        for i, cb in enumerate(self.cb_choices):
            cur = cb.get() or "none"
            cb["values"] = options
            if select_name and i == 0 and select_name in options:
                cb.set(select_name)
            elif cur in options:
                cb.set(cur)
            else:
                cb.current(0)

    def _animate_progress(self, current, target, on_done):
        if current < target:
            current = min(target, current + 5)
            self.progress_bar["value"] = current
            self.after(15, lambda: self._animate_progress(current, target, on_done))
        else:
            self.progress_bar["value"] = target
            if on_done:
                on_done()

    def start_progress(self, status_msg, on_done):
        self.lbl_progress_text.config(text=status_msg)
        self.progress_bar["value"] = 0
        self._animate_progress(0, 100, on_done)

    def append_log(self, text):
        self.log_area.insert("end", text + "\n")
        self.log_area.see("end")

    # -----------------------------------------------------------------
    # Logic Phần 1: Nhập dữ liệu nhân vật
    # -----------------------------------------------------------------
    def on_clear_inputs(self):
        self.ent_slot1.delete(0, "end")

    def on_show_guide(self):
        guide_win = tk.Toplevel(self)
        guide_win.title(I18n.t("Hướng dẫn: Lấy mã nhân vật để đăng ký", "Guide: Extract Character Code for Registration"))
        guide_win.geometry("680x560")
        guide_win.minsize(640, 520)
        guide_win.configure(bg="#f0f0f0")
        guide_win.transient(self)
        guide_win.grab_set()

        guide_text_vi = (
            "Hướng dẫn: Lấy mã nhân vật để đăng ký\n\n"
            "Mã nhân vật là gì? Đó là một dòng chữ bắt đầu bằng #FF#. Công cụ dùng dòng chữ này để nhận ra bạn đang chọn nhân vật nào. Bạn không cần hiểu nó nghĩa là gì, chỉ cần sao chép đúng.\n\n"
            "Lưu ý: Mỗi nhân vật cần một mã riêng. Muốn đăng ký 5 nhân vật thì làm 5 lần.\n\n"
            "Các bước\n\n"
            "Bước 1. Đưa game về trạng thái trống.\n"
            "Sao chép dòng mã combo dưới đây và dán nó vào mục combo game:\n"
            "#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=\n"
            "Combo này sẽ xóa sạch combo hiện tại: 4 ô trống, không pet, không công cụ hỗ trợ (CCHT).\n\n"
            "Bước 2. Chọn nhân vật cần đăng ký.\n"
            "Chọn đúng 1 nhân vật vào ô đầu tiên. Ba ô còn lại để trống. Không bật Pet hay CCHT.\n\n"
            "Bước 3. Lấy mã.\n"
            "Bấm [Sao chép mã] để copy mã cho việc đăng ký.\n\n"
            "Bước 4. Đăng ký.\n"
            "Quay lại công cụ này, dán mã vào ô nhập của Phần 1 và bấm Đăng ký."
        )

        guide_text_en = (
            "Guide: Extract Character Code for Registration\n\n"
            "What is a character code? It is a text string starting with #FF#. The tool uses this code to identify which character you selected. You do not need to understand what it means, simply copy it accurately.\n\n"
            "Note: Each character requires a unique code. If you want to register 5 characters, repeat this 5 times.\n\n"
            "Steps\n\n"
            "Step 1. Reset game to empty state.\n"
            "Copy the combo code below and paste it into the in-game combo section:\n"
            "#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=\n"
            "This combo clears your active combo: 4 empty slots, no pet, no loadout/shop tool.\n\n"
            "Step 2. Select the character to register.\n"
            "Equip exactly 1 character in the first slot. Leave the other 3 slots empty. Do not equip Pet or loadout tools.\n\n"
            "Step 3. Copy code.\n"
            "Click [Copy Code] in-game to copy the code for registration.\n\n"
            "Step 4. Register.\n"
            "Return to this tool, paste the code into the input field in Part 1 and click Register Character."
        )

        txt_guide = ScrolledText(
            guide_win,
            font=("Segoe UI", 10),
            bg="white",
            relief="solid",
            bd=1,
            padx=10,
            pady=10
        )
        txt_guide.insert("1.0", I18n.t(guide_text_vi, guide_text_en))
        txt_guide.config(state="disabled")
        txt_guide.pack(fill="both", expand=True, padx=12, pady=10)

        btn_row = tk.Frame(guide_win, bg="#f0f0f0")
        btn_row.pack(fill="x", padx=12, pady=(0, 10))

        def copy_reset():
            self.clipboard_clear()
            self.clipboard_append("#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=")
            messagebox.showinfo(
                I18n.t("Đã sao chép", "Copied"),
                I18n.t(
                    "Đã sao chép mã combo xóa trống (#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=) vào Clipboard!\nDán mã này vào game để xóa sạch combo trước khi chọn nhân vật.",
                    "Reset combo code (#FF#dGJ/ZW9jYmd4dW9yZH9xZ38=) copied to Clipboard!\nPaste it in game to clear active combo before selecting character."
                )
            )

        btn_copy_reset = tk.Button(
            btn_row,
            text=I18n.t("Sao chép mã xóa trống (Bước 1)", "Copy Reset Code (Step 1)"),
            font=("Segoe UI", 9, "bold"),
            command=copy_reset
        )
        btn_copy_reset.pack(side="left")

        def show_screenshot():
            img_path = os.path.join(os.path.dirname(__file__), "huong_dan_slot.png")
            if not os.path.exists(img_path):
                messagebox.showerror(I18n.t("Lỗi", "Error"), f"Image not found: {img_path}")
                return
            img_win = tk.Toplevel(guide_win)
            img_win.title(I18n.t("Ảnh minh họa game", "In-Game Screenshot"))
            img_win.geometry("700x720")
            img_win.configure(bg="black")
            try:
                self._guide_photo = tk.PhotoImage(file=img_path)
                lbl_img = tk.Label(img_win, image=self._guide_photo, bg="black")
                lbl_img.pack(fill="both", expand=True)
            except Exception as e:
                messagebox.showerror("Error", str(e))

        btn_view_img = tk.Button(
            btn_row,
            text=I18n.t("Xem ảnh minh họa", "View Screenshot"),
            font=("Segoe UI", 9),
            command=show_screenshot
        )
        btn_view_img.pack(side="left", padx=8)

        btn_close = tk.Button(
            btn_row,
            text=I18n.t("Đóng", "Close"),
            font=("Segoe UI", 9, "bold"),
            width=12,
            command=guide_win.destroy
        )
        btn_close.pack(side="right")

    def on_import_character(self):
        text = self.ent_slot1.get().strip()
        if not text:
            messagebox.showinfo(
                I18n.t("Chưa có dữ liệu", "No Input Data"),
                I18n.t("Vui lòng dán mã combo (#FF#...) của nhân vật vào ô Slot 1 Code Combo!",
                       "Please paste character combo code (#FF#...) into Slot 1 Code Combo!")
            )
            return

        detected_name = ""
        m_name = re.match(r"^([A-Za-z0-9_\s\(\)\u00C0-\u1EF9]+?)[\s\:\;\(]+#FF#", text)
        if m_name:
            cand = re.sub(r"\(\d+\)", "", m_name.group(1)).strip()
            if cand:
                detected_name = cand

        m_code = re.search(r"#FF#([A-Za-z0-9+/=]+)", text)
        code = ("#FF#" + m_code.group(1)) if m_code else text

        try:
            p = codec.plain_of(code)
            if len(p) < 2 or p[0] != 0 or p[1] != codec.SEP:
                messagebox.showerror(
                    I18n.t("Lỗi", "Error"),
                    I18n.t("Mã combo có header không đúng định dạng Free Fire!", "Invalid combo code header format!")
                )
                return

            segs = []
            cur = []
            for v in p[2:]:
                if v == codec.SEP:
                    segs.append(cur)
                    cur = []
                else:
                    cur.append(v)
            segs.append(cur)

            if len(segs) < 4:
                messagebox.showerror(
                    I18n.t("Lỗi", "Error"),
                    I18n.t("Mã combo không đủ các đoạn dữ liệu nhân vật!", "Combo code lacks character data segments!")
                )
                return

            # Lấy slot 1 trước, nếu slot 1 trống thì tìm slot không trống đầu tiên
            if segs[0] != codec.NONE_SEG:
                target_seg = segs[0]
            else:
                target_seg = next((s for s in segs[1:4] if s != codec.NONE_SEG), None)

            if target_seg is None:
                messagebox.showwarning(
                    I18n.t("Thông báo", "Notice"),
                    I18n.t("Mã combo nhập vào là combo trống (cả 4 slot đều trống, không có nhân vật nào)!",
                           "The entered combo code is empty (all 4 slots are empty, no character found)!")
                )
                return
        except Exception as e:
            messagebox.showerror(
                I18n.t("Lỗi giải mã", "Decoding Error"),
                I18n.t(f"Không thể giải mã combo: {e}", f"Cannot decode combo: {e}")
            )
            return

        def process_done():
            block_tuple = tuple(target_seg)
            canonical = ("Tatsuya", "Alok (Pre)", "BHXK", "Phun lửa", "Chidori", "Alok", "Kelly", "Xayne", "Rin", "Ignis", "Dasha")
            existing = None
            for c in canonical:
                if c in codec.BLOCKS and tuple(codec.BLOCKS[c]) == block_tuple:
                    existing = c
                    break
            if not existing:
                for k, v in codec.BLOCKS.items():
                    if tuple(v) == block_tuple:
                        existing = k
                        break

            if existing:
                msg = I18n.t(
                    f"==> ĐĂNG KÝ NHÂN VẬT: Nhân vật đã tồn tại!\n- Tên: {existing}\n- Khối: {target_seg}\n- Đã tự động chọn vào Choicebox 1.",
                    f"==> REGISTER CHARACTER: Character already exists!\n- Name: {existing}\n- Block: {target_seg}\n- Automatically selected in Choicebox 1."
                )
                self.append_log(msg)
                self.txt_result.delete(0, "end")
                self.txt_result.insert(0, existing)
                self._refresh_choiceboxes(existing)
                self.lbl_progress_text.config(
                    text=I18n.t(f"Đã nhận diện: {existing}", f"Identified: {existing}")
                )
                messagebox.showinfo(
                    I18n.t("Đã nhận diện", "Identified"),
                    I18n.t(f"Nhân vật này đã có sẵn trong cơ sở dữ liệu:\n- Tên: {existing}\n- Khối byte: {target_seg}",
                           f"This character already exists in database:\n- Name: {existing}\n- Byte block: {target_seg}")
                )
            else:
                prompt_text = I18n.t(
                    f"Phát hiện nhân vật mới!\nKhối byte: {target_seg}\nNhập tên nhân vật:",
                    f"New character detected!\nByte block: {target_seg}\nEnter character name:"
                )
                new_name = simpledialog.askstring(
                    I18n.t("Đăng ký nhân vật mới", "Register New Character"),
                    prompt_text,
                    initialvalue=detected_name,
                    parent=self
                )
                if not new_name or not new_name.strip():
                    self.append_log(
                        I18n.t(f"Đã hủy đăng ký nhân vật mới (khối: {target_seg}).",
                               f"Cancelled registering new character (block: {target_seg}).")
                    )
                    self.lbl_progress_text.config(
                        text=I18n.t("Đã hủy đăng ký.", "Registration cancelled.")
                    )
                    return

                new_name = new_name.strip()
                codec.save_character(new_name, target_seg)
                self._refresh_choiceboxes(new_name)
                self.txt_result.delete(0, "end")
                self.txt_result.insert(0, new_name)
                msg = I18n.t(
                    f"==> THÀNH CÔNG: Đã đăng ký nhân vật mới!\n- Tên: {new_name}\n- Khối byte: {target_seg}\n- Cập nhật Choicebox.",
                    f"==> SUCCESS: Registered new character!\n- Name: {new_name}\n- Byte block: {target_seg}\n- Choiceboxes updated."
                )
                self.append_log(msg)
                self.lbl_progress_text.config(
                    text=I18n.t(f"Đã đăng ký thành công: {new_name}", f"Successfully registered: {new_name}")
                )
                messagebox.showinfo(
                    I18n.t("Thành công", "Success"),
                    I18n.t(f"Đã đăng ký thành công nhân vật '{new_name}' vào hệ thống!",
                           f"Successfully registered character '{new_name}' into the system!")
                )

        self.start_progress(
            I18n.t("Đang phân tích và đăng ký nhân vật...", "Analyzing and registering character..."),
            process_done
        )

    # -----------------------------------------------------------------
    # Logic Phần 2: Ghép thành combo
    # -----------------------------------------------------------------
    def on_reset_combo(self):
        for cb in self.cb_choices:
            cb.current(0)
        self.cb_shop.current(0)
        self.cb_pet.current(0)

    def on_build_combo(self):
        slots = [cb.get() or "none" for cb in self.cb_choices]
        shop = (self.cb_shop.current() == 1)
        pet = self.cb_pet.current()

        def do_build():
            try:
                code = codec.encode(slots, shop=shop, pet=pet)
                self.txt_result.delete(0, "end")
                self.txt_result.insert(0, code)

                dec = codec.decode(code)
                want = slots + ["none"] * (4 - len(slots))
                verify_ok = (dec["slots"] == want and bool(dec["shop"]) == bool(shop) and dec["pet"] == pet)

                is_inferred = (shop and pet > 0)
                status_vi = "SUY LUẬN (Bật đồng thời cả Pet và Shop)" if is_inferred else "ĐÃ KIỂM CHỨNG"
                status_en = "INFERRED (Both Pet and Shop enabled)" if is_inferred else "VERIFIED"
                status = I18n.t(status_vi, status_en)

                if I18n.current_lang == "en":
                    pet_name = "No Pet" if pet == 0 else ("Mr. Waggor (Penguin)" if pet == 1 else "Ottero")
                    log_lines = [
                        "==================================================",
                        "FREE FIRE COMBO CALCULATION RESULT:",
                        f"Code: {code}",
                        f"Status: {status}",
                        f"Automatic Verification: {'PASS (100% Match)' if verify_ok else 'FAIL'}",
                        f"Combination: {' + '.join(slots)} | Shop: {'Yes' if shop else 'No'} | Pet: {pet_name}",
                        f"Data Length: {dec['length']} bytes",
                        "=================================================="
                    ]
                else:
                    pet_name = "Không Pet" if pet == 0 else ("Cánh cụt" if pet == 1 else "Ottero")
                    log_lines = [
                        "==================================================",
                        "KẾT QUẢ TÍNH COMBO FREE FIRE:",
                        f"Mã: {code}",
                        f"Tình trạng: {status}",
                        f"Giải ngược tự động: {'PASS (100% Khớp)' if verify_ok else 'FAIL'}",
                        f"Tổ hợp: {' + '.join(slots)} | Shop: {'Có' if shop else 'Không'} | Trợ thủ: {pet_name}",
                        f"Độ dài: {dec['length']} byte",
                        "=================================================="
                    ]
                self.append_log("\n".join(log_lines))
                self.lbl_progress_text.config(
                    text=I18n.t(
                        f"Ghép combo hoàn tất! ({'PASS' if verify_ok else 'Cảnh báo'})",
                        f"Combo assembly complete! ({'PASS' if verify_ok else 'Warning'})"
                    )
                )
            except Exception as e:
                messagebox.showerror(I18n.t("Lỗi ghép combo", "Combo Assembly Error"), str(e))
                self.lbl_progress_text.config(
                    text=I18n.t("Lỗi tính combo!", "Combo calculation error!")
                )

        self.start_progress(
            I18n.t("Đang ghép và giải mã kiểm chứng combo...", "Assembling and verifying combo code..."),
            do_build
        )

    # -----------------------------------------------------------------
    # Logic Phần 3: Sao chép & Giải ngược
    # -----------------------------------------------------------------
    def on_copy_result(self):
        val = self.txt_result.get().strip()
        if val:
            self.clipboard_clear()
            self.clipboard_append(val)
            self.lbl_progress_text.config(
                text=I18n.t("Đã sao chép mã combo vào Clipboard!", "Combo code copied to Clipboard!")
            )

    def on_verify_result(self):
        code = self.txt_result.get().strip()
        if not code:
            code = simpledialog.askstring(
                I18n.t("Giải Combo", "Solve Combo"),
                I18n.t("Vui lòng nhập hoặc dán mã combo (#FF#...):",
                       "Please enter or paste Free Fire combo code (#FF#...):"),
                parent=self
            )
            if not code or not code.strip():
                return
            code = code.strip()
            self.txt_result.delete(0, "end")
            self.txt_result.insert(0, code)

        m = re.search(r"#FF#([A-Za-z0-9+/=]+)", code)
        if not m:
            messagebox.showerror(
                I18n.t("Lỗi định dạng", "Format Error"),
                I18n.t("Mã combo không đúng định dạng (cần bắt đầu bằng #FF#)!",
                       "Invalid combo code format (must start with #FF#)!")
            )
            return
        clean_code = "#FF#" + m.group(1)

        def do_verify():
            try:
                res = codec.analyze_combo(clean_code, lang=I18n.current_lang)
                if I18n.current_lang == "en":
                    lines = [
                        "==================================================",
                        "FREE FIRE COMBO SYNTHESIS ANALYSIS RESULT:",
                        f"Source Code: {clean_code}",
                        f"Data Length: {res['length']} bytes",
                    ]
                    for s in res["slots"]:
                        lines.append(f"Slot {s['slot']}: {s['name']} (block: {s['block']})")
                    lines.append(f"Shop Tool: {res['shop']['status']} (block: {res['shop']['block']})")
                    lines.append(f"Pet Companion: {res['pet']['name']} (block: {res['pet']['block']})")
                    lines.append("==================================================")
                else:
                    lines = [
                        "==================================================",
                        "KẾT QUẢ PHÂN TÍCH TỔNG HỢP COMBO FREE FIRE:",
                        f"Mã nguồn: {clean_code}",
                        f"Độ dài dữ liệu: {res['length']} byte",
                    ]
                    for s in res["slots"]:
                        lines.append(f"Slot {s['slot']}: {s['name']} (khối: {s['block']})")
                    lines.append(f"Shop Tool: {res['shop']['status']} (khối: {res['shop']['block']})")
                    lines.append(f"Trợ thủ (Pet): {res['pet']['name']} (khối: {res['pet']['block']})")
                    lines.append("==================================================")
                self.append_log("\n".join(lines))
                self.lbl_progress_text.config(
                    text=I18n.t("Phân tích combo hoàn tất!", "Combo analysis complete!")
                )

                # Mở hộp thoại phân tích chi tiết
                ComboAnalysisDialog(self, res)
            except Exception as e:
                messagebox.showerror(I18n.t("Lỗi giải combo", "Solve Combo Error"), str(e))
                self.lbl_progress_text.config(
                    text=I18n.t("Lỗi giải combo!", "Solve combo error!")
                )

        self.start_progress(
            I18n.t("Đang phân tích và tổng hợp combo...", "Analyzing and synthesizing combo components..."),
            do_verify
        )


class ComboAnalysisDialog(tk.Toplevel):
    def __init__(self, master, data):
        super().__init__(master)
        self.master = master
        self.data = data
        self.title(I18n.t("Giải Combo - Phân tích chi tiết thành phần", "Solve Combo - Component Analysis"))
        self.geometry("760x710")
        self.minsize(740, 680)
        self.configure(bg="#f0f0f0")
        self.transient(master)
        self.grab_set()

        self.btn_item_details = []
        self._init_ui()
        self.select_component(0)

    def _init_ui(self):
        # 1. Mã combo nguồn
        grp_source_text = I18n.t(
            f" Mã combo được phân tích ({self.data['length']} bytes giải mã) ",
            f" Analyzed Combo Code ({self.data['length']} decoded bytes) "
        )
        grp_source = tk.LabelFrame(
            self,
            text=grp_source_text,
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=6
        )
        grp_source.pack(fill="x", padx=12, pady=(8, 4))

        src_row = tk.Frame(grp_source, bg="#f0f0f0")
        src_row.pack(fill="x")
        self.ent_src = tk.Entry(src_row, font=("Consolas", 9, "bold"), bg="white", relief="solid", bd=1)
        self.ent_src.insert(0, self.data["code"])
        self.ent_src.config(state="readonly")
        self.ent_src.pack(side="left", fill="x", expand=True, padx=(0, 6))

        btn_copy_src = tk.Button(
            src_row,
            text=I18n.t("Sao chép", "Copy"),
            font=("Segoe UI", 9, "bold"),
            width=10,
            command=lambda: self._copy_to_clip(
                self.data["code"],
                I18n.t("Đã sao chép mã combo vào Clipboard!", "Combo code copied to Clipboard!")
            )
        )
        btn_copy_src.pack(side="left")

        # 2. Danh sách các thành phần
        grp_items = tk.LabelFrame(
            self,
            text=I18n.t(" Các thành phần nhận diện bên trong Combo ", " Recognized Combo Components "),
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=6
        )
        grp_items.pack(fill="x", padx=12, pady=4)

        header_row = tk.Frame(grp_items, bg="#f0f0f0")
        header_row.pack(fill="x", pady=(0, 4))
        tk.Label(
            header_row,
            text=I18n.t("Vị trí", "Position"),
            width=18,
            anchor="w",
            font=("Segoe UI", 8, "bold"),
            bg="#f0f0f0"
        ).pack(side="left")

        tk.Label(
            header_row,
            text=I18n.t("Tên nhận diện / Trạng thái", "Name / Status"),
            width=24,
            anchor="w",
            font=("Segoe UI", 8, "bold"),
            bg="#f0f0f0"
        ).pack(side="left")

        tk.Label(
            header_row,
            text=I18n.t("Khối byte dữ liệu", "Data Bytes Block"),
            width=30,
            anchor="w",
            font=("Segoe UI", 8, "bold"),
            bg="#f0f0f0"
        ).pack(side="left")

        tk.Label(
            header_row,
            text=I18n.t("Tác vụ", "Action"),
            width=12,
            anchor="w",
            font=("Segoe UI", 8, "bold"),
            bg="#f0f0f0"
        ).pack(side="left")

        self.rows_info = []
        for s in self.data["slots"]:
            pos_text = I18n.t(
                f"Slot {s['slot']} ({'Chủ động' if s['slot'] == 1 else 'Bị động'})",
                f"Slot {s['slot']} ({'Active' if s['slot'] == 1 else 'Passive'})"
            )
            name_text = s["name"]
            if name_text == "none":
                name_text = I18n.t("none (Trống)", "none (Empty)")

            self.rows_info.append({
                "type": "char",
                "pos": pos_text,
                "name": name_text,
                "block": s["block"],
                "is_unknown": s["is_unknown"],
                "data": s,
            })

        self.rows_info.append({
            "type": "shop",
            "pos": "Shop Tool",
            "name": self.data["shop"]["status"],
            "block": self.data["shop"]["block"],
            "is_unknown": self.data["shop"]["is_unknown"],
            "data": self.data["shop"],
        })

        self.rows_info.append({
            "type": "pet",
            "pos": I18n.t("Trợ thủ (Pet)", "Pet Companion"),
            "name": self.data["pet"]["name"],
            "block": self.data["pet"]["block"],
            "is_unknown": self.data["pet"]["is_unknown"],
            "data": self.data["pet"],
        })

        for idx, row in enumerate(self.rows_info):
            f_row = tk.Frame(grp_items, bg="#f0f0f0")
            f_row.pack(fill="x", pady=1)

            lbl_pos = tk.Label(f_row, text=row["pos"], width=18, anchor="w", font=("Segoe UI", 9), bg="#f0f0f0")
            lbl_pos.pack(side="left")

            fg_color = "#b00000" if row["is_unknown"] else ("#666666" if "none" in row["name"] or "Không" in row["name"] or "No " in row["name"] else "black")
            font_name = ("Segoe UI", 9, "bold") if row["is_unknown"] else ("Segoe UI", 9)
            lbl_name = tk.Label(f_row, text=row["name"], width=24, anchor="w", font=font_name, fg=fg_color, bg="#f0f0f0")
            lbl_name.pack(side="left")

            lbl_bytes = tk.Label(f_row, text=str(row["block"]), width=30, anchor="w", font=("Consolas", 9), bg="#f0f0f0")
            lbl_bytes.pack(side="left")

            btn_det = tk.Button(
                f_row,
                text="Detail ->",
                font=("Segoe UI", 8, "bold"),
                width=11,
                command=lambda i=idx: self.select_component(i)
            )
            btn_det.pack(side="left")
            self.btn_item_details.append(btn_det)

        # 3. Khung chi tiết (Detail)
        self.grp_detail = tk.LabelFrame(
            self,
            text=I18n.t(" Thông tin chi tiết (Detail) ", " Component Details (Detail) "),
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=6
        )
        self.grp_detail.pack(fill="both", expand=True, padx=12, pady=4)

        heading_row = tk.Frame(self.grp_detail, bg="#f0f0f0")
        heading_row.pack(fill="x", pady=(0, 4))

        self.lbl_detail_heading = tk.Label(heading_row, text="", font=("Segoe UI", 9, "bold"), anchor="w", bg="#f0f0f0")
        self.lbl_detail_heading.pack(side="left", fill="x", expand=True)

        self.lbl_detail_badge = tk.Label(heading_row, text="", font=("Segoe UI", 9, "bold"), bg="#f0f0f0")
        self.lbl_detail_badge.pack(side="right")

        self.container = tk.Frame(self.grp_detail, bg="#f0f0f0")
        self.container.pack(fill="both", expand=True)

        # Panel nhân vật (Full 4 slot codes)
        self.pnl_char = tk.Frame(self.container, bg="#f0f0f0")
        self.char_slot_entries = []
        self.char_slot_labels = []
        for i in range(4):
            slot_prompt = I18n.t(
                f"Mã combo khi ở Slot {i+1} ({'Chủ động' if i == 0 else 'Bị động'}):",
                f"Combo code when placed in Slot {i+1} ({'Active' if i == 0 else 'Passive'}):"
            )
            lbl_slot_prompt = tk.Label(
                self.pnl_char,
                text=slot_prompt,
                anchor="w",
                font=("Segoe UI", 8),
                bg="#f0f0f0"
            )
            lbl_slot_prompt.pack(fill="x", pady=(2, 0))
            self.char_slot_labels.append(lbl_slot_prompt)

            r_ent = tk.Frame(self.pnl_char, bg="#f0f0f0")
            r_ent.pack(fill="x", pady=(0, 2))
            ent = tk.Entry(r_ent, font=("Consolas", 9), bg="white", relief="solid", bd=1)
            ent.pack(side="left", fill="x", expand=True, padx=(0, 6))
            self.char_slot_entries.append(ent)

            btn_cp = tk.Button(
                r_ent,
                text=I18n.t("Sao chép", "Copy"),
                font=("Segoe UI", 8, "bold"),
                width=9,
                command=lambda e=ent: self._copy_to_clip(
                    e.get(),
                    I18n.t("Đã sao chép mã combo vào Clipboard!", "Combo code copied to Clipboard!")
                )
            )
            btn_cp.pack(side="left")

        btn_action_row = tk.Frame(self.pnl_char, bg="#f0f0f0")
        btn_action_row.pack(fill="x", pady=(6, 2))

        btn_copy_all = tk.Button(
            btn_action_row,
            text=I18n.t("Sao chép cả 4 mã", "Copy All 4 Codes"),
            font=("Segoe UI", 9, "bold"),
            command=self.on_copy_all_4
        )
        btn_copy_all.pack(side="left", padx=(0, 6))

        btn_fill_part1 = tk.Button(
            btn_action_row,
            text=I18n.t("Nạp mã vào Phần 1", "Load Code to Part 1"),
            font=("Segoe UI", 9, "bold"),
            command=self.on_fill_to_part1
        )
        btn_fill_part1.pack(side="left")

        lbl_char_hint = tk.Label(
            self.pnl_char,
            text=I18n.t(
                "(* Mẹo: Bấm 'Nạp mã vào Phần 1' để tự động điền mã Slot 1 của nhân vật này vào Phần 1 rồi bấm 'Đăng ký nhân vật' mà không cần copy paste)",
                "(* Tip: Click 'Load Code to Part 1' to autofill the Slot 1 code into Part 1, then click 'Register Character' without manual copy-pasting)"
            ),
            font=("Segoe UI", 8, "italic"),
            fg="#555555",
            bg="#f0f0f0",
            anchor="w"
        )
        lbl_char_hint.pack(fill="x", pady=(4, 0))

        # Panel đơn (Shop Tool / Pet)
        self.pnl_single = tk.Frame(self.container, bg="#f0f0f0")

        self.lbl_single_prompt = tk.Label(self.pnl_single, text="", anchor="w", font=("Segoe UI", 9), bg="#f0f0f0")
        self.lbl_single_prompt.pack(fill="x", pady=(4, 2))

        r_single_ent = tk.Frame(self.pnl_single, bg="#f0f0f0")
        r_single_ent.pack(fill="x", pady=2)

        self.ent_single = tk.Entry(r_single_ent, font=("Consolas", 9, "bold"), bg="white", relief="solid", bd=1)
        self.ent_single.pack(side="left", fill="x", expand=True, padx=(0, 6))

        btn_cp_single = tk.Button(
            r_single_ent,
            text=I18n.t("Sao chép", "Copy"),
            font=("Segoe UI", 9, "bold"),
            width=10,
            command=lambda: self._copy_to_clip(
                self.ent_single.get(),
                I18n.t("Đã sao chép mã độc lập vào Clipboard!", "Isolated standalone code copied to Clipboard!")
            )
        )
        btn_cp_single.pack(side="left")

        self.lbl_single_desc = tk.Label(
            self.pnl_single,
            text="",
            justify="left",
            anchor="w",
            font=("Segoe UI", 9),
            fg="#444444",
            bg="#f0f0f0"
        )
        self.lbl_single_desc.pack(fill="x", pady=(10, 0))

        # 4. Nút Đóng
        btn_bottom = tk.Frame(self, bg="#f0f0f0")
        btn_bottom.pack(fill="x", padx=12, pady=(4, 8))
        btn_close = tk.Button(
            btn_bottom,
            text=I18n.t("Đóng", "Close"),
            font=("Segoe UI", 9, "bold"),
            width=12,
            command=self.destroy
        )
        btn_close.pack(side="right")

    def _copy_to_clip(self, val, msg=None):
        if val:
            self.clipboard_clear()
            self.clipboard_append(val)
            if msg:
                messagebox.showinfo(I18n.t("Thông báo", "Notice"), msg)

    def select_component(self, idx):
        view_text = I18n.t(">> Xem <<", ">> View <<")
        det_text = "Detail ->"
        for i, b in enumerate(self.btn_item_details):
            b.config(text=view_text if i == idx else det_text)

        row = self.rows_info[idx]
        if row["type"] == "char":
            self.pnl_single.pack_forget()
            self.pnl_char.pack(fill="both", expand=True)

            heading = I18n.t(
                f"Chi tiết {row['pos']}: {row['name']} (Khối byte: {row['block']})",
                f"Details for {row['pos']}: {row['name']} (Byte block: {row['block']})"
            )
            self.lbl_detail_heading.config(text=heading)

            if row["is_unknown"]:
                self.lbl_detail_badge.config(text="UNKNOWN", fg="#b00000")
            elif "none" in row["name"]:
                self.lbl_detail_badge.config(text=I18n.t("SLOT TRỐNG", "EMPTY SLOT"), fg="#666666")
            else:
                self.lbl_detail_badge.config(text=I18n.t("ĐÃ NHẬN DIỆN", "IDENTIFIED"), fg="#008000")

            codes = row["data"]["codes"]
            for i, ent in enumerate(self.char_slot_entries):
                ent.config(state="normal")
                ent.delete(0, "end")
                ent.insert(0, codes[i])
                ent.config(state="readonly")
        else:
            self.pnl_char.pack_forget()
            self.pnl_single.pack(fill="both", expand=True)

            heading = I18n.t(
                f"Chi tiết {row['pos']}: {row['name']} (Khối byte: {row['block']})",
                f"Details for {row['pos']}: {row['name']} (Byte block: {row['block']})"
            )
            self.lbl_detail_heading.config(text=heading)

            if row["is_unknown"]:
                self.lbl_detail_badge.config(text="UNKNOWN", fg="#b00000")
            else:
                badge_text = "SHOP TOOL" if row["type"] == "shop" else I18n.t("TRỢ THỦ (PET)", "PET COMPANION")
                self.lbl_detail_badge.config(text=badge_text, fg="#0055aa")

            iso_code = row["data"]["isolated_code"]
            self.ent_single.config(state="normal")
            self.ent_single.delete(0, "end")
            self.ent_single.insert(0, iso_code)
            self.ent_single.config(state="readonly")

            if row["type"] == "shop":
                self.lbl_single_prompt.config(
                    text=I18n.t(
                        "Mã combo riêng biệt khi chỉ kích hoạt Shop Tool này:",
                        "Isolated combo code with only this Shop Tool activated:"
                    )
                )
                self.lbl_single_desc.config(
                    text=I18n.t(
                        "Mã trên là mã độc lập đại diện cho cấu hình Shop Tool được trích xuất từ combo nguồn.\n"
                        f"Khối byte dữ liệu gốc của Shop: {row['block']}\n\n"
                        "Bạn có thể sao chép mã này để chia sẻ hoặc lưu trữ độc lập.",
                        "The code above is an isolated standalone code representing the Shop Tool extracted from the source combo.\n"
                        f"Raw Shop byte block: {row['block']}\n\n"
                        "You can copy this code to share or store independently."
                    )
                )
            else:
                self.lbl_single_prompt.config(
                    text=I18n.t(
                        "Mã combo riêng biệt khi chỉ trang bị Trợ thủ (Pet) này:",
                        "Isolated combo code with only this Pet equipped:"
                    )
                )
                self.lbl_single_desc.config(
                    text=I18n.t(
                        "Mã trên là mã độc lập đại diện cho cấu hình Trợ thủ (Pet) được trích xuất từ combo nguồn.\n"
                        f"Khối byte dữ liệu gốc của Pet: {row['block']}\n\n"
                        "Bạn có thể sao chép mã này để chia sẻ hoặc lưu trữ độc lập.",
                        "The code above is an isolated standalone code representing the Pet companion extracted from the source combo.\n"
                        f"Raw Pet byte block: {row['block']}\n\n"
                        "You can copy this code to share or store independently."
                    )
                )

    def on_copy_all_4(self):
        codes = [ent.get() for ent in self.char_slot_entries]
        text = "\n".join([f"Slot {i+1}: {c}" for i, c in enumerate(codes)])
        self._copy_to_clip(
            text,
            I18n.t("Đã sao chép cả 4 mã vào Clipboard!", "All 4 codes copied to Clipboard!")
        )

    def on_fill_to_part1(self):
        code = self.char_slot_entries[0].get()
        self.master.ent_slot1.delete(0, "end")
        self.master.ent_slot1.insert(0, code)
        btn_name = I18n.t("Đăng ký nhân vật", "Register Character")
        messagebox.showinfo(
            I18n.t("Đã nạp thành công", "Loaded Successfully"),
            I18n.t(
                f"Đã nạp mã Slot 1 vào ô 'Slot 1 Code Combo' ở Phần 1 giao diện chính!\nBạn chỉ cần bấm nút '{btn_name}' tại Phần 1 để lưu nhân vật này.",
                f"Loaded Slot 1 code into 'Slot 1 Code Combo' in Part 1!\nSimply click '{btn_name}' in Part 1 to register this character."
            )
        )
        self.destroy()


if __name__ == "__main__":
    app = ComboCalculatorApp()
    app.mainloop()
