# YÊU CẦU THIẾT KẾ GIAO DIỆN
## Dự án: Hệ thống Quản lý Bán Điện thoại Di động

> Tài liệu này quy định các nguyên tắc thiết kế giao diện (UI/UX) bắt buộc phải tuân thủ khi phát triển frontend cho dự án. Bất kỳ AI hoặc lập trình viên nào tham gia phát triển đều phải đọc và áp dụng đúng các quy tắc dưới đây.

---

## 1. Tổng quan dự án

- **Tên dự án**: Hệ thống quản lý bán điện thoại di động
- **Công nghệ sử dụng**:
  - Backend: **C#** (.NET)
  - Frontend: **HTML** (kèm CSS/JavaScript)
  - Trao đổi/lưu trữ dữ liệu: **XML**
- **Đối tượng người dùng**: Nhân viên bán hàng, quản lý cửa hàng, khách hàng xem sản phẩm
- **Mục tiêu giao diện**: Hiện đại, sáng, dễ thao tác, có hiệu ứng động đẹp mắt khi hiển thị và lướt qua sản phẩm

---

## 2. Nguyên tắc thiết kế chung

1. **Hiện đại (Modern)**: Sử dụng flat design/neumorphism nhẹ, bo góc mềm mại (border-radius 8–16px), tránh các chi tiết rườm rà, đổ bóng (shadow) tinh tế để tạo chiều sâu.
2. **Sáng (Light theme)**: Nền chủ đạo là màu sáng/trắng, tạo cảm giác thoáng, sạch sẽ, chuyên nghiệp.
3. **Dễ thao tác (Usability)**: 
   - Bố cục rõ ràng, phân cấp thông tin hợp lý (hierarchy).
   - Nút bấm (button) đủ lớn, dễ bấm, có trạng thái hover/active/disabled rõ ràng.
   - Điều hướng (navigation) đơn giản, tối đa 3 cấp menu.
   - Form nhập liệu có validate trực quan (inline, không cần load lại trang).
4. **Nhất quán (Consistency)**: Toàn bộ hệ thống dùng chung 1 bảng màu, 1 bộ font, 1 hệ thống spacing.

---

## 3. Bảng màu (Color Palette)

| Vai trò | Màu gợi ý | Mã màu (HEX) |
|---|---|---|
| Màu nền chính | Trắng / Xám rất nhạt | `#FFFFFF`, `#F7F8FA` |
| Màu chính (Primary) | Xanh dương hiện đại | `#2F6FED` |
| Màu nhấn (Accent) | Xanh ngọc / Cam nhẹ | `#00C2A8` hoặc `#FF7A45` |
| Màu chữ chính | Đen xám đậm | `#1A1A1A` |
| Màu chữ phụ | Xám trung | `#6B7280` |
| Màu cảnh báo lỗi | Đỏ | `#E5484D` |
| Màu thành công | Xanh lá | `#2ECC71` |
| Đường viền / phân cách | Xám nhạt | `#E5E7EB` |

> Không dùng nền tối (dark mode) làm giao diện mặc định. Có thể chuẩn bị biến CSS để mở rộng dark mode sau nhưng không bắt buộc ở phiên bản đầu.

---

## 4. Typography

- **Font chữ**: Sử dụng font sans-serif hiện đại, dễ đọc — ưu tiên `Inter`, `Be Vietnam Pro` (hỗ trợ tiếng Việt tốt), hoặc `Roboto`.
- **Kích thước chữ**:
  - Tiêu đề lớn (H1): 28–32px, đậm (bold/700)
  - Tiêu đề phụ (H2): 20–24px, semi-bold (600)
  - Nội dung chính: 14–16px, regular (400)
  - Chữ phụ/chú thích: 12–13px, màu xám phụ
- Giãn dòng (line-height) tối thiểu 1.4 để dễ đọc.

---

## 5. Bố cục & Component

### 5.1 Layout tổng thể
- Header cố định (fixed) chứa logo, thanh tìm kiếm, giỏ hàng/thông báo, avatar tài khoản.
- Sidebar (đối với trang quản trị/nhân viên) chứa menu điều hướng chính.
- Grid layout responsive cho danh sách sản phẩm (2 cột trên mobile, 3–4 cột trên desktop).

### 5.2 Card sản phẩm (Product Card)
- Bo góc mềm (`border-radius: 12px`), có shadow nhẹ khi hover.
- Ảnh sản phẩm chiếm phần lớn diện tích card, tỉ lệ vuông hoặc 4:3.
- Hiển thị: tên sản phẩm, giá, (giá gốc gạch ngang nếu giảm giá), đánh giá sao, nút "Thêm vào giỏ".
- Khi hover: card nâng nhẹ lên (`transform: translateY(-4px)`), shadow đậm hơn, ảnh có hiệu ứng zoom nhẹ (`scale(1.05)`), thời gian chuyển động 0.25–0.3s với `ease-in-out`.

