<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes" encoding="utf-8"/>

  <xsl:template match="/">
    <div class="xslt-catalogue-container">
      <div class="catalogue-header" style="margin-bottom: 1.5rem;">
        <h2 style="color: var(--primary); font-size: 1.4rem; font-weight: 700;">📱 DANH MỤC ĐIỆN THOẠI DI ĐỘNG (XSLT TRANSFORMED)</h2>
        <p class="text-muted" style="font-size: 0.9rem;">Dữ liệu được chuyển đổi trực tiếp từ XML bằng XSLT Template Engine trong C# .NET 9</p>
      </div>

      <div class="phone-grid">
        <xsl:for-each select="DanhSachDienThoai/DienThoai">
          <div class="phone-card scroll-reveal">
            <div class="phone-badge">
              <xsl:value-of select="TenHang"/>
            </div>
            <div class="phone-img-box">
              <img>
                <xsl:attribute name="src">
                  <xsl:value-of select="HinhAnh"/>
                </xsl:attribute>
                <xsl:attribute name="alt">
                  <xsl:value-of select="TenSP"/>
                </xsl:attribute>
              </img>
            </div>
            <div class="phone-info">
              <span class="phone-code">XML Code: <xsl:value-of select="MaSP"/></span>
              <h3 class="phone-title"><xsl:value-of select="TenSP"/></h3>
              
              <div class="phone-specs">
                <div><strong>Màn hình:</strong> <xsl:value-of select="ThongSoKyThuat/ManHinh"/></div>
                <div><strong>RAM/ROM:</strong> <xsl:value-of select="ThongSoKyThuat/RAM"/> - <xsl:value-of select="ThongSoKyThuat/BoNhoTrong"/></div>
                <div><strong>Chip:</strong> <xsl:value-of select="ThongSoKyThuat/Chip"/></div>
                <div><strong>Pin:</strong> <xsl:value-of select="ThongSoKyThuat/Pin"/></div>
              </div>

              <div class="phone-footer">
                <div class="phone-price">
                  <xsl:value-of select="format-number(GiaBan, '#,##0')"/> VNĐ
                </div>
                <div class="phone-stock">
                  Tồn: <span class="stock-num"><xsl:value-of select="SoLuongTon"/></span>
                </div>
              </div>
            </div>
          </div>
        </xsl:for-each>
      </div>
    </div>
  </xsl:template>
</xsl:stylesheet>
