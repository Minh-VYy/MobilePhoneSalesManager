<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes" encoding="utf-8"/>

  <xsl:template match="/">
    <div class="invoice-print-wrapper">
      <xsl:for-each select="DanhSachHoaDon/HoaDon | HoaDon">
        <div class="invoice-box">
          <table cellpadding="0" cellspacing="0" class="invoice-table-header">
            <tr>
              <td class="title">
                <h2>📱 CỬA HÀNG ĐIỆN THOẠI TECHSTORE</h2>
                <p>ĐC: 123 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh</p>
                <p>Hotline: 1900 6868 - Email: cskh@techstore.vn</p>
              </td>
              <td class="invoice-meta">
                <h3 class="inv-title">HÓA ĐƠN BÁN LẺ</h3>
                <p><strong>Mã HĐ:</strong> <span class="inv-code"><xsl:value-of select="MaHD"/></span></p>
                <p><strong>Ngày lập:</strong> <xsl:value-of select="NgayLap"/></p>
                <p><strong>Thu ngân:</strong> <xsl:value-of select="NhanVien"/></p>
              </td>
            </tr>
          </table>

          <div class="customer-section">
            <h4>THÔNG TIN KHÁCH HÀNG:</h4>
            <p><strong>Họ &amp; Tên:</strong> <xsl:value-of select="KhachHang/TenKH"/></p>
            <p><strong>Số điện thoại:</strong> <xsl:value-of select="KhachHang/SoDienThoai"/></p>
            <p><strong>Địa chỉ:</strong> <xsl:value-of select="KhachHang/DiaChi"/></p>
          </div>

          <table class="invoice-items">
            <thead>
              <tr class="heading">
                <th>STT</th>
                <th>Mã SP</th>
                <th>Tên sản phẩm</th>
                <th>Số lượng</th>
                <th>Đơn giá (VNĐ)</th>
                <th>Thành tiền (VNĐ)</th>
              </tr>
            </thead>
            <tbody>
              <xsl:for-each select="ChiTietHoaDon/ChiTiet">
                <tr class="item">
                  <td><xsl:value-of select="position()"/></td>
                  <td><xsl:value-of select="MaSP"/></td>
                  <td><xsl:value-of select="TenSP"/></td>
                  <td><xsl:value-of select="SoLuong"/></td>
                  <td><xsl:value-of select="format-number(DonGia, '#,##0')"/></td>
                  <td><xsl:value-of select="format-number(ThanhTien, '#,##0')"/></td>
                </tr>
              </xsl:for-each>
            </tbody>
          </table>

          <div class="invoice-summary">
            <div class="total-row">
              <strong>TỔNG CỘNG THÀNH TIỀN: </strong>
              <span class="total-amount"><xsl:value-of select="format-number(TongTien, '#,##0')"/> VNĐ</span>
            </div>
            <xsl:if test="GhiChu != ''">
              <div class="note-row">
                <em>Ghi chú: <xsl:value-of select="GhiChu"/></em>
              </div>
            </xsl:if>
          </div>

          <div class="invoice-footer-signatures">
            <div class="sig-box">
              <p><strong>Khách hàng</strong></p>
              <span>(Ký, ghi rõ họ tên)</span>
            </div>
            <div class="sig-box">
              <p><strong>Người lập hóa đơn</strong></p>
              <span>(Ký, ghi rõ họ tên)</span>
            </div>
          </div>
        </div>
      </xsl:for-each>
    </div>
  </xsl:template>
</xsl:stylesheet>
