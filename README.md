# Free Fire Combo Calculator

Công cụ tạo và giải mã chuỗi combo nhân vật Free Fire (dạng `#FF#...`).

Hỗ trợ trích xuất khối byte nhân vật mới từ game, ghép tổ hợp kỹ năng tùy chọn (kèm Shop Tool và Trợ thủ), tự động giải ngược đối soát tính hợp lệ và hỗ trợ **song ngữ Tiếng Việt / English**.

---

## 1. Cách sử dụng

Bạn có thể chạy bằng 1 trong 2 cách:

- **Cách 1 (Khuyên dùng trên Windows):** Chạy trực tiếp file `FF_Combo_Calculator.exe`. Bản này viết bằng Win32 C# thuần, dung lượng nhẹ (~300 KB do nhúng sẵn ảnh hướng dẫn), không cần cài Python hay bất kỳ thư viện nào, mở lên là dùng.
- **Cách 2 (Python):** Nếu dùng môi trường Python, chạy lệnh:
  ```bash
  python combo_calculator_gui.py
  ```
  *(hoặc click đúp vào file `run_gui.bat`)*

---

## 2. Các chức năng trên giao diện

### Chuyển đổi ngôn ngữ (Language)
Ở góc trên bên phải giao diện có thanh chọn nhanh ngôn ngữ:
- **Tiếng Việt:** Toàn bộ giao diện, thông báo, nhật ký, hướng dẫn và phân tích hiển thị chuẩn tiếng Việt.
- **English:** Chuẩn hóa thuật ngữ quốc tế chính xác của game Free Fire (Slot 1 Active, Slots 2-4 Passive, Mr. Waggor, Ottero, Shop Tool,...).

### 1. Nhập dữ liệu nhân vật
Dùng để thêm nhân vật mới vào phần mềm:
- Vào game, tháo hết kỹ năng, chỉ để duy nhất 1 nhân vật cần lấy mã.
- Lần lượt đặt nhân vật đó vào Slot 1, Slot 2, Slot 3, Slot 4 rồi bấm nút sao chép mã trong game.
- Dán mã tương ứng vào 4 ô `Slot 1` đến `Slot 4` trong phần mềm rồi bấm **Nhập**.
- Phần mềm sẽ tự trích xuất khối byte, kiểm tra tính đồng nhất giữa các slot và yêu cầu đặt tên nếu là nhân vật mới. Nhân vật sau khi lưu sẽ tự động xuất hiện ở các hộp chọn bên dưới.
- Bấm nút **Cách lấy mã slot** để xem ảnh minh họa chi tiết vị trí lấy mã trong game.

### 2. Ghép thành combo
- Chọn kỹ năng cho 4 slot theo ý muốn (Slot 1 là kỹ năng chủ động, Slot 2-4 là bị động).
- Chọn trạng thái **Shop Tool** (Có / Không).
- Chọn **Trợ thủ** (Không Pet / Cánh cụt / Ottero).
- Bấm **Ghép Combo** để tạo chuỗi mã.

### 3. Thanh progress và kết quả
- Hiển thị chuỗi mã `#FF#...` tạo được (ô nhập có thể gõ hoặc dán trực tiếp bất kỳ mã `#FF#...` nào để giải).
- Nút **Sao chép mã**: Copy nhanh mã vào bộ nhớ tạm để dán vào game.
- Nút **Giải Combo**: Dùng thuật toán phân tích tổng hợp toàn bộ chuỗi mã `#FF#...` bên trong:
  - Mở cửa sổ phân tích bóc tách cả 6 thành phần: Slot 1, 2, 3, 4, Shop Tool và Trợ thủ (Pet).
  - Tự động nhận diện tên nhân vật theo khối byte; nếu gặp nhân vật/vật phẩm lạ chưa có trong hệ thống sẽ tự động gắn nhãn **Unknown**.
  - Mỗi thành phần có nút **Detail** riêng:
    - **Nhân vật (Slot 1-4):** Tự động sinh đầy đủ **full 4 mã combo ở cả 4 slot** (Slot 1 chủ động, Slot 2-4 bị động), kèm nút sao chép từng slot, sao chép cả 4 slot và nút **"Nạp 4 mã vào Phần 1"** (tự điền 4 mã vào 4 ô ở Phần 1 để bấm "Nhập" lưu nhân vật mới ngay lập tức mà không cần copy paste thủ công).
    - **Shop Tool & Trợ thủ:** Hiển thị mã combo riêng biệt (độc lập) của riêng vật phẩm đó khi chỉ trang bị duy nhất thành phần này.
- Khung nhật ký bên dưới ghi lại chi tiết các byte, độ dài và phân loại mã.

---

## 3. Chạy qua dòng lệnh (CLI)

File `combo_codec.py` chứa toàn bộ thuật toán lõi, có thể chạy độc lập:

- **Chạy tự kiểm tra dữ liệu:**
  ```bash
  python combo_codec.py --selftest
  ```
- **Tạo mã combo:**
  ```bash
  python combo_codec.py --encode Tatsuya Chidori Kelly Dasha --shop --pet 1
  ```
  *(pet: 0 = không pet, 1 = Cánh cụt, 2 = Ottero; bật shop thì thêm cờ `--shop`)*

- **Giải mã một chuỗi:**
  ```bash
  python combo_codec.py --decode "#FF#fmJweHxpfG1ucnR/..."
  ```

---

## 4. Cấu trúc thư mục

- `FF_Combo_Calculator.exe`: Ứng dụng Win32 Native chạy trực tiếp.
- `FF_Combo_Calculator.cs`: Mã nguồn C# WinForms của ứng dụng.
- `combo_calculator_gui.py`: Giao diện ứng dụng viết bằng Python (Tkinter).
- `combo_codec.py`: Module mã hóa/giải mã và script dòng lệnh.
- `characters.json`: File dữ liệu lưu danh sách khối byte của các nhân vật.
- `huong_dan_slot.png`: Ảnh chụp màn hình hướng dẫn lấy mã 4 slot trong game.
- `run_gui.bat`: File bật nhanh bản Python GUI.

---

## 5. Nguyên lý mã hóa (Tóm tắt)

Mỗi mã combo là chuỗi Base64 có tiền tố `#FF#`. Bản rõ được giải mã bằng cách XOR từng byte với mảng khóa chu kỳ 22 phần tử, tính từ phải sang trái:
```text
plain[i] = raw_byte[i] XOR KEY[(length - 1 - i) % 22]
```
Bản rõ được cấu trúc thành các đoạn phân cách bởi số `23`:
- Header: `0`
- 4 slot nhân vật (mỗi slot gồm byte `23` + khối byte của nhân vật, hoặc `[23, 1]` nếu để trống)
- Slot Shop Tool (`[23, 1]` nếu tắt, hoặc đoạn shop tool nếu bật)
- Đoạn cố định: `[23, 0, 1, 4]`
- Slot Trợ thủ: `[23, 1]` (không pet), `[23, 0, 2, 0, 4, 1, 1, 1, 1, 1, 8]` (Cánh cụt), hoặc `[23, 0, 2, 0, 4, 1, 1, 1, 1, 0, 1]` (Ottero).
