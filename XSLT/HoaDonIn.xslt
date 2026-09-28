<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes" encoding="utf-8"/>

  <xsl:template match="/Order">
    <html>
      <head>
        <meta charset="utf-8"/>
        <title>Hóa đơn điện tử - <xsl:value-of select="@id"/></title>
        <style>
          body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #0d1117;
            color: #e6edf3;
            padding: 40px;
          }
          .invoice-card {
            max-width: 650px;
            margin: 0 auto;
            background: rgba(22, 27, 34, 0.75);
            backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 16px;
            padding: 32px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
          }
          .header { border-bottom: 1px solid rgba(255, 255, 255, 0.1); padding-bottom: 16px; margin-bottom: 20px; }
          .header h2 { margin: 0; color: #58a6ff; font-size: 24px; }
          .meta { font-size: 13px; color: #8b949e; margin-top: 6px; }
          table { width: 100%; border-collapse: collapse; margin-top: 20px; }
          th { text-align: left; padding: 10px; border-bottom: 1px solid rgba(255, 255, 255, 0.15); color: #8b949e; }
          td { padding: 12px 10px; border-bottom: 1px solid rgba(255, 255, 255, 0.05); }
          .price-col { text-align: right; }
          .summary { margin-top: 24px; padding-top: 16px; border-top: 1px solid rgba(255, 255, 255, 0.15); }
          .summary-row { display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; }
          .total { font-size: 18px; font-weight: bold; color: #3fb950; }
          .badge { background: rgba(56, 139, 253, 0.15); color: #58a6ff; padding: 4px 8px; border-radius: 6px; font-size: 12px; }
        </style>
      </head>
      <body>
        <div class="invoice-card">
          <div class="header">
            <h2>HÓA ĐƠN BÁN HÀNG</h2>
            <div class="meta">Mã HĐ: <strong><xsl:value-of select="@id"/></strong> | Ngày tạo: <xsl:value-of select="@createdAt"/></div>
            <div class="meta">Khách hàng: <xsl:value-of select="CustomerInfo/Name"/> - SĐT: <xsl:value-of select="CustomerInfo/Phone"/></div>
          </div>

          <table>
            <thead>
              <tr>
                <th>Tên sản phẩm</th>
                <th style="width: 50px; text-align: center;">SL</th>
                <th class="price-col">Đơn giá</th>
                <th class="price-col">Thành tiền</th>
              </tr>
            </thead>
            <tbody>
              <xsl:for-each select="Items/Item">
                <tr>
                  <td><xsl:value-of select="ProductName"/></td>
                  <td style="text-align: center;"><xsl:value-of select="Quantity"/></td>
                  <td class="price-col"><xsl:value-of select="UnitPrice"/> đ</td>
                  <td class="price-col"><xsl:value-of select="Subtotal"/> đ</td>
                </tr>
              </xsl:for-each>
            </tbody>
          </table>

          <div class="summary">
            <div class="summary-row">
              <span>Tạm tính:</span>
              <span><xsl:value-of select="PaymentSummary/SubtotalAmount"/> đ</span>
            </div>
            <xsl:if test="PaymentSummary/VoucherApplied">
              <div class="summary-row" style="color: #f85149;">
                <span>Giảm giá (<xsl:value-of select="PaymentSummary/VoucherApplied/@code"/>):</span>
                <span>-<xsl:value-of select="PaymentSummary/VoucherApplied"/> đ</span>
              </div>
            </xsl:if>
            <div class="summary-row total">
              <span>Tổng thanh toán:</span>
              <span><xsl:value-of select="PaymentSummary/TotalAmount"/> đ</span>
            </div>
            <div class="summary-row" style="margin-top: 12px;">
              <span>Điểm tích lũy cộng thêm:</span>
              <span class="badge">+<xsl:value-of select="PaymentSummary/PointsEarned"/> điểm</span>
            </div>
          </div>
        </div>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
