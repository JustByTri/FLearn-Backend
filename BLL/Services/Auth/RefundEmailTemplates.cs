using System;

namespace BLL.Services.Auth
{
    public static class RefundEmailTemplates
    {
  public static string GetRefundInstructionEmail(string userName, string className, DateTime classStartDateTime, string? reason)
 {
            var reasonSection = !string.IsNullOrEmpty(reason)
           ? $"<p style='font-size: 16px; line-height: 1.6; color: #856404; margin: 15px 0 0 0;'><strong>Lý do:</strong> {reason}</p>"
      : "";

      return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><title>H??ng d?n yêu c?u hoàn ti?n</title></head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>?? Flearn</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>H??ng d?n yêu c?u hoàn ti?n</p>
   </div>
   <div style='padding: 40px 30px;'>
       <div style='text-align: center; margin-bottom: 30px;'>
<h2 style='color: #2c3e50; margin: 0 0 10px 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
  </div>
            <div style='background-color: #fff3cd; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #ffc107;'>
 <h3 style='color: #856404; margin: 0 0 15px 0; font-size: 18px;'>?? Thông báo v? l?p h?c</h3>
    <p style='font-size: 16px; line-height: 1.6; color: #856404; margin: 0;'>
            L?p h?c <strong>{className}</strong> d? ki?n di?n ra vào <strong>{classStartDateTime:dd/MM/yyyy HH:mm}</strong> ?ã b? h?y.
        </p>
   {reasonSection}
  </div>
            <div style='background-color: #e7f3ff; padding: 30px; border-radius: 12px; margin: 30px 0;'>
  <h3 style='color: #004085; margin: 0 0 20px 0; font-size: 20px; text-align: center;'>?? H??ng d?n yêu c?u hoàn ti?n</h3>
              <p style='font-size: 14px; color: #555; line-height: 1.8;'>
     <strong>1.</strong> ??ng nh?p vào tài kho?n<br/>
          <strong>2.</strong> Vào ""L?p h?c c?a tôi""<br/>
    <strong>3.</strong> Ch?n ""G?i ??n hoàn ti?n""<br/>
     <strong>4.</strong> Ch?n lo?i yêu c?u và ?i?n thông tin ngân hàng<br/>
   <strong>5.</strong> G?i ??n và ch? ph?n h?i
         </p>
     </div>
 <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 25px; border-radius: 12px; text-align: center; margin: 30px 0;'>
         <h3 style='color: white; margin: 0 0 10px 0; font-size: 18px;'>?? Th?i gian x? lý</h3>
        <p style='color: rgba(255,255,255,0.9); margin: 0; font-size: 16px;'>Yêu c?u c?a b?n s? ???c x? lý trong vòng <strong>3-5 ngày làm vi?c</strong></p>
            </div>
        </div>
        <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
      <p style='color: #6c757d; margin: 0; font-size: 12px;'>© 2025 Flearn - N?n t?ng h?c ngôn ng? thông minh</p>
        </div>
  </div>
</body>
</html>";
        }

 public static string GetRefundConfirmationEmail(string userName, string className, string refundRequestId)
  {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><title>?ã nh?n yêu c?u hoàn ti?n</title></head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 40px 20px; text-align: center;'>
       <h1 style='color: white; margin: 0; font-size: 28px;'>? ?ã nh?n yêu c?u</h1>
    </div>
 <div style='padding: 40px 30px;'>
      <h2 style='color: #2c3e50;'>Xin chào {userName}!</h2>
     <p style='font-size: 16px; color: #555;'>
            Chúng tôi ?ã nh?n ???c yêu c?u hoàn ti?n c?a b?n cho l?p h?c <strong>{className}</strong>.
 </p>
            <div style='background-color: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0;'>
    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Mã yêu c?u:</strong> {refundRequestId}</p>
   </div>
 <p style='font-size: 16px; color: #555;'>
       Yêu c?u c?a b?n ?ang ???c xem xét và s? ???c x? lý trong vòng 3-5 ngày làm vi?c.
            </p>
        </div>
    </div>
</body>
</html>";
  }

