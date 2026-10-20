# Giao diện quản trị

## Quy tắc cần giữ

- Áp dụng `UI_Design_Requirements.md` và các skill `ui-styling`, `ui-ux-pro-max`, `design-system` trong `.agents/skills`.
- Inter, nền sáng mặc định, màu chính `#2F6FED`, bo góc 8–16px, khoảng cách và trạng thái theo token của `admin-theme.css`. `admin-workspace.css` nối các thành phần tài khoản/đơn hàng/chat vào cùng bảng màu.
- Hiệu ứng nhẹ phục vụ phản hồi thao tác; lọc và chuyển dạng danh sách tức thì. Không thêm hiệu ứng tải trang, số liệu giả, biểu đồ minh họa hoặc nút chỉ hiện thông báo thành công.
- Trên di động: menu đóng/mở được bằng bàn phím, vùng phía sau bị vô hiệu khi mở; kho chuyển thành thẻ. Bảng số liệu cuộn ngang trong khung riêng.
- Mỗi biểu mẫu có nhãn rõ ràng, trạng thái lỗi; thao tác ghi có chống CSRF. Quyền Admin phải được kiểm tra trên máy chủ.

## Nguồn số liệu

- Doanh số POS: `HoaDon.xml`, theo ngày lập hóa đơn (ngày giờ tại Việt Nam). Dùng tổng số lượng × đơn giá đã lưu trong chi tiết, đồng nhất với các biểu đồ phân tích; cảnh báo nếu tổng hóa đơn đã lưu bị lệch.
- Doanh số trực tuyến: `BuyerOrders.xml`, chỉ trạng thái đã giao; ngày lấy từ lịch sử giao thành công. Không gộp đơn chờ hoặc đã hủy.
- Tồn kho và giá trị tồn theo **giá bán**, không phải giá vốn: `DienThoai.xml`.
- Khách đăng ký: tài khoản có vai trò User trong `Accounts.xml`.
- Đơn cần xử lý và tin chưa đọc: dữ liệu hiện tại, không giới hạn theo kỳ doanh số.
- Khi Chart.js không tải được, các bảng số liệu tự mở và vẫn dùng được.

## Biểu đồ và chỉ số tại `/ThongKe`

- Kỳ 7/30/90 ngày bao gồm hôm nay, cùng múi giờ UTC+7. Mỗi kỳ đọc chung một snapshot hóa đơn, đơn mua và kho dưới khóa giao dịch hiện có.
- Bốn chỉ số: doanh thu tiền sản phẩm, số giao dịch hoàn tất (hóa đơn POS + đơn online đã giao), số máy đã bán, giá trị trung bình mỗi giao dịch. Không hiển thị tỷ lệ tăng trưởng giả hoặc suy ra lợi nhuận từ giá bán.
- Biểu đồ thời gian giữ đủ ngày không bán (giá trị 0); hai đường dùng nét liền/nét đứt. Chuyển từng ngày/nhóm 7 ngày trên trình duyệt không tải lại trang. Nhóm 7 ngày tính từ đầu kỳ; nhóm cuối có thể ngắn hơn, tooltip ghi đầy đủ khoảng ngày. Kỳ 90 ngày mặc định gộp 7 ngày.
- Biểu đồ vòng thể hiện tỷ trọng doanh thu tại quầy/online, kèm bảng luôn hiển thị số tiền, số giao dịch và tỷ trọng. Phương thức thanh toán chỉ thống kê POS; hóa đơn cũ chưa có trường phương thức thuộc nhóm “Chưa ghi phương thức”, không suy đoán từ ghi chú.
- Biểu đồ thanh ngang theo hãng: tối đa 6 mục; nếu vượt, gộp các hãng ngoài 5 hãng đầu vào “Các hãng còn lại”. Bảng bên dưới vẫn có đầy đủ hãng. Hãng căn cứ danh mục hiện tại; mã sản phẩm đã xóa vẫn giữ doanh thu trong “Không phân loại”.
- Biểu đồ sản phẩm thể hiện 6 SKU doanh thu cao nhất. Bảng có tất cả SKU đã bán, tên từ giao dịch đã lưu, số máy và số tiền. Tổng bảng ngày/kênh/hãng/sản phẩm cùng nguồn chi tiết giao dịch; giá sản phẩm hiện tại không thay đổi số tiền lịch sử.
- Đơn online đã giao nhưng thiếu sự kiện giao trong lịch sử được cảnh báo và chưa xếp vào kỳ nào. Đơn chờ/đã xác nhận/đang giao/đã hủy và sổ IMEI không được cộng lại vào doanh thu.
- Không vẽ biểu đồ tỷ trọng khi tổng bằng 0; dùng thông báo trống rõ ràng. Bảng luôn truy cập được bằng bàn phím, tooltip hiển thị số tiền đầy đủ bằng đồng Việt Nam; trục dùng số rút gọn. Biểu đồ tắt animation, dùng token màu hiện có và cập nhật theo theme.

