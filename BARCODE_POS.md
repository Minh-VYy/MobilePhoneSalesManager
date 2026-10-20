# Tra cứu giá trên web và bán tại cửa hàng bằng mã máy

## Quy trình sử dụng

1. Khách xem giá, phiên bản và tình trạng trên web; đặt online hoặc liên hệ để đến cửa hàng xem máy.
2. Quản trị mở **Mã máy & IMEI**, chọn đúng phiên bản và đăng ký IMEI/serial thật của máy đang trong kho. Có thể gắn thêm IMEI thứ hai cho cùng một máy.
3. Đăng ký mã không tăng số lượng tồn. Số mã đang có trạng thái còn kho không vượt quá tồn kho khả dụng của phiên bản. Nếu đăng ký sai, chỉ gỡ được mã chưa bán/chưa giữ cho đơn rồi đăng ký lại.
4. Mở **Bán hàng tại quầy**, đặt con trỏ vào ô quét IMEI/serial. Máy quét USB/Bluetooth dạng bàn phím nhập chuỗi mã và kết thúc bằng Enter; cũng có thể nhập mã thủ công.
5. Mỗi mã hợp lệ nhận diện một máy. Quét mã phụ vẫn nhận diện máy chính. Các mã trùng trong hóa đơn, đã bán, đang giữ cho đơn online, chưa đăng ký hoặc không đủ tồn đều bị chặn.
6. Kiểm tra sản phẩm và số lượng. Nếu chọn phiên bản bằng nút trên danh sách, cần quét đủ mã máy cho số lượng đã chọn.
7. Nhập thông tin khách, chọn tiền mặt/chuyển khoản và xác nhận đã nhận đủ thanh toán. Bấm **Xác nhận bán & xuất hóa đơn**.
8. Máy chủ kiểm tra lại giá, tồn kho và trạng thái từng mã. Hóa đơn, lượng tồn và trạng thái máy được ghi cùng nhật ký giao dịch; gửi lại cùng mã checkout không tạo hóa đơn trùng.
9. Hóa đơn có IMEI/serial, số lượng, giá và hình thức thanh toán. Có thể tìm lại hóa đơn bằng IMEI trong danh sách hóa đơn hoặc mở từ màn hình Mã máy & IMEI.

## Trạng thái và liên hệ với đơn online

- **Còn trong kho:** đã đăng ký, chưa được bán hoặc giữ cho đơn.
- **Đang giữ cho đơn online:** khi xác nhận đơn online, các máy đã đăng ký của phiên bản được phân bổ trước. Màn hình chi tiết đơn admin hiển thị mã để nhân viên đóng gói đúng máy; các mã này không được bán tại POS.
- **Đã bán:** POS đã xuất hóa đơn sau xác nhận thanh toán hoặc đơn online đã giao thành công. Quét lại trả về trạng thái, giao dịch và thời điểm bán, không thêm vào hóa đơn mới.
- Hủy đơn online đã xác nhận sẽ hoàn tồn và giải phóng các mã máy đã giữ.

Đăng ký đủ mã máy thực tế khi bắt đầu áp dụng quy trình. Tồn cũ chưa gắn mã vẫn là tồn theo số lượng; hệ thống không tự tạo IMEI hoặc suy đoán mã máy cho hóa đơn/đơn cũ. Các đơn online có thể chứa máy tồn cũ chưa gắn mã; danh sách IMEI trong đơn chỉ phản ánh các mã đã được đăng ký và phân bổ.

## Ghi nhận doanh thu

- Quét mã và đăng ký mã là thao tác tra cứu/chuẩn bị, không ghi nhận doanh thu.
- POS ghi nhận sau khi nhân viên xác nhận thanh toán và lưu hóa đơn thành công.
- Thống kê hiện có tổng hợp hóa đơn POS theo ngày lập và đơn online đã giao theo ngày hoàn tất; không cộng lại dữ liệu sổ mã máy nên không đếm đôi doanh thu.
- `StockUnits.xml` lưu giá từng máy, thời điểm bán, kênh bán, giao dịch và nhân viên liên quan để đối chiếu.
- Checkbox thanh toán là xác nhận của nhân viên; không kết nối hay tự xác minh giao dịch ngân hàng.

## Lưu trữ và phạm vi

- `Data/StockUnits.xml` là dữ liệu riêng, ngoài webroot và đã được bỏ qua trong Git. Sao lưu cùng sản phẩm, hóa đơn, đơn online và nhật ký giao dịch.
- Quản lý/tra cứu mã máy yêu cầu quyền Admin trên máy chủ. Không cung cấp IMEI toàn kho cho khách vãng lai.
- Phiên bản triển khai dùng khóa chung trong một tiến trình ứng dụng như các luồng XML hiện có.
- Không yêu cầu thư viện quét camera. Máy quét cần ở chế độ nhập bàn phím với ký tự kết thúc Enter.
- Đã biên dịch và kiểm tra cú pháp; chưa thử với máy quét vật lý hoặc kiểm thử chức năng tự động cho phần mới trong phiên này.
