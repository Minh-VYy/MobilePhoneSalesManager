<p align="center">
  <img src="wwwroot/images/logo.png" width="96" alt="Logo hệ thống quản lý bán điện thoại" />
</p>

# Quản lý bán điện thoại di động

Đồ án học phần **Công nghệ XML**, xây dựng website tham khảo giá, mua điện thoại trực tuyến và quản lý bán hàng tại cửa hàng. Hệ thống dùng **ASP.NET Core MVC (.NET 9)**, lưu dữ liệu bằng **XML**, kiểm tra cấu trúc bằng **XSD**, truy vấn với **XPath / LINQ to XML** và kết xuất danh mục, hóa đơn bằng **XSLT**.

Khách hàng và quản trị viên có giao diện, quyền truy cập và quy trình thao tác riêng. Bán online và bán tại quầy cùng cập nhật tồn kho; IMEI/serial giúp theo dõi từng máy; biểu đồ tổng hợp doanh thu từ giao dịch đã hoàn tất.

## Mục lục

- [1. Tổng quan](#1-tổng-quan)
- [2. Công nghệ và kiến trúc](#2-công-nghệ-và-kiến-trúc)
- [3. Chức năng](#3-chức-năng)
- [4. Cài đặt và khởi chạy](#4-cài-đặt-và-khởi-chạy)
- [5. Tài khoản và phân quyền](#5-tài-khoản-và-phân-quyền)
- [6. Quy trình sử dụng](#6-quy-trình-sử-dụng)
- [7. Quy tắc thống kê doanh thu](#7-quy-tắc-thống-kê-doanh-thu)
- [8. Dữ liệu XML và lược đồ XSD](#8-dữ-liệu-xml-và-lược-đồ-xsd)
- [9. Cấu trúc mã nguồn](#9-cấu-trúc-mã-nguồn)
- [10. Các đường dẫn chính](#10-các-đường-dẫn-chính)
- [11. Triển khai và sao lưu](#11-triển-khai-và-sao-lưu)
- [12. Xử lý sự cố thường gặp](#12-xử-lý-sự-cố-thường-gặp)
- [13. Phạm vi hiện tại và tài liệu liên quan](#13-phạm-vi-hiện-tại-và-tài-liệu-liên-quan)

## 1. Tổng quan

Hệ thống phục vụ ba nhu cầu:

- **Tham khảo trước khi đến cửa hàng:** xem dòng máy, phiên bản, giá, tình trạng, thông số và nhắn tư vấn.
- **Mua trực tuyến:** chọn đúng phiên bản, đặt hàng, theo dõi tiến trình và trao đổi với nhân viên.
- **Quản lý cửa hàng:** quản lý sản phẩm, tồn kho, đơn online, bán tại quầy, mã máy, khách hàng và doanh thu.

XML là nguồn lưu trữ chính của phiên bản này. **Không cần cài SQL Server, MySQL hoặc chạy migration cơ sở dữ liệu** để sử dụng dự án.

## 2. Công nghệ và kiến trúc

| Thành phần | Công nghệ / vai trò |
| --- | --- |
| Nền tảng | C#, ASP.NET Core MVC, `net9.0` |
| Giao diện | Razor Views, HTML, CSS, JavaScript; các thành phần jQuery có sẵn |
| Lưu trữ | XML trong thư mục `Data/`, thao tác bằng `XDocument`, `XElement` |
| Truy vấn | XPath và LINQ to XML |
| Kiểm tra dữ liệu | XSD, `XmlSchemaSet`, `XmlReaderSettings`, Data Annotations |
| Kết xuất | `XslCompiledTransform`, XSLT danh mục và hóa đơn |
| Xác thực | Cookie Authentication, vai trò `Admin` / `User`, ASP.NET Core `PasswordHasher` |
| Hỗ trợ trực tiếp | Server-Sent Events (SSE), lưu lịch sử tin nhắn trong XML |
| Thống kê | Chart.js 4.4.4, bảng số liệu HTML đi kèm |
| Trạng thái giỏ hàng | `localStorage` trên trình duyệt; giá và tồn được kiểm tra lại trên máy chủ |

~~~mermaid
flowchart LR
    Browser["Trình duyệt: khách / quản trị"] --> MVC["Controllers + xác thực và phân quyền"]
    MVC --> Services["Services xử lý nghiệp vụ"]
    Services --> XML["Data: các tệp XML"]
    Services --> XSD["XSD: kiểm tra cấu trúc"]
    Services --> XSLT["XSLT: kết xuất HTML"]
    MVC --> Views["Razor Views + JavaScript"]
    Views --> Browser
~~~

Các yêu cầu thay đổi dữ liệu được kiểm tra tại máy chủ. Việc cập nhật hóa đơn hoặc trạng thái đơn hàng cùng tồn kho và sổ mã máy sử dụng khóa chung và nhật ký `CommerceTransaction.xml` để hỗ trợ phục hồi sau khi ghi bị gián đoạn.

Giao diện quản trị dùng nền sáng mặc định, font Inter và màu chính theo token của hệ thống. Giao diện cửa hàng dùng lại hiệu ứng kính, bố cục responsive và các nút dùng chung; lựa chọn phiên bản được xử lý tại trang, không điều hướng lại toàn bộ trang.

## 3. Chức năng

### Khách xem hàng và người mua

| Nhóm | Chức năng |
| --- | --- |
| Danh mục | Xem, tìm kiếm, lọc hãng / nhóm iPhone, Android / khoảng giá; danh sách gộp các phiên bản cùng dòng máy |
| Chi tiết | Hình ảnh, mô tả, thông số, đánh giá, sản phẩm liên quan |
| Chọn phiên bản | Tình trạng mới / đã dùng, dung lượng, màu và bảo hành theo các phiên bản thực có trong dữ liệu |
| Tồn kho | Lựa chọn không còn hàng bị vô hiệu hóa và hiển thị mờ hơn |
| Giỏ hàng | Thêm, đổi số lượng, xóa, mua ngay; đối chiếu giá và tồn trước khi đặt |
| Đơn mua | Đặt hàng COD, xem lịch sử / chi tiết và hủy đơn đang chờ xác nhận |
| Tài khoản | Đăng ký, đăng nhập bằng tên đăng nhập hoặc email, cập nhật hồ sơ, đổi mật khẩu, đăng xuất |
| Tư vấn | Chat trực tiếp với nhân viên; có thể đưa thông tin sản phẩm đang chọn vào nội dung tư vấn |
| Nội dung | Banner tự chuyển theo nội dung, ảnh và thời gian cấu hình; mục “Điểm nổi bật” dẫn đến tin công nghệ |

Không phải mọi tổ hợp màu, dung lượng và bảo hành đều có thể mua: lựa chọn phải tương ứng với một mã phiên bản có thật và còn hàng.

### Quản trị viên

| Nhóm | Chức năng |
| --- | --- |
| Tổng quan | Chỉ số kinh doanh, 4 biểu đồ doanh thu, bảng đối chiếu, đơn cần xử lý và tin chưa đọc |
| Sản phẩm | Thêm / sửa / xóa, tìm và lọc kho, bảng / thẻ, xem nhanh, kiểm tra XSD |
| Phiên bản | Thiết lập dòng máy, tình trạng, màu, dung lượng, bảo hành, giá niêm yết và thông số mở rộng |
| Đơn online | Xác nhận, chuyển giao, xác nhận đã giao, hủy theo trạng thái; cập nhật tồn và mã máy tương ứng |
| Bán tại quầy | Chọn sản phẩm, quét IMEI/serial, xác nhận thanh toán, lập và in hóa đơn XSLT |
| Mã máy | Đăng ký mã chính / mã phụ, tra cứu còn kho / đang giữ / đã bán và mở giao dịch liên quan |
| Khách hàng | Xem tài khoản khách, liên hệ, lịch sử đơn và hội thoại có sẵn |
| Hỗ trợ | Hộp thư khách hàng, số tin chưa đọc, trả lời trực tiếp |
| Quảng cáo | Thêm / sửa / xóa, sắp xếp, bật / ẩn banner, chỉnh thời gian chuyển và xem trước |
| Dữ liệu | Xem dung lượng, số bản ghi, thời điểm cập nhật, kết quả XSD; tải XML trong danh sách được phép |

## 4. Cài đặt và khởi chạy

### Yêu cầu

- **.NET 9 SDK** để biên dịch dự án.
- Git nếu lấy mã nguồn từ repository.
- Trình duyệt có bật JavaScript và cho phép lưu trữ cục bộ.
- Quyền đọc / ghi thư mục `Data/` của ứng dụng.
- Tùy chọn: Visual Studio hỗ trợ .NET 9, hoặc VS Code có công cụ C#.
- Tùy chọn: máy quét mã dạng bàn phím, có ký tự kết thúc **Enter**, để bán tại quầy.

Dự án hiện không có bước `npm install` hoặc build frontend riêng. Một số font, icon, thư viện biểu đồ và ảnh dùng nguồn bên ngoài; cần truy cập mạng để tải những tài nguyên đó.

### Lấy mã nguồn

~~~powershell
git clone https://github.com/Minh-VYy/MobilePhoneSalesManager.git
cd MobilePhoneSalesManager
~~~

Nếu đã có mã nguồn trên máy, mở terminal tại thư mục chứa `QuanLyBanDienThoai.csproj`. Ví dụ với workspace hiện tại:

~~~powershell
cd E:/CongNgheXML/QuanLyBanDienThoai
~~~

### Khôi phục và chạy

~~~powershell
dotnet restore
dotnet run --launch-profile http
~~~

Mở **http://localhost:5193**. Địa chỉ thực tế được ghi trong dòng `Now listening on` của terminal; cấu hình các profile nằm tại [Properties/launchSettings.json](Properties/launchSettings.json).

Dừng ứng dụng bằng **Ctrl+C**. Sau khi cập nhật mã nguồn C#, khởi động lại ứng dụng để nạp bản mới.

### Chạy bằng HTTPS

Với máy phát triển Windows, nếu chưa tin cậy chứng chỉ phát triển:

~~~powershell
dotnet dev-certs https --trust
dotnet run --launch-profile https
~~~

Profile này dùng **https://localhost:7059** và **http://localhost:5193**. Trong Visual Studio, mở `QuanLyBanDienThoai.csproj`, chọn profile phù hợp rồi nhấn **F5**.

## 5. Tài khoản và phân quyền

### Tạo quản trị viên đầu tiên

Hệ thống **không cấp tài khoản hoặc mật khẩu demo**.

1. Khởi chạy ứng dụng.
2. Khi chưa có quản trị viên, hệ thống tạo khóa ngẫu nhiên tại `Data/AdminSetup.key`.
3. Trên máy chủ, đọc khóa từ tệp này. Nếu ứng dụng đang chạy trong terminal thứ nhất, dùng terminal thứ hai tại thư mục dự án:

   ~~~powershell
   Get-Content ./Data/AdminSetup.key
   ~~~

4. Mở `/Account/Setup`, nhập khóa và tự đặt thông tin đăng nhập.
5. Khi tạo thành công, khóa bị xóa; trang thiết lập không còn khả dụng và trả về 404.
6. Đăng nhập tại `/Account/Login`; tài khoản Admin được chuyển đến trang tổng quan.

Khóa chỉ dùng cho lần thiết lập đầu tiên, không phải mật khẩu đăng nhập. Không đưa khóa này vào README, ảnh chụp hoặc repository.

### Tài khoản người mua

Khách đăng ký tại `/Account/Register`, sau đó đăng nhập bằng tên đăng nhập hoặc email đã đăng ký. Mật khẩu dài **10–128 ký tự**; tên đăng nhập dài **3–32 ký tự**, gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.

| Đối tượng | Quyền truy cập |
| --- | --- |
| Chưa đăng nhập | Xem cửa hàng, sản phẩm, tin tức và chuẩn bị giỏ hàng |
| `User` | Đặt hàng, đánh giá, chat và quản lý thông tin / đơn của chính mình |
| `Admin` | Quản lý kho, đơn mua, khách hàng, POS, mã máy, dữ liệu và thống kê |

Vai trò do máy chủ quyết định. Mở giao diện cửa hàng không đổi vai trò Admin thành User; Admin không dùng thao tác đặt đơn của người mua. Để sử dụng cả hai vai trò đồng thời, có thể mở hai hồ sơ trình duyệt riêng.

### Cơ chế xác thực

- Mật khẩu được băm bằng ASP.NET Core `PasswordHasher`; không lưu mật khẩu gốc.
- Cookie đăng nhập không được lưu lâu dài, có thời hạn 30 phút và gia hạn khi sử dụng.
- Đổi mật khẩu thu hồi các phiên cũ và yêu cầu đăng nhập lại.
- Đăng xuất dùng POST; các thao tác ghi qua form có kiểm tra CSRF.
- Có giới hạn tần suất xác thực / gửi chat; nhập sai mật khẩu 5 lần khiến tài khoản tạm khóa 15 phút.
- Khách chỉ đọc đơn và hội thoại của mình; các chức năng quản trị được kiểm tra quyền tại controller.

## 6. Quy trình sử dụng

### Mua trực tuyến

1. Tìm sản phẩm từ thanh tìm kiếm hoặc danh mục.
2. Mở chi tiết, chọn phiên bản còn hàng, thêm giỏ hoặc mua ngay.
3. Đăng nhập tài khoản khách, kiểm tra giỏ và điền thông tin nhận hàng.
4. Đặt đơn. Máy chủ lấy lại giá / tồn, kiểm tra số lượng và lưu đơn vào `BuyerOrders.xml`.
5. Theo dõi tại `/Orders`; có thể hủy khi đơn còn **Chờ xác nhận**.
6. Nhân viên xử lý đơn theo luồng:

~~~text
Chờ xác nhận → Đã xác nhận → Đang giao hàng → Đã giao hàng
      └→ Đã hủy       └→ Đã hủy
~~~

| Mốc | Tồn kho và mã máy | Doanh thu |
| --- | --- | --- |
| Tạo đơn chờ | Chưa trừ tồn, chưa giữ mã máy | Chưa ghi nhận |
| Xác nhận đơn | Trừ tồn; giữ các mã đã đăng ký được phân bổ cho đơn | Chưa ghi nhận |
| Đang giao | Giữ nguyên phần tồn / mã đã phân bổ | Chưa ghi nhận |
| Đã giao | Các mã đã phân bổ chuyển sang đã bán | Ghi nhận theo ngày giao thành công |
| Hủy đơn đã xác nhận | Hoàn tồn và giải phóng mã đã giữ | Không ghi nhận |

Người mua chỉ tự hủy đơn chờ. Nhân viên có thể hủy đơn đã xác nhận trước khi chuyển giao. Đơn đang giao không có bước hủy trong luồng hiện tại.

### Bán tại cửa hàng bằng mã máy

1. Trong **Mã máy & IMEI**, đăng ký IMEI/serial thật cho đúng phiên bản đang có tồn.
2. Nếu máy có hai IMEI, gắn IMEI thứ hai làm mã phụ của cùng máy. Đăng ký mã không làm tăng tồn.
3. Mở **Bán hàng tại quầy** tại `/HoaDon/Create`.
4. Đặt con trỏ vào ô quét, quét mã và kết thúc bằng Enter; có thể nhập mã thủ công.
5. Hệ thống nhận diện máy / phiên bản, kiểm tra trạng thái và thêm vào hóa đơn. Mã đã bán, đang giữ cho online hoặc bị quét trùng đều bị chặn.
6. Nhập thông tin khách, kiểm tra số lượng, chọn tiền mặt / chuyển khoản, xác nhận đã nhận đủ thanh toán.
7. Bấm **Xác nhận bán & xuất hóa đơn**. Máy chủ kiểm tra lại toàn bộ mã, giá và tồn; lưu hóa đơn, trừ tồn và đánh dấu máy đã bán.
8. Xem / in hóa đơn, hoặc dùng hộp thoại in của trình duyệt để lưu PDF.

Các mã IMEI/serial phải đủ theo số lượng bán. Mã vạch chỉ nhận diện chung một mẫu sản phẩm không thay thế được mã duy nhất của từng máy. Quét và tra cứu mã **không tự tạo doanh thu**.

Xem quy trình chi tiết và giới hạn với máy tồn cũ tại [BARCODE_POS.md](BARCODE_POS.md).

### Quản lý sản phẩm và banner

- Thêm phiên bản trong **Quản lý sản phẩm**; nhập giá và tồn thực tế.
- Từ màn hình sửa, mở **Thiết lập phiên bản**. Đặt cùng `ModelSeries` cho các phiên bản thuộc cùng dòng máy để gộp trong danh mục.
- Điều chỉnh điều kiện, màu, dung lượng, bảo hành và thông số cho đúng SKU. Giá bán / tồn vận hành lấy từ `DienThoai.xml`.
- Với banner, vào **Quảng cáo**, cấu hình nội dung, đường dẫn ảnh, thứ tự, thời gian chuyển 3–30 giây và trạng thái hiển thị.

## 7. Quy tắc thống kê doanh thu

Tại **Quản trị → Tổng quan kinh doanh** (`/ThongKe`), chọn kỳ **7 / 30 / 90 ngày**. Kỳ bao gồm hôm nay, tính theo giờ Việt Nam **UTC+7**.

| Thành phần | Cách tính |
| --- | --- |
| Doanh thu tại quầy | Tổng số lượng × đơn giá đã lưu trong các dòng hóa đơn thuộc kỳ, theo ngày lập |
| Doanh thu online | Tổng tiền sản phẩm của các đơn đã giao thuộc kỳ, theo ngày giao trong lịch sử |
| Tổng doanh thu | Doanh thu tại quầy + doanh thu online đã giao |
| Giao dịch hoàn tất | Số hóa đơn tại quầy + số đơn online đã giao trong kỳ |
| Số máy đã bán | Tổng số lượng sản phẩm của các giao dịch trên |
| Giá trị trung bình | Tổng doanh thu / số giao dịch hoàn tất; bằng 0 khi chưa có giao dịch |

Bốn biểu đồ gồm:

1. **Doanh thu theo thời gian:** hai kênh riêng, có thể chuyển từng ngày / nhóm 7 ngày ngay trên trang.
2. **Kênh bán hàng:** tỷ trọng doanh thu quầy / online, kèm số tiền và số giao dịch.
3. **Theo thương hiệu:** doanh thu từng hãng; khi nhiều hơn 6 hãng, các hãng ngoài 5 hãng đầu được gộp trên biểu đồ.
4. **Sản phẩm tạo doanh thu cao nhất:** 6 phiên bản dẫn đầu theo doanh thu, kèm bảng đầy đủ tất cả SKU đã bán.

Các quy tắc đối chiếu:

- Dùng giá tại thời điểm bán, không dùng giá hiện tại trong danh mục.
- Không cộng đơn chờ, đã xác nhận, đang giao hoặc đã hủy; không cộng lại sổ IMEI vào doanh thu.
- Tổng tiền hóa đơn lệch với chi tiết sẽ được cảnh báo; các biểu đồ thống nhất dùng tổng chi tiết.
- Đơn đã giao thiếu sự kiện giao trong lịch sử được cảnh báo và chưa phân vào kỳ.
- Tồn kho, tin chưa đọc và đơn cần xử lý là số hiện tại, được ghi rõ riêng với chỉ số theo kỳ.
- Doanh thu tiền sản phẩm chưa phản ánh lợi nhuận, giá vốn hoặc chi phí giao hàng.
- Khi không tải được Chart.js, bảng số liệu vẫn sử dụng được. Kỳ không phát sinh doanh thu hiển thị trạng thái trống.

## 8. Dữ liệu XML và lược đồ XSD

### Nguồn dữ liệu đang dùng

| Tệp trong `Data/` | Nội dung | Lược đồ |
| --- | --- | --- |
| `DienThoai.xml` | Mã phiên bản, hãng, giá bán, tồn, thông số cơ bản, ảnh và mô tả | `DienThoai.xsd` |
| `MobileStoreDatabase.xml` | Dòng máy, tình trạng, màu, dung lượng, bảo hành, gallery, thông số mở rộng | `MobileStoreDatabase.xsd` |
| `HoaDon.xml` | Hóa đơn tại quầy, chi tiết, mã máy, phương thức thanh toán, token chống gửi trùng | `HoaDon.xsd` |
| `TinTuc.xml` | Tin công nghệ và nội dung tham khảo thị trường | `TinTuc.xsd` |
| `QuangCao.xml` | Nội dung, ảnh, thứ tự và thời gian chạy banner | Chưa có XSD riêng |
| `Accounts.xml` | Tài khoản, vai trò, mật khẩu đã băm, trạng thái xác thực | Chưa có XSD riêng |
| `BuyerOrders.xml` | Đơn online theo tài khoản và lịch sử trạng thái | Chưa có XSD riêng |
| `SupportChats.xml` | Hội thoại, tin nhắn và mốc đã đọc | Chưa có XSD riêng |
| `StockUnits.xml` | Mã máy duy nhất, mã phụ, trạng thái và giao dịch liên quan | Chưa có XSD riêng |
| `ProductReviews.xml` | Đánh giá của khách, tạo khi có đánh giá đầu tiên | Chưa có XSD riêng |
| `CommerceTransaction.xml` | Nhật ký tạm để phục hồi cập nhật thương mại bị gián đoạn | Tệp nội bộ, không sửa thủ công |
| `AdminSetup.key` | Khóa thiết lập quản trị viên đầu tiên | Tệp khóa văn bản, không phải XML |

Các tệp nghiệp vụ phát sinh được tạo khi cần; không phải mọi tệp trong bảng đều tồn tại ngay sau khi clone.

`Orders.xml`, `Customers.xml`, `CustomerOrderRequests.xml`, `Vouchers.xml` và `AppConfig.xml` còn trong kho từ các luồng trước. Luồng đặt hàng / tài khoản đang sử dụng `BuyerOrders.xml` và `Accounts.xml`. Không tự chuyển hay gán đơn cũ cho tài khoản mới dựa trên số điện thoại; voucher chưa được tích hợp vào checkout hiện tại.

### Vai trò của XSD, XPath và XSLT

- `DienThoai.xsd` kiểm tra cấu trúc, mã sản phẩm, hãng, giá / tồn không âm và mã duy nhất. Mã hiện chấp nhận 3–64 ký tự in hoa / số / gạch ngang, bắt đầu bằng chữ; không chỉ giới hạn ở `SP001`.
- `MobileStoreDatabase.xsd` định nghĩa dữ liệu mở rộng, khóa danh mục và tham chiếu từ sản phẩm đến danh mục.
- `HoaDon.xsd` định nghĩa hóa đơn; các trường mã máy và phương thức thanh toán là tùy chọn để tương thích tệp cũ. Luồng POS mới vẫn yêu cầu mã máy và xác nhận thanh toán tại máy chủ.
- `XmlValidationService` thực hiện kiểm tra XML với XSD và trả về lỗi dòng / cột. Kiểm tra tồn kho, chống bán trùng và tính tổng tiền thuộc xử lý nghiệp vụ C#.
- `XSLT/DienThoaiCatalogue.xslt` kết xuất danh mục thành HTML; `XSLT/HoaDonIn.xslt` kết xuất mẫu hóa đơn để xem / in.

Một số truy vấn phục vụ học phần; bộ truy vấn nằm trong [Data/XPathQueries.xml](Data/XPathQueries.xml):

| Mục đích | XPath |
| --- | --- |
| Điện thoại Apple | `//DienThoai[MaHang='APPLE']` |
| Sản phẩm giá từ 25 triệu | `//DienThoai[GiaBan >= 25000000]` |
| Phiên bản tồn dưới 5 máy | `//DienThoai[SoLuongTon < 5]` |
| Sản phẩm dùng chip Snapdragon | `//DienThoai[contains(ThongSoKyThuat/Chip, 'Snapdragon')]` |
| Hóa đơn trên 30 triệu | `//HoaDon[TongTien > 30000000]` |
| Khách mua mã SP001 tại quầy | `//HoaDon[ChiTietHoaDon/ChiTiet/MaSP='SP001']/KhachHang` |

## 9. Cấu trúc mã nguồn

~~~text
QuanLyBanDienThoai/
├── Controllers/                  # Điều hướng, phân quyền, nhận / trả dữ liệu
│   ├── AccountController.cs
│   ├── DienThoaiController.cs
│   ├── OrdersController.cs
│   ├── HoaDonController.cs
│   ├── StockUnitsController.cs
│   ├── ThongKeController.cs
│   ├── SupportController.cs
│   ├── CustomersController.cs
│   ├── QuangCaoController.cs
│   ├── TinTucController.cs
│   └── AdminDataController.cs
├── Models/                       # Model nghiệp vụ, dữ liệu form và view model
├── Services/                     # Xác thực, XML, kho, đơn, POS, chat và XSLT
│   └── PrivateXmlStore.cs        # Khóa chung, ghi tệp và phục hồi giao dịch
├── Data/                         # XML, XSD và dữ liệu phát sinh
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml        # Khung quản trị
│   │   ├── _LayoutHome.cshtml    # Khung trang chủ
│   │   ├── _LayoutUser.cshtml    # Khung trang người mua
│   │   └── _SupportChat.cshtml   # Thành phần chat dùng chung
│   └── ...                       # View theo controller
├── wwwroot/
│   ├── css/
│   │   ├── admin-theme.css
│   │   ├── admin-workspace.css
│   │   ├── commerce.css
│   │   └── storefront-buttons.css
│   ├── js/                       # Giỏ hàng, phiên bản, POS, biểu đồ và chat
│   ├── images/                   # Logo và ảnh cục bộ
│   └── lib/                      # Thư viện frontend có sẵn
├── XSLT/                         # Stylesheet danh mục và hóa đơn
├── Phân tích thiết kế/           # Sơ đồ use case, activity, sequence, robustness
├── Properties/launchSettings.json
├── Program.cs                    # DI, cookie, phân quyền, rate limit, routing
├── QuanLyBanDienThoai.csproj
└── README.md
~~~

## 10. Các đường dẫn chính

Các đường dẫn dưới đây tính từ địa chỉ ứng dụng đang chạy. Thay `{id}` bằng mã bản ghi thật, ví dụ mã sản phẩm hoặc mã đơn.

| Đường dẫn | Quyền / mục đích |
| --- | --- |
| `/` | Trang chủ cửa hàng |
| `/DienThoai` | Danh sách / tìm sản phẩm |
| `/DienThoai?category=IPHONE` | Nhóm iPhone |
| `/DienThoai?category=ANDROID` | Nhóm Android |
| `/DienThoai/Details/{id}` | Chi tiết sản phẩm |
| `/DienThoai/Cart` | Giỏ hàng |
| `/TinTuc` | Tin công nghệ / Điểm nổi bật |
| `/Home/Policies` | Thông tin chính sách |
| `/Account/Register`, `/Account/Login` | Tạo tài khoản khách / đăng nhập |
| `/Account/Setup` | Thiết lập Admin khi chưa có Admin |
| `/Account/Profile`, `/Account/ChangePassword` | Tài khoản đã đăng nhập |
| `/Orders` | Lịch sử đơn của User |
| `/Orders/Details/{id}` | Chủ đơn hoặc Admin |
| `/Support` | Chat khách / hộp thư Admin, yêu cầu đăng nhập |
| `/ThongKe` | Admin: tổng quan và biểu đồ |
| `/DienThoai/Manage` | Admin: quản lý sản phẩm |
| `/DienThoai/VariantSettings/{id}` | Admin: thiết lập phiên bản |
| `/Orders/Manage` | Admin: quản lý đơn online |
| `/HoaDon`, `/HoaDon/Create` | Admin: hóa đơn / bán tại quầy |
| `/HoaDon/Print/{id}` | Admin: xem / in hóa đơn |
| `/StockUnits` | Admin: quản lý IMEI/serial |
| `/Customers`, `/Customers/Details/{id}` | Admin: danh sách / hồ sơ khách |
| `/QuangCao` | Admin: quản lý banner |
| `/AdminData` | Admin: trạng thái tệp XML, kiểm tra XSD và tải dữ liệu được phép |
| `/DienThoai/XsltView` | Admin: danh mục kết xuất bằng XSLT |
| `/DienThoai/ValidateXsd`, `/HoaDon/ValidateXsd` | Admin: kiểm tra XML với XSD |
| `/DienThoai/XmlApi` | Admin: XML danh mục |
| `/DienThoai/RssFeed` | Kênh RSS danh mục sản phẩm |

## 11. Triển khai và sao lưu

### Bản phát hành

Tại thư mục dự án:

~~~powershell
dotnet publish -c Release -o ../PhoneStore-publish
~~~

Sau khi publish, bảo đảm thư mục triển khai có:

- Bộ XML / XSD cần sử dụng trong `Data/`, đặc biệt danh mục và metadata phiên bản.
- Thư mục `XSLT/` với đủ hai stylesheet.
- `wwwroot/`, bao gồm các ảnh cục bộ được dữ liệu tham chiếu.

Project hiện chưa khai báo quy tắc publish riêng cho `Data/` và `XSLT/`. Cần kiểm tra đầu ra và sao chép các tệp cần thiết nếu chưa có; không ghi đè dữ liệu vận hành bằng dữ liệu trong bản mã nguồn khi cập nhật ứng dụng.

Chạy từ thư mục triển khai để đường dẫn dữ liệu tương đối được giải quyết đúng:

~~~powershell
cd ../PhoneStore-publish
dotnet QuanLyBanDienThoai.dll
~~~

Địa chỉ lắng nghe phụ thuộc môi trường host; `launchSettings.json` chỉ phục vụ chạy phát triển. Cấu hình HTTPS ở môi trường triển khai và cấp quyền ghi `Data/` cho đúng tài khoản chạy ứng dụng.

### Dữ liệu cần giữ

- Sao lưu bộ dữ liệu sản phẩm / phiên bản, hóa đơn, đơn online, tài khoản, chat, mã máy, đánh giá, quảng cáo và tin tức.
- Dừng ứng dụng trước khi sao lưu bằng cách sao chép tệp để tránh lấy các tệp ở những thời điểm giao dịch khác nhau.
- Nếu có `CommerceTransaction.xml` sau lần ghi bị gián đoạn, giữ cùng bản sao dữ liệu để cơ chế phục hồi có thể xử lý khi đọc lại.
- `Accounts.xml`, `BuyerOrders.xml`, `SupportChats.xml`, `StockUnits.xml`, `AdminSetup.key` và nhật ký giao dịch đã được liệt kê trong `.gitignore`. Các tệp nghiệp vụ khác, như hóa đơn hoặc đánh giá phát sinh, cũng cần được bảo vệ khi chia sẻ mã nguồn.
- Đặt dữ liệu riêng ngoài `wwwroot/`. Trang tải dữ liệu quản trị chỉ cho phép một số tệp danh mục / phiên bản / hóa đơn, không phải công cụ sao lưu toàn bộ hệ thống.

Phiên bản hiện tại phù hợp **một tiến trình ứng dụng trên một máy**, có thư mục dữ liệu được lưu bền vững. Khóa và thông báo SSE nằm trong tiến trình; muốn chạy nhiều instance cần cơ chế khóa, lưu trữ và truyền sự kiện dùng chung.

Nếu dùng reverse proxy, cho phép kết nối streaming và tắt buffering cho `/Support/Events` để chat cập nhật kịp thời.

## 12. Xử lý sự cố thường gặp

| Hiện tượng | Cách xử lý |
| --- | --- |
| Không nhận lệnh `dotnet` hoặc báo không hỗ trợ `net9.0` | Kiểm tra `dotnet --list-sdks`; cài SDK phù hợp với target của dự án |
| Cổng 5193 đang được dùng | Dừng phiên chạy cũ hoặc chạy `dotnet run --launch-profile http --urls http://localhost:5200` |
| Chứng chỉ HTTPS chưa tin cậy | Dùng profile HTTP khi phát triển, hoặc thiết lập chứng chỉ bằng lệnh ở mục cài đặt |
| `/Account/Setup` trả 404 | Trang đã đóng khi tồn tại Admin; đăng nhập bằng tài khoản đã tạo |
| Không thấy `AdminSetup.key` | Khởi động ứng dụng, kiểm tra đúng thư mục `Data/` của bản đang chạy và quyền ghi; tệp không còn sau khi đã tạo Admin |
| Không ghi được XML | Kiểm tra quyền ghi, tệp chỉ đọc và đường dẫn dữ liệu; tránh sửa XML đồng thời bằng công cụ ngoài ứng dụng |
| Chọn phiên bản nhưng thiếu tùy chọn | Kiểm tra SKU trong `DienThoai.xml`, metadata trong `MobileStoreDatabase.xml`, cùng `ModelSeries` và tồn thực tế |
| Máy quét không thêm sản phẩm | Đặt con trỏ vào ô quét; kiểm tra chế độ bàn phím, hậu tố Enter và mã máy đã đăng ký |
| Không chốt được POS | Quét đủ mã theo số lượng, kiểm tra tồn / trạng thái máy và xác nhận thanh toán |
| Không có doanh thu trong kỳ | Kiểm tra kỳ ngày; đơn online phải đã giao, còn POS phải có hóa đơn được lưu; quét mã riêng lẻ chưa tạo doanh thu |
| Biểu đồ không xuất hiện | Kiểm tra khả năng tải Chart.js từ CDN; các bảng số liệu vẫn có thể xem |
| Chat không cập nhật trực tiếp | Kiểm tra đăng nhập, mạng và cấu hình SSE / buffering trên proxy |
| Mất ảnh / lỗi XSLT sau publish | Kiểm tra đường dẫn ảnh và bảo đảm bộ `Data/`, `XSLT/`, `wwwroot/` được triển khai đầy đủ |

## 13. Phạm vi hiện tại và tài liệu liên quan

### Những phần chưa tích hợp

- Thanh toán online hiện là COD. Chuyển khoản tại quầy do nhân viên xác nhận, chưa có xác minh giao dịch ngân hàng tự động hoặc cổng thanh toán.
- Chưa có tích hợp hãng vận chuyển, gửi email / SMS hoặc khôi phục mật khẩu qua email.
- Quét mã phục vụ máy quét dạng bàn phím và nhập thủ công; chưa có luồng quét bằng camera.
- Tồn cũ chưa đăng ký IMEI vẫn là tồn theo số lượng. Không tự sinh mã máy hoặc suy đoán mã của giao dịch cũ; đơn online có thể chứa máy chưa có mã theo dõi.
- Chưa có quy trình hoàn hàng / hoàn tiền sau khi đã giao hoặc xuất hóa đơn; các bước này cần bổ sung nghiệp vụ riêng.
- Tin tức hiện có trang đọc / tìm kiếm; chưa có màn hình quản trị bài viết.
- Chưa có hệ quản trị SQL hoặc hạ tầng nhiều instance.

### Tình trạng kiểm tra

Bản cập nhật thống kê và mã máy gần nhất đã biên dịch .NET thành công với **0 lỗi, 0 cảnh báo**; cú pháp JavaScript, CSS, XSLT và XSD đã được kiểm tra. Đây không phải xác nhận đã kiểm thử toàn bộ chức năng. Giao diện mới chưa được kiểm tra trực quan trong trình duyệt và luồng POS chưa thử với máy quét vật lý trong phiên triển khai này.

### Tài liệu

| Tài liệu | Nội dung |
| --- | --- |
| [BUYER_FEATURES.md](BUYER_FEATURES.md) | Tài khoản, người mua, đơn online và chat |
| [ADMIN_WORKSPACE.md](ADMIN_WORKSPACE.md) | Chức năng quản trị, thành phần dùng chung và cách đọc thống kê |
| [BARCODE_POS.md](BARCODE_POS.md) | IMEI/serial, máy quét, bán tại quầy và giới hạn tồn cũ |
| [UI_Design_Requirements.md](UI_Design_Requirements.md) | Nguyên tắc giao diện, font, màu, bố cục và responsive |
| [Sơ đồ phân tích thiết kế](<Phân tích thiết kế/UseCaseDiagram/UsecaseDiagram.svg>) | Use case; các sơ đồ activity, sequence và robustness nằm cùng thư mục |

**Học phần:** Công nghệ XML. Kho mã nguồn chưa có giấy phép mã nguồn riêng; quyền sử dụng logo, hình ảnh, font và các thư viện phụ thuộc theo nguồn tương ứng.
