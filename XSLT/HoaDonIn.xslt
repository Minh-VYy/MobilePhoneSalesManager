<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes" encoding="utf-8"/>
  <xsl:decimal-format name="vi" decimal-separator="," grouping-separator="."/>
  <xsl:template match="/">
    <xsl:for-each select="DanhSachHoaDon/HoaDon">
      <article class="admin-invoice">
        <header class="admin-invoice-header"><div><strong>PhoneStore</strong><p>Hóa đơn bán hàng tại quầy</p></div><div><b><xsl:value-of select="MaHD"/></b><p>Ngày lập: <xsl:value-of select="substring(NgayLap, 9, 2)"/>/<xsl:value-of select="substring(NgayLap, 6, 2)"/>/<xsl:value-of select="substring(NgayLap, 1, 4)"/> · <xsl:value-of select="substring(NgayLap, 12, 5)"/></p></div></header>
        <h2>HÓA ĐƠN BÁN HÀNG</h2>
        <dl class="admin-invoice-customer">
          <div><dt>Khách hàng</dt><dd><xsl:value-of select="KhachHang/TenKH"/></dd></div>
          <div><dt>Điện thoại</dt><dd><xsl:value-of select="KhachHang/SoDienThoai"/></dd></div>
          <xsl:if test="string-length(KhachHang/DiaChi) &gt; 0"><div><dt>Địa chỉ</dt><dd><xsl:value-of select="KhachHang/DiaChi"/></dd></div></xsl:if>
          <div><dt>Nhân viên</dt><dd><xsl:value-of select="NhanVien"/></dd></div>
          <div><dt>Thanh toán</dt><dd><xsl:choose><xsl:when test="PaymentMethod = 'Cash'">Tiền mặt</xsl:when><xsl:when test="PaymentMethod = 'BankTransfer'">Chuyển khoản</xsl:when><xsl:otherwise>Chưa ghi nhận phương thức</xsl:otherwise></xsl:choose></dd></div>
        </dl>
        <div class="admin-table-scroll"><table class="admin-invoice-table"><thead><tr><th>Sản phẩm</th><th class="num">Số lượng</th><th class="num">Đơn giá</th><th class="num">Thành tiền</th></tr></thead><tbody>
          <xsl:for-each select="ChiTietHoaDon/ChiTiet"><tr><td><b><xsl:value-of select="TenSP"/></b><small><xsl:value-of select="MaSP"/></small><xsl:if test="count(UnitCodes/Code) &gt; 0"><small class="admin-invoice-unit-codes">IMEI/serial: <xsl:for-each select="UnitCodes/Code"><xsl:value-of select="."/><xsl:if test="position() != last()"> · </xsl:if></xsl:for-each></small></xsl:if></td><td class="num"><xsl:value-of select="SoLuong"/></td><td class="num"><xsl:value-of select="format-number(DonGia, '#.##0', 'vi')"/> ₫</td><td class="num"><xsl:value-of select="format-number(ThanhTien, '#.##0', 'vi')"/> ₫</td></tr></xsl:for-each>
        </tbody></table></div>
        <div class="admin-invoice-total"><span>Tổng tiền sản phẩm</span><strong><xsl:value-of select="format-number(TongTien, '#.##0', 'vi')"/> ₫</strong></div>
        <xsl:if test="string-length(GhiChu) &gt; 0"><p class="admin-invoice-note"><b>Ghi chú: </b><xsl:value-of select="GhiChu"/></p></xsl:if>
        <footer class="admin-invoice-footer"><span>Nhân viên bán hàng</span><span>Khách hàng</span></footer>
        <p class="admin-invoice-thanks">Cảm ơn bạn đã mua hàng tại PhoneStore.</p>
      </article>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
