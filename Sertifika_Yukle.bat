@echo off
chcp 65001 >nul
echo ========================================================
echo         Alt Tab Canavarı - Sertifika Yükleyici
echo ========================================================
echo.
echo Bu işlem, 'Alt Tab Canavarı' sertifikasını Windows Güvenilen
echo Yayıncılar ve Kök Sertifika deposuna ekler.
echo Böylece Windows SmartScreen uyarısı vermeden doğrudan açılır.
echo.
echo Yönetici yetkisi istenecektir, lütfen 'Evet'i seçin...
echo.

powershell -Command "Start-Process certutil -ArgumentList '-addstore -f Root \"%~dp0AltTabCanavari.cer\"' -Verb RunAs -Wait"
powershell -Command "Start-Process certutil -ArgumentList '-addstore -f TrustedPublisher \"%~dp0AltTabCanavari.cer\"' -Verb RunAs -Wait"

echo.
echo [BAŞARILI] Sertifika başarıyla Windows güvenilen yayıncılarına eklendi!
echo Artık AltTabCanavari.exe dosyasını doğrudan çalıştırabilirsiniz.
echo.
pause