   public static string GetRefundApprovedEmail(string userName, string className, decimal refundAmount, string? proofImageUrl, string? adminNote)
        {
         var proofImageSection = !string.IsNullOrEmpty(proofImageUrl)
           ? $@"<div style='text-align: center; margin: 25px 0;'>
           <h4 style='color: #28a745; margin: 0 0 15px 0;'>?? Hình ?nh xác nh?n chuy?n kho?n:</h4>
       <img src='{proofImageUrl}' alt='Ch?ng t? chuy?n kho?n' style='max-width: 100%; border-radius: 8px; border: 2px solid #28a745;'/>
 </div>"
          : "";

   var adminNoteSection = !string.IsNullOrEmpty(adminNote)
       ? $@"<div style='background-color: #d4edda; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745;'>
           <h4 style='color: #155724; margin: 0 0 10px 0;'>?? Ghi chú t? Admin:</h4>
              <p style='margin: 0; color: #155724; font-size: 14px;'>{adminNote}</p>
   </div>"
     : "";

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><title>??n hoàn ti?n ?ã ???c ch?p nh?n</title></head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #28a745 0%, #20c997 100%);'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 40px 20px; text-align: center;'>
     <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>?? Flearn</h1>
    <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Hoàn ti?n thành công</p>
        </div>
        <div style='padding: 40px 30px;'>
 <div style='text-align: center; margin-bottom: 30px;'>
           <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
   <span style='font-size: 36px; color: white;'>?</span>
                </div>
       <h2 style='color: #2c3e50; margin: 0 0 10px 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
            </div>
     <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 30px; border-radius: 12px; text-align: center; margin: 30px 0;'>
     <h3 style='color: white; margin: 0 0 15px 0; font-size: 22px;'>?? ??n hoàn ti?n ?ã ???c ch?p nh?n!</h3>
          <p style='color: rgba(255,255,255,0.9); margin: 0; font-size: 16px;'>S? ti?n ?ã ???c chuy?n v? tài kho?n c?a b?n</p>
            </div>
            <div style='background-color: #e7f3ff; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #007bff;'>
     <h3 style='color: #004085; margin: 0 0 15px 0; font-size: 18px;'>?? Thông tin hoàn ti?n:</h3>
      <p style='margin: 10px 0; color: #004085; font-size: 15px;'><strong>L?p h?c:</strong> {className}</p>
   <p style='margin: 10px 0; color: #004085; font-size: 15px;'><strong>S? ti?n hoàn:</strong> <span style='color: #28a745; font-size: 20px; font-weight: bold;'>{refundAmount:N0} VN?</span></p>
            </div>
     {proofImageSection}
      {adminNoteSection}
   <div style='background-color: #d4edda; border: 1px solid #c3e6cb; padding: 20px; border-radius: 8px; margin: 25px 0;'>
     <p style='margin: 0; color: #155724; font-size: 14px; text-align: center;'>
    ?? <strong>L?u ý:</strong> Vui lòng ki?m tra tài kho?n ngân hàng c?a b?n. Ti?n th??ng v? trong vòng 1-3 ngày làm vi?c.
              </p>
</div>
</div>
        <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
<p style='color: #6c757d; margin: 0; font-size: 12px;'>© 2025 Flearn - N?n t?ng h?c ngôn ng? thông minh</p>
        </div>
    </div>
</body>
</html>";
        }

  public static string GetRefundRejectedEmail(string userName, string className, string rejectionReason)
   {
      return $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><title>??n hoàn ti?n không ???c ch?p nh?n</title></head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #6c757d 0%, #495057 100%);'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
 <div style='background: linear-gradient(135deg, #6c757d 0%, #495057 100%); padding: 40px 20px; text-align: center;'>
        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>?? Flearn</h1>
   <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Thông báo v? ??n hoàn ti?n</p>
        </div>
  <div style='padding: 40px 30px;'>
            <div style='text-align: center; margin-bottom: 30px;'>
     <div style='background: linear-gradient(135deg, #6c757d 0%, #495057 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
        <span style='font-size: 36px; color: white;'>?</span>
           </div>
   <h2 style='color: #2c3e50; margin: 0 0 10px 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
     </div>
      <div style='background-color: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #6c757d;'>
      <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
        Chúng tôi r?t ti?c ph?i thông báo r?ng yêu c?u hoàn ti?n c?a b?n cho l?p h?c <strong>{className}</strong> không ???c ch?p nh?n.
          </p>
            </div>
            <div style='background-color: #f8d7da; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #dc3545;'>
      <h3 style='color: #721c24; margin: 0 0 15px 0; font-size: 18px;'>?? Lý do t? ch?i:</h3>
     <p style='margin: 0; color: #721c24; font-size: 15px; line-height: 1.6;'>{rejectionReason}</p>
       </div>
            <div style='background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 20px; border-radius: 8px; margin: 25px 0;'>
            <p style='margin: 0; color: #0c5460; font-size: 14px; text-align: center;'>
    ?? <strong>C?n h? tr? thêm?</strong> Vui lòng liên h? v?i b? ph?n ch?m sóc khách hàng ?? ???c gi?i ?áp th?c m?c
      </p>
          </div>
    </div>
        <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
       <p style='color: #6c757d; margin: 0; font-size: 12px;'>© 2025 Flearn - N?n t?ng h?c ngôn ng? thông minh</p>
   </div>
    </div>
</body>
</html>";
        }
    }
}
