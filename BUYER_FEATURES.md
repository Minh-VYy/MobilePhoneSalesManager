# Tài khoản, đơn mua và hỗ trợ trực tiếp

## Khởi chạy và tài khoản đầu tiên

1. Chạy lại ứng dụng sau khi cập nhật source (`dotnet run`).
2. Khi chưa có admin, ứng dụng sinh mã ngẫu nhiên tại `Data/AdminSetup.key` trên máy chủ.
3. Mở `/Account/Setup`, nhập mã đó và tự đặt thông tin/tài khoản/mật khẩu quản trị viên. Mã bị xóa và trang thiết lập đóng khi tạo thành công.
4. Admin đăng nhập tại `/Account/Login`. Khách mua tạo tài khoản tại `/Account/Register`, sau đó đăng nhập bằng thông tin đã đăng ký.

Không có tài khoản hoặc mật khẩu demo. Mật khẩu tối thiểu 10 ký tự được băm bằng PasswordHasher của ASP.NET Core; không lưu mật khẩu gốc. Vai trò chỉ lấy từ dữ liệu tài khoản trên máy chủ. Session demo cũ không được dùng để xác thực. Cookie đăng nhập không được lưu lâu dài, có thời hạn 30 phút và được gia hạn khi sử dụng. Đổi mật khẩu vô hiệu hóa các phiên cũ.

## Chức năng người mua

- Tìm/chọn phiên bản, thêm giỏ hàng và mua ngay dùng lại luồng sản phẩm hiện có.
- Giỏ hàng cập nhật tên, giá, tùy chọn và tồn kho từ máy chủ trước khi cho đặt hàng. Máy chủ kiểm tra lại giá/tồn kho, không dùng giá từ trình duyệt.
- Đơn COD lưu theo tài khoản. Gửi lại cùng mã checkout không tạo đơn trùng; giỏ chỉ được xóa sau khi đặt hàng thành công.
- `/Orders/Index`: lịch sử mua hàng; `/Orders/Details/{id}`: sản phẩm, thông tin nhận hàng, tiền sản phẩm và lịch sử trạng thái. Người mua chỉ xem đơn của mình và chỉ hủy được đơn chờ xác nhận.
- `/Account/Profile`: thông tin liên hệ/địa chỉ nhận hàng; `/Account/ChangePassword`: đổi mật khẩu bằng mật khẩu hiện tại.
- Nhận xét sản phẩm yêu cầu đăng nhập khách hàng, tên lấy từ tài khoản.
- Các nút chính dùng chung `storefront-buttons.css` với class `store-action`: hình chữ nhật bo góc, nền tối trung tính và chấm sáng chuyển động nhẹ theo mẫu tham khảo floating-dots. Nút phụ, nút gọn trên header và nút xem tất cả có modifier riêng. CSS giới hạn trong `storefront-body`, hỗ trợ reduced-motion và giữ trạng thái disabled của sản phẩm hết hàng; không cần React/Three.js.
- Nút chat nổi và nút tư vấn trên sản phẩm mở cùng một giao diện chat. Tư vấn giữ nội dung lựa chọn sản phẩm và chỉ gửi khi khách bấm Gửi.

## Chức năng quản trị

