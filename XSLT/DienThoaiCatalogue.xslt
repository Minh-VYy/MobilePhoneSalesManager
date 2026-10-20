<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes" encoding="utf-8"/>
  <xsl:decimal-format name="vi" decimal-separator="," grouping-separator="."/>
  <xsl:template match="/">
    <table class="admin-data-table admin-xslt-catalog">
      <thead><tr><th>Sản phẩm / mã phiên bản</th><th>Thương hiệu</th><th>Cấu hình</th><th class="num">Giá bán</th><th class="num">Tồn kho</th></tr></thead>
      <tbody>
        <xsl:for-each select="DanhSachDienThoai/DienThoai">
          <tr>
            <td><div class="inventory-product"><img src="{HinhAnh}" alt=""/><span><b><xsl:value-of select="TenSP"/></b><small><xsl:value-of select="MaSP"/></small></span></div></td>
            <td><xsl:value-of select="TenHang"/></td>
            <td><xsl:value-of select="ThongSoKyThuat/RAM"/> · <xsl:value-of select="ThongSoKyThuat/BoNhoTrong"/><small><xsl:value-of select="ThongSoKyThuat/Chip"/></small><small><xsl:value-of select="ThongSoKyThuat/ManHinh"/></small></td>
            <td class="num"><xsl:value-of select="format-number(GiaBan, '#.##0', 'vi')"/> ₫</td>
            <td class="num"><xsl:value-of select="SoLuongTon"/></td>
          </tr>
        </xsl:for-each>
        <xsl:if test="count(DanhSachDienThoai/DienThoai) = 0"><tr><td colspan="5">Chưa có dữ liệu sản phẩm.</td></tr></xsl:if>
      </tbody>
    </table>
  </xsl:template>
</xsl:stylesheet>
