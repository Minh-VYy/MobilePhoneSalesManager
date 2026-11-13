# Mã giảm giá và quyền lợi mua hàng

## Cấu hình cho admin

1. Mở **Mã & thẻ giảm giá** (`/Vouchers`) → **Tạo thẻ giảm giá**.
2. Nhập mã duy nhất gồm 3–32 ký tự không dấu, tên chương trình; chọn giảm số tiền hoặc phần trăm. Phần trăm tối đa 100%; số tiền là đồng Việt Nam nguyên.
3. Đặt giá trị sản phẩm tối thiểu, trần giảm tùy chọn, ngày bắt đầu và kết thúc theo UTC+7, tổng lượt và số lượt mỗi tài khoản.
4. Chọn các SKU/phiên bản được áp dụng. Không chọn SKU nào nghĩa là tất cả sản phẩm. Điều kiện tối thiểu chỉ cộng các dòng thuộc phạm vi này.
5. Nhập thông báo và quyền lợi thực tế, mỗi quyền lợi một dòng. Bật nhận mã; bật **Công khai thẻ** nếu muốn hiển thị trên cửa hàng.
6. Mở **Banner quảng cáo** (`/QuangCao`), thêm/sửa banner và chọn thẻ ở trường **Thẻ giảm giá / quyền lợi mua hàng**. Banner hiển thị mức giảm, mã, hạn dùng và ba dòng quyền lợi đầu khi chương trình khả dụng; liên kết mở đầy đủ điều kiện.

Thẻ công khai còn hiệu lực và còn lượt xuất hiện tại `/Promotions` và trang chi tiết các SKU phù hợp. Khi đổi phiên bản, ưu đãi được lọc tức thời theo SKU đang chọn. Thẻ riêng vẫn áp được bằng mã nhưng không xuất hiện công khai. Quyền lợi là nội dung do cửa hàng cấu hình, không tự thay đổi bảo hành, phí vận chuyển, tồn quà hoặc sinh quà tặng trong đơn.

## Dành cho người mua

- Đăng nhập bằng tài khoản khách, thêm sản phẩm vào giỏ và nhập mã → **Áp dụng**. Nút trên thẻ chuyển tới giỏ kèm mã để kiểm tra.
- Tạm tính, tiền giảm và tiền sản phẩm cần thanh toán cập nhật tại chỗ; **Bỏ mã** tính lại không dùng ưu đãi.
- Mỗi **đơn online** dùng một mã. Luồng POS tại quầy chưa áp mã giảm giá này.
- Giá, tồn, ngày hiệu lực, phạm vi, đơn tối thiểu, trần và giới hạn lượt được kiểm tra trên máy chủ khi xem trước và khi tạo đơn. Xem trước chưa giữ lượt, nên mã có thể hết lượt trước lúc đặt.
- Chỉ đặt hàng sau khi nhận báo giá hợp lệ cho giỏ hiện tại; nhập mã mới cần áp dụng hoặc bỏ mã trước khi đặt.
- Số tiền hiển thị là tiền sản phẩm; phí giao hàng được cửa hàng xác nhận riêng theo quy trình hiện có.

## Dữ liệu và doanh thu

- `Data/Vouchers.xml` lưu định nghĩa; `Vouchers.xsd` kiểm tra cấu trúc trước mỗi lần lưu/tắt/bật. `/AdminData` cung cấp kiểm tra XSD và tải XML cho admin.
- `BuyerOrders.xml` lưu mã/tên chương trình cùng tiền giảm trên từng dòng sản phẩm. Sửa/tắt chương trình không tính lại đơn đã lưu.
- Tổng trước giảm = tổng đơn giá × số lượng. Tiền giảm được phân bổ theo tỷ trọng các dòng hợp lệ, làm tròn xuống đơn vị đồng và phân bổ phần dư để tổng chính xác. Tổng thanh toán = tổng trước giảm − tổng giảm.
- Lượt giữ/đã dùng được tính từ các đơn có mã và chưa hủy, gồm cả đơn chờ xác nhận. Hủy đơn trả lượt; đơn lưu lại vẫn giữ lịch sử giảm giá. Tạo đơn và kiểm tra lượt dùng chung khóa trong một tiến trình, token checkout chống tạo trùng.
- Doanh thu online, biểu đồ hãng/sản phẩm, hồ sơ khách và giá bán sổ IMEI lấy số tiền **sau giảm**, ghi nhận doanh thu khi giao thành công. Dữ liệu POS giữ nguyên quy trình hiện có.
- Tệp voucher cũ được đọc tương thích: thiếu giới hạn mặc định một lượt tổng và một lượt/tài khoản; thiếu phạm vi nghĩa là tất cả SKU; thiếu ngày bắt đầu nghĩa là không có mốc bắt đầu; thiếu công khai mặc định ẩn. Trạng thái `used` không nhận mã. Admin cần rà soát phạm vi/điều kiện thực tế trước khi bật hoặc công khai các mã cũ. Không tự thêm chương trình, quyền lợi hoặc sửa dữ liệu lịch sử.

Khóa tệp hiện áp dụng trong một tiến trình ứng dụng. Chạy nhiều instance cần kho giao dịch dùng chung để bảo đảm giới hạn lượt toàn hệ thống.