- Các trang kho, CRUD sản phẩm, hóa đơn, thống kê, quảng cáo, XSLT và XML API được kiểm tra quyền Admin trên server.
- `/Orders/Manage`: xử lý đơn trực tuyến. Luồng: Chờ xác nhận → Đã xác nhận → Đang giao → Đã giao. Có thể hủy trước khi giao; xác nhận trừ tồn kho, hủy đơn đã xác nhận hoàn tồn kho.
- Cập nhật trạng thái và tồn kho dùng khóa chung cùng nhật ký XML để phục hồi nếu việc ghi tệp bị ngắt.
- `/Support/Index`: hộp thư hỗ trợ, số tin chưa đọc, lịch sử và trả lời khách hàng. Không có trả lời tự động giả lập.
- `/ThongKe/Index`: tổng quan theo 7/30/90 ngày; doanh số POS và đơn trực tuyến đã giao, đơn cần xử lý, tồn kho, tài khoản khách hàng và tin chưa đọc đều lấy từ XML thực tế.
- Kho có tìm kiếm không dấu, lọc hãng/tồn kho, sắp xếp giá, bảng/thẻ và hộp xem nhanh cấu hình. Đơn trực tuyến và hóa đơn POS có tìm kiếm, phân trang thực tế tại trình duyệt.
- Form thêm/sửa dùng chung partial, có nhãn trường, giới hạn giá/tồn kho không âm và phản hồi lỗi. Giao diện dùng lại token, hỗ trợ bàn phím và bố cục di động.
- Hồ sơ khách hàng có lịch sử đơn theo tài khoản và mở đúng hội thoại đã có. Chi tiết đơn admin tách riêng, trình bày từng phiên bản, giá, nhận hàng, ghi chú, lịch sử và thao tác trạng thái.
- Thiết lập phiên bản từ màn hình sửa sản phẩm: dòng máy để gộp danh mục, tình trạng, dung lượng, màu sắc, bảo hành, giá niêm yết và thông số mở rộng; dữ liệu XML kiểm tra XSD trước khi ghi.
- POS tìm/lọc sản phẩm, giới hạn số lượng theo tồn, giữ các dòng hợp lệ khi nhập sai; lấy nhân viên từ tài khoản và giá từ máy chủ. Ghi hóa đơn/tồn kho cùng nhật ký giao dịch, chống gửi trùng bằng token. Mẫu XSLT hóa đơn và danh mục có bố cục phù hợp để in.
- POS bổ sung quét IMEI/serial đã đăng ký, chặn bán trùng và yêu cầu quét đủ mã trước khi xác nhận thanh toán. Đơn online giữ/giải phóng các mã đã đăng ký cùng biến động tồn kho; quét mã không tạo doanh thu. Xem `BARCODE_POS.md` để cấu hình quy trình dùng máy quét dạng bàn phím.
- Banner có danh sách, tìm/lọc/phân trang, bật/ẩn, thêm/sửa với xem trước. Trang dữ liệu hệ thống cung cấp thông tin và kết quả kiểm tra XSD cho các tệp danh mục, phiên bản, hóa đơn; chỉ tải các tệp trong danh sách cho phép.
- Liên kết về cửa hàng giữ nguyên tài khoản admin; không đổi vai trò. Tài khoản admin không sử dụng thao tác đặt hàng của khách mua.

## Lưu trữ và triển khai

`Accounts.xml`, `BuyerOrders.xml`, `SupportChats.xml`, `AdminSetup.key` và nhật ký giao dịch nằm trong `Data`, ngoài webroot và đã được loại khỏi Git. Sao lưu dữ liệu này cùng dữ liệu sản phẩm; không đưa khóa khởi tạo lên kho mã nguồn. Các yêu cầu đặt hàng cũ trong `CustomerOrderRequests.xml` không tự gán cho tài khoản mới bằng số điện thoại.

Chat truyền cập nhật bằng Server-Sent Events, lưu lịch sử XML và tự kết nối lại. Khi khách rời trang, tin nhắn vẫn được lưu cho lần đăng nhập sau. Triển khai một tiến trình ứng dụng trên một máy với quyền ghi thư mục `Data`; nếu mở nhiều instance, cần thay khóa tệp và bộ truyền sự kiện bằng hạ tầng dùng chung. Khi đi qua proxy, cho phép streaming và không buffer đường dẫn `/Support/Events`.

Thanh toán hiện là COD, cửa hàng xác nhận chi phí giao hàng trước khi gửi. Chưa kết nối cổng thanh toán hoặc nhà cung cấp email; không có thao tác giả lập thanh toán/khôi phục mật khẩu bằng email.

## Kiểm tra đã thực hiện

- Build .NET 9: không có lỗi/cảnh báo.
- 68 kiểm tra HTTP cho luồng tài khoản/đơn mua/chat trên bộ XML tạm riêng trước đợt chỉnh giao diện admin: đăng ký/đăng nhập thật; chống nâng quyền qua form; chặn truy cập admin và dữ liệu của tài khoản khác; CSRF; đặt đơn và nhắn tin không trùng; giá từ server; luồng trạng thái/tồn kho; tranh chấp đơn cuối kho; phục hồi giao dịch XML; nhận tin trực tiếp qua SSE; tin chưa đọc; thu hồi phiên sau đổi mật khẩu; đăng xuất bằng POST.
- Kiểm tra cú pháp các script mới/sửa và parse CSS mới.
- Đợt mở rộng admin tiếp theo đã build và biên dịch XSLT/XSD; chưa chạy kiểm thử chức năng tự động mới cho đợt này.
- Chưa kiểm tra bằng ảnh chụp trình duyệt trong phiên triển khai này vì không có browser khả dụng.