## Thành phần dùng chung

- `_Layout`: khung, điều hướng, thông báo, đăng xuất.
- `_ProductForm`: thêm và sửa sản phẩm.
- `_AdminPagination` và `admin-workspace.js`: lọc/phân trang đơn trực tuyến và hóa đơn POS.
- `_SupportChat`: chat khách hàng và hộp thư admin.

## Các màn hình quản trị mở rộng

- `/Customers`: tìm kiếm, phân trang danh sách khách mua. Hồ sơ có liên hệ, ngày đăng ký, số đơn đang xử lý, giá trị đơn đã giao và lịch sử mua; mở hội thoại hiện có theo đúng khách. Không đưa dữ liệu mật khẩu vào model giao diện.
- `/DienThoai/VariantSettings/{id}`: cấu hình dòng máy, tình trạng, màu, dung lượng, bảo hành, giá niêm yết và thông số mở rộng trong `MobileStoreDatabase.xml`; kiểm tra XSD trước khi ghi. Từ màn hình sửa sản phẩm, chọn “Mở thiết lập phiên bản”. Sản phẩm cùng tên dòng tiếp tục được gộp trên cửa hàng.
- `/HoaDon/Create`: POS có tìm theo mã/tên/cấu hình, lọc hãng, số lượng theo tồn kho, thông tin khách và nhân viên lấy từ tài khoản đăng nhập. Hóa đơn và tồn kho dùng nhật ký ghi chung; mã checkout chống gửi trùng. XSD hóa đơn chấp nhận mã từ HD001 trở lên và trường token tùy chọn để tương thích dữ liệu cũ.
- `/HoaDon/Print/{id}`: XSLT đọc đúng cấu trúc `DanhSachHoaDon/HoaDon`, hiển thị bảng tiền và thông tin thực tế, CSS giới hạn trong hóa đơn; in ẩn điều hướng quản trị.
- `/StockUnits`: đăng ký/tra cứu IMEI hoặc serial duy nhất theo máy, mã phụ cho cùng máy, trạng thái còn kho/giữ cho online/đã bán và giao dịch liên quan. POS yêu cầu đủ mã và xác nhận thanh toán; hóa đơn, tồn kho và sổ máy dùng cùng nhật ký. Hướng dẫn tại `BARCODE_POS.md`.
- `/Orders/Details/{id}`: phần admin riêng, có bảng từng phiên bản, tổng tiền, nhận hàng, ghi chú, tiến trình và hành động tiếp theo. Có liên kết sang hồ sơ khách và hội thoại đã tồn tại.
- `/QuangCao`: danh sách gọn, tìm/lọc/phân trang, bật/ẩn, xóa; màn hình thêm/sửa riêng với xem trước và giữ dữ liệu khi validation thất bại.
- `/AdminData`: số bản ghi, dung lượng, thời điểm cập nhật, kết quả XSD; tải XML sản phẩm, phiên bản hoặc hóa đơn qua danh sách tệp được phép; xem đầy đủ lỗi theo dòng/cột. Các đường dẫn dữ liệu tài khoản/chat không được cung cấp tại đây.

Các trang mới kiểm tra quyền Admin trên máy chủ. Giao dịch POS và đơn trực tuyến dùng cùng khóa để tránh bán vượt tồn khi thao tác đồng thời trong một tiến trình ứng dụng.

Build .NET đã thành công. Chưa có kiểm tra trực quan bằng trình duyệt cho bản admin này do phiên làm việc không cung cấp browser khả dụng.