### 5.3 Nút bấm (Buttons)
- Nút chính: nền màu Primary, chữ trắng, bo góc 8px.
- Nút phụ: viền màu Primary, nền trong suốt, chữ màu Primary.
- Hiệu ứng khi bấm: giảm độ sáng nhẹ hoặc hiệu ứng ripple.

### 5.4 Form
- Input có border mỏng, khi focus đổi màu viền sang Primary kèm hiệu ứng glow nhẹ.
- Label nằm phía trên input, rõ ràng, không dùng placeholder thay label.

---

## 6. Hiệu ứng động (Animation) — Trọng tâm khi lướt sản phẩm

Đây là yêu cầu **bắt buộc và nổi bật** của giao diện:

1. **Hiệu ứng xuất hiện khi cuộn trang (scroll reveal)**: 
   - Các card sản phẩm xuất hiện dần (fade-in + trượt nhẹ từ dưới lên `translateY(20px) → 0`) khi được cuộn tới, sử dụng `Intersection Observer API`.
   - Stagger effect: các card xuất hiện lệch thời gian nhau ~50–100ms để tạo cảm giác mượt mà, không xuất hiện đồng loạt.

2. **Hiệu ứng hover trên sản phẩm**:
   - Ảnh sản phẩm zoom nhẹ, card nâng lên, shadow đậm hơn (đã nêu ở mục 5.2).
   - Nút "Thêm vào giỏ" hiện ra hoặc đổi màu mượt khi hover vào card.

3. **Hiệu ứng chuyển trang / chuyển tab**: dùng fade hoặc slide nhẹ (200–300ms), tránh giật cục.

4. **Hiệu ứng carousel/slider sản phẩm nổi bật (banner, sản phẩm hot)**: chuyển động mượt, tự động chạy, có thể vuốt (swipe) trên mobile.

5. **Micro-interactions**:
   - Khi thêm vào giỏ hàng: icon giỏ hàng "nảy" nhẹ (bounce) để phản hồi hành động.
   - Khi submit form thành công: hiệu ứng checkmark động hoặc toast notification trượt vào từ góc màn hình.

6. **Nguyên tắc chung cho animation**:
   - Thời lượng animation: 150–400ms (không quá chậm gây khó chịu).
   - Dùng easing tự nhiên: `ease-in-out`, `cubic-bezier` mượt mà, tránh `linear`.
   - Animation phải có mục đích (feedback, dẫn hướng sự chú ý), không lạm dụng gây rối mắt.
   - Tôn trọng `prefers-reduced-motion` cho người dùng nhạy cảm với chuyển động.

---

## 7. Responsive & Đa nền tảng

- Giao diện phải responsive tốt trên Desktop, Tablet, Mobile.
- Breakpoint gợi ý: Mobile (<576px), Tablet (576–992px), Desktop (>992px).
- Grid sản phẩm tự điều chỉnh số cột theo màn hình.

---

## 8. Ràng buộc kỹ thuật liên quan

- Dữ liệu sản phẩm/đơn hàng trao đổi giữa Backend (C#) và Frontend (HTML) thông qua định dạng **XML** — giao diện cần xử lý parse/hiển thị dữ liệu từ XML một cách mượt mà, có trạng thái loading/skeleton khi chờ dữ liệu.
- Ưu tiên dùng HTML thuần + CSS + JavaScript (có thể kèm thư viện nhẹ nếu cần) để tương thích tốt với backend C#/ASP.NET.
- Code giao diện cần rõ ràng, dễ bảo trì, đặt tên class theo quy tắc nhất quán (khuyến nghị BEM hoặc tương tự).

---

## 9. Nguyên tắc khi AI/Dev thực hiện thiết kế

Khi bất kỳ ai (kể cả AI) triển khai giao diện cho dự án này, cần:
1. Bám sát bảng màu, font chữ, spacing đã quy định ở trên — không tự ý đổi phong cách.
2. Ưu tiên trải nghiệm mượt mà, hiệu ứng động tinh tế cho phần hiển thị sản phẩm (mục 6).
3. Đảm bảo giao diện sáng, sạch, hiện đại, dễ thao tác cho cả nhân viên bán hàng lẫn khách hàng.
4. Kiểm tra responsive trên nhiều kích thước màn hình trước khi hoàn thiện.
5. Giữ code sạch, có comment khi cần, thuận tiện cho việc tích hợp với backend C#/XML.
