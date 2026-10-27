#!/usr/bin/env python3
"""
combo_calculator_gui.py - Ứng dụng Win32 Native cổ điển tính toán và ghép Combo Free Fire.
Dựa trên nền tảng thuật toán combo_codec.py.
"""
import os
import re
import tkinter as tk
from tkinter import ttk, messagebox, simpledialog
from tkinter.scrolledtext import ScrolledText

import combo_codec as codec


class ComboCalculatorApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Free Fire Combo Calculator")
        self.geometry("820x680")
        self.minsize(800, 650)
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

    def _init_ui(self):
        # -------------------------------------------------------------
        # 1. NHẬP DỮ LIỆU NHÂN VẬT
        # -------------------------------------------------------------
        grp1 = tk.LabelFrame(
            self,
            text=" 1. Nhập dữ liệu nhân vật ",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=8
        )
        grp1.pack(fill="x", padx=12, pady=6)

        self.slot_entries = []
        for i in range(4):
            row = tk.Frame(grp1, bg="#f0f0f0")
            row.pack(fill="x", pady=2)
            lbl = tk.Label(row, text=f"Slot {i+1}:", width=7, anchor="w", bg="#f0f0f0", font=("Segoe UI", 9))
            lbl.pack(side="left")
            ent = tk.Entry(row, font=("Segoe UI", 9))
            ent.pack(side="left", fill="x", expand=True)
            self.slot_entries.append(ent)

        btn_row1 = tk.Frame(grp1, bg="#f0f0f0")
        btn_row1.pack(fill="x", pady=(5, 0))
        btn_import = tk.Button(
            btn_row1,
            text="Nhập",
            font=("Segoe UI", 9, "bold"),
            width=12,
            command=self.on_import_character
        )
        btn_import.pack(side="left", padx=(50, 6))

        btn_clear = tk.Button(
            btn_row1,
            text="Xóa trắng",
            font=("Segoe UI", 9),
            width=10,
            command=self.on_clear_inputs
        )
        btn_clear.pack(side="left")

        btn_guide = tk.Button(
            btn_row1,
            text="Cách lấy mã slot",
            font=("Segoe UI", 9),
            command=self.on_show_guide
        )
        btn_guide.pack(side="left", padx=6)

        lbl_guide_hint = tk.Label(
            btn_row1,
            text="(* Bấm 'Cách lấy mã slot' để xem ảnh hướng dẫn)",
            font=("Segoe UI", 8, "italic"),
            fg="#555555",
            bg="#f0f0f0"
        )
        lbl_guide_hint.pack(side="left", padx=4)

        # -------------------------------------------------------------
        # 2. GHÉP THÀNH COMBO
        # Kiểu: {Choicebox1} + {Choicebox2} + {Choicebox3} + {Choicebox4} + {Choicebox5} + {Choicebox6}
        # -------------------------------------------------------------
        grp2 = tk.LabelFrame(
            self,
            text=" 2. Ghép thành combo ",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=8
        )
        grp2.pack(fill="x", padx=12, pady=6)

        # Tiêu đề cột nhỏ
        header_row = tk.Frame(grp2, bg="#f0f0f0")
        header_row.pack(fill="x")
        headers = [
            ("Slot 1 (Chủ động)", 14),
            ("Slot 2 (Bị động)", 14),
            ("Slot 3 (Bị động)", 14),
            ("Slot 4 (Bị động)", 14),
            ("Shop Tool", 13),
            ("Trợ Thủ (Pet)", 14)
        ]
        for h, w in headers:
            lbl = tk.Label(header_row, text=h, width=w, anchor="w", bg="#f0f0f0", font=("Segoe UI", 8))
            lbl.pack(side="left", padx=3)

        # Hàng 6 Choicebox kèm dấu +
        choices_row = tk.Frame(grp2, bg="#f0f0f0")
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
        self.cb_pet = ttk.Combobox(choices_row, state="readonly", width=13, font=("Segoe UI", 9))
        self.cb_pet["values"] = ["Không Pet (0)", "Cánh cụt", "Ottero"]
        self.cb_pet.current(0)
        self.cb_pet.pack(side="left", padx=2)

        # Nút Ghép Combo ở dưới
        btn_row2 = tk.Frame(grp2, bg="#f0f0f0")
        btn_row2.pack(fill="x", pady=(6, 0))

        btn_build = tk.Button(
            btn_row2,
            text="Ghép Combo",
            font=("Segoe UI", 9, "bold"),
            width=14,
            command=self.on_build_combo
        )
        btn_build.pack(side="left", padx=(2, 6))

        btn_reset = tk.Button(
            btn_row2,
            text="Đặt lại",
            font=("Segoe UI", 9),
            width=10,
            command=self.on_reset_combo
        )
        btn_reset.pack(side="left")

        # -------------------------------------------------------------
        # 3. THANH PROGRESS VÀ KẾT QUẢ TÍNH COMBO
        # -------------------------------------------------------------
        grp3 = tk.LabelFrame(
            self,
            text=" 3. Thanh progress và kết quả tính combo ",
            font=("Segoe UI", 9, "bold"),
            bg="#f0f0f0",
            padx=10,
            pady=8
        )
        grp3.pack(fill="both", expand=True, padx=12, pady=6)

        self.lbl_progress_text = tk.Label(
            grp3,
            text="Tiến độ sẵn sàng",
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 8)
        )
        self.lbl_progress_text.pack(fill="x")

        self.progress_bar = ttk.Progressbar(grp3, orient="horizontal", mode="determinate")
        self.progress_bar.pack(fill="x", pady=2)

        lbl_res = tk.Label(
            grp3,
            text="Mã Combo Free Fire (#FF#...):",
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 9)
        )
        lbl_res.pack(fill="x", pady=(6, 1))

        res_row = tk.Frame(grp3, bg="#f0f0f0")
        res_row.pack(fill="x", pady=2)

        self.txt_result = tk.Entry(
            res_row,
            font=("Consolas", 10, "bold"),
            bg="white",
            relief="solid",
            bd=1
        )
        self.txt_result.pack(side="left", fill="x", expand=True, padx=(0, 6))

        btn_copy = tk.Button(
            res_row,
            text="Sao chép mã",
            font=("Segoe UI", 9, "bold"),
            width=12,
            command=self.on_copy_result
        )
        btn_copy.pack(side="left", padx=2)

        btn_verify = tk.Button(
            res_row,
            text="Giải ngược",
            font=("Segoe UI", 9),
            width=11,
            command=self.on_verify_result
        )
        btn_verify.pack(side="left", padx=2)

        lbl_log = tk.Label(
            grp3,
            text="Chi tiết phân tích & Nhật ký:",
            anchor="w",
            bg="#f0f0f0",
            font=("Segoe UI", 9)
        )
        lbl_log.pack(fill="x", pady=(6, 1))

        self.log_area = ScrolledText(
            grp3,
            font=("Consolas", 9),
            bg="white",
            relief="solid",
            bd=1,
            height=10
        )
        self.log_area.pack(fill="both", expand=True, pady=2)

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
        for ent in self.slot_entries:
            ent.delete(0, "end")

    def on_show_guide(self):
        guide_win = tk.Toplevel(self)
        guide_win.title("Hướng dẫn: Cách lấy mã slot")
        guide_win.geometry("700x745")
        guide_win.minsize(680, 700)
        guide_win.configure(bg="#f0f0f0")
        guide_win.transient(self)
        guide_win.grab_set()

        lbl_text = tk.Label(
            guide_win,
            text=(
                "Để lấy mã của các slot để nhập một nhân vật mới vào hộp chọn trong phần ghép combo "
                "thì hãy vào game rồi chọn 1 nhân vật duy nhất cần nhập vào rồi làm theo hình ảnh để "
                "lấy mã các slot của nhân vật mong muốn."
            ),
            font=("Segoe UI", 9),
            wraplength=660,
            justify="left",
            bg="#f0f0f0",
            padx=12,
            pady=10
        )
        lbl_text.pack(fill="x")

        img_path = os.path.join(os.path.dirname(__file__), "huong_dan_slot.png")
        if os.path.exists(img_path):
            try:
                self._guide_photo = tk.PhotoImage(file=img_path)
                lbl_img = tk.Label(guide_win, image=self._guide_photo, bg="black", relief="solid", bd=1)
                lbl_img.pack(padx=12, pady=5)
            except Exception as e:
                lbl_err = tk.Label(guide_win, text=f"Lỗi hiển thị ảnh: {e}", fg="red", bg="#f0f0f0")
                lbl_err.pack(pady=10)
        else:
            lbl_err = tk.Label(guide_win, text=f"Không tìm thấy file ảnh: {img_path}", fg="red", bg="#f0f0f0")
            lbl_err.pack(pady=10)

        btn_close = tk.Button(
            guide_win,
            text="Đóng",
            font=("Segoe UI", 9, "bold"),
            width=12,
            command=guide_win.destroy
        )
        btn_close.pack(pady=10)

    def on_import_character(self):
        filled_slots = []
        extracted_blocks = []
        detected_name = ""

        for i, ent in enumerate(self.slot_entries):
            text = ent.get().strip()
            if not text:
                continue

            # Thử đoán tên nhân vật
            m_name = re.match(r"^([A-Za-z0-9_\s\(\)\u00C0-\u1EF9]+?)[\s\:\;\(]+#FF#", text)
            if m_name and not detected_name:
                cand = re.sub(r"\(\d+\)", "", m_name.group(1)).strip()
                if cand:
                    detected_name = cand

            m_code = re.search(r"#FF#([A-Za-z0-9+/=]+)", text)
            code = ("#FF#" + m_code.group(1)) if m_code else text

            try:
                p = codec.plain_of(code)
                if len(p) < 2 or p[0] != 0 or p[1] != codec.SEP:
                    messagebox.showerror("Lỗi", f"Mã ở Slot {i+1} có header không đúng!")
                    return

                # Tách segments
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
                    messagebox.showerror("Lỗi", f"Mã ở Slot {i+1} không đủ 4 slot!")
                    return

                target_seg = segs[i]
                if target_seg == codec.NONE_SEG:
                    messagebox.showwarning("Thông báo", f"Mã ở Slot {i+1} là slot trống (none)!")
                    return

                filled_slots.append(i + 1)
                extracted_blocks.append(target_seg)
            except Exception as e:
                messagebox.showerror("Lỗi giải mã", f"Không thể giải mã Slot {i+1}: {e}")
                return

        if not filled_slots:
            messagebox.showinfo("Chưa có dữ liệu", "Vui lòng nhập ít nhất 1 mã (#FF#...) vào các ô Slot 1..4!")
            return

        # Kiểm tra tính đồng nhất giữa các slot
        first_b = extracted_blocks[0]
        for j in range(1, len(extracted_blocks)):
            if extracted_blocks[j] != first_b:
                messagebox.showwarning(
                    "Không đồng nhất",
                    f"Cảnh báo: Khối trích xuất ở Slot {filled_slots[j]} khác Slot {filled_slots[0]}!\n"
                    f"Slot {filled_slots[0]}: {first_b}\n"
                    f"Slot {filled_slots[j]}: {extracted_blocks[j]}"
                )
                return

        def process_done():
            # Kiểm tra xem khối đã có chưa
            block_tuple = tuple(first_b)
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
                msg = f"==> NHẬP DỮ LIỆU: Nhân vật đã có sẵn!\n- Tên: {existing}\n- Khối: {first_b}\n- Khớp qua các slot: {filled_slots}"
                self.append_log(msg)
                self.txt_result.delete(0, "end")
                self.txt_result.insert(0, existing)
                self._refresh_choiceboxes(existing)
                self.lbl_progress_text.config(text=f"Đã nhận diện: {existing}")
                messagebox.showinfo("Đã nhận diện", f"Nhân vật đã có sẵn: {existing} (Khối: {first_b})")
            else:
                # Nhân vật mới
                prompt_text = f"Phát hiện nhân vật mới!\nKhối byte: {first_b}\nNhập tên nhân vật:"
                new_name = simpledialog.askstring("Nhân vật mới", prompt_text, initialvalue=detected_name, parent=self)
                if not new_name or not new_name.strip():
                    self.append_log(f"Đã hủy thêm nhân vật mới (khối: {first_b}).")
                    self.lbl_progress_text.config(text="Đã hủy lưu nhân vật mới.")
                    return

                new_name = new_name.strip()
                codec.save_character(new_name, first_b)
                self._refresh_choiceboxes(new_name)
                self.txt_result.delete(0, "end")
                self.txt_result.insert(0, new_name)
                msg = f"==> THÀNH CÔNG: Đã lưu nhân vật mới!\n- Tên: {new_name}\n- Khối byte: {first_b}\n- Cập nhật Choicebox."
                self.append_log(msg)
                self.lbl_progress_text.config(text=f"Đã lưu thành công: {new_name}")
                messagebox.showinfo("Thành công", f"Đã lưu thành công nhân vật '{new_name}'!")

        self.start_progress("Đang phân tích dữ liệu nhân vật...", process_done)

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
                status = "SUY LUẬN (Bật đồng thời cả Pet và Shop)" if is_inferred else "ĐÃ KIỂM CHỨNG"

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
                self.lbl_progress_text.config(text=f"Ghép combo hoàn tất! ({'PASS' if verify_ok else 'Cảnh báo'})")
            except Exception as e:
                messagebox.showerror("Lỗi ghép combo", str(e))
                self.lbl_progress_text.config(text="Lỗi tính combo!")

        self.start_progress("Đang ghép và giải mã kiểm chứng combo...", do_build)

    # -----------------------------------------------------------------
    # Logic Phần 3: Sao chép & Giải ngược
    # -----------------------------------------------------------------
    def on_copy_result(self):
        val = self.txt_result.get().strip()
        if val:
            self.clipboard_clear()
            self.clipboard_append(val)
            self.lbl_progress_text.config(text="Đã sao chép mã combo vào Clipboard!")

    def on_verify_result(self):
        code = self.txt_result.get().strip()
        if not code:
            messagebox.showinfo("Thông báo", "Chưa có mã để giải ngược!")
            return

        def do_verify():
            try:
                dec = codec.decode(code)
                info = (
                    "--- KẾT QUẢ GIẢI NGƯỢC CHI TIẾT ---\n"
                    f"Mã nguồn: {code}\n"
                    f"Độ dài: {dec['length']} byte\n"
                    f"Slot 1: {dec['slots'][0]}\n"
                    f"Slot 2: {dec['slots'][1]}\n"
                    f"Slot 3: {dec['slots'][2]}\n"
                    f"Slot 4: {dec['slots'][3]}\n"
                    f"Shop: {'Có Shop Tool' if dec['shop'] else 'Không Shop'}\n"
                    f"Trợ thủ (Pet): {'Không Pet' if dec['pet'] == 0 else ('Cánh cụt' if dec['pet'] == 1 else 'Ottero')}\n"
                    "-----------------------------------"
                )
                self.append_log(info)
                self.lbl_progress_text.config(text="Giải ngược thành công!")
                messagebox.showinfo("Kết quả giải ngược", info)
            except Exception as e:
                messagebox.showerror("Lỗi giải ngược", str(e))

        self.start_progress("Đang giải ngược mã...", do_verify)


if __name__ == "__main__":
    app = ComboCalculatorApp()
    app.mainloop()
