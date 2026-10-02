<div align="center">
  <img src="Assets/logo.png" width="160" alt="Alt Tab Canavarı Logo" style="border-radius: 24px;" />
  <h1>👾 Alt Tab Canavarı</h1>
  <p><strong>Windows 10 ve 11 için Çoklu Monitör Akıllı Alt+Tab Pencere Yöneticisi</strong></p>
  <p>
    <a href="https://github.com/Akadirr1/alt_tab_canavari/releases"><img src="https://img.shields.io/github/v/release/Akadirr1/alt_tab_canavari?style=for-the-badge&color=blue" alt="Release" /></a>
    <a href="https://github.com/Akadirr1/alt_tab_canavari/releases"><img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=for-the-badge&logo=windows" alt="Windows" /></a>
    <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 8" /></a>
  </p>
</div>

---

## 🎯 Nedir?

Windows'un varsayılan Alt+Tab davranışında tüm ekranlardaki pencereler tek bir listede toplanır ve bu da çoklu monitör kullananlarda dikkat dağınıklığına yol açar.

**Alt Tab Canavarı**, fare imlecinizin bulunduğu monitörü anlık olarak tespit eder ve `Alt+Tab` tuşlarına bastığınızda **yalnızca o ekranda bulunan açık pencereleri** listeler! Diğer ekranlardaki pencereler kesinlikle araya girmez.

---

## ✨ Temel Özellikler

- 🖥️ **Monitör Bazlı Filtreleme**: Fare imleciniz hangi ekrandaysa yalnızca o ekrandaki pencereler gösterilir.
- 🎛️ **Modern Kontrol Paneli**: Bağlı monitörleri, farenin bulunduğu ekranı ve açık pencereleri canlı olarak izleyebileceğiniz modern karanlık tema arayüz.
- 🎨 **Özel Akıcı Overlay**: Koyu temalı, uygulama simgelerini ve başlıklarını içeren modern Alt+Tab arayüzü.
- 🖱️ **Fare ile Doğrudan Seçim**: Açılan liste üzerinden istediğiniz pencereye tıklayarak anında öne getirebilirsiniz.
- 🔄 **Sınırsız Dolaşım**: `Tab` ile ileri, `Shift+Tab` ile geri yönde tüm pencereleri gezebilme.
- 🛡️ **Dijital İmzalı & SmartScreen Uyumlu**: Uygulama dijital olarak imzalanmıştır. Tek tıkla yerel güvenilir yayıncılara eklenebilir.
- 🚀 **Windows ile Otomatik Başlatma**: İsteğe bağlı olarak Windows açılışında arka planda sessizce başlayabilir.
- 📌 **Sistem Tepsisi Entegrasyonu**: Kapatıldığında arka planda minimum kaynak tüketimiyle (10 MB RAM) sessizce çalışmaya devam eder.

---

## ⌨️ Kısayollar ve Kullanım

| Kısayol | İşlev |
|---------|-------|
| `Alt + Tab` | Overlay'i açar, sıradaki pencereye geçer |
| `Alt + Tab` (tekrar) | Pencereler arasında ileri doğru dolaşır |
| `Alt + Shift + Tab` | Ters yönde geriye doğru dolaşır |
| `Alt` (bırakma) | Seçili pencereyi ön plana getirir |
| `Fare Sol Tık` | Listeden tıklanan pencereyi doğrudan aktifleştirir |
| `Escape (Esc)` | Seçimi iptal eder, overlay'i kapatır |

---

## 🖱️ Nasıl Çalışır?

```
Fare Sol Monitörde  ──►  Alt+Tab  ──►  Yalnızca Sol Ekrandaki Pencereler
Fare Sağ Monitörde ──►  Alt+Tab  ──►  Yalnızca Sağ Ekrandaki Pencereler
```

```
[Sol Monitör]                      [Sağ Monitör]
├── Visual Studio Code              ├── Google Chrome
├── Windows Terminal                ├── Spotify
└── Slack                           └── Discord
```
Fare sağ ekrandayken `Alt+Tab` basıldığında yalnızca **Chrome ➔ Spotify ➔ Discord** arasında geçiş yapılır.

---

## 🛡️ Sertifika ve SmartScreen Çözümü

Windows, bağımsız geliştiricilerin yeni uygulamalarını indirdiğinizde *"Windows bilgisayarınızı korudu"* (SmartScreen) uyarısı verebilir.

Bu projede **Alt Tab Canavarı** adına özel dijital sertifika üretilmiştir. Uyarıyı tamamen kaldırmak için:

1. İndirdiğiniz zip içerisindeki **`Sertifika_Yukle.bat`** dosyasını çalıştırın (veya Kontrol Panelindeki **"Sertifikayı Yükle"** butonuna tıklayın).
2. Gelen onay penceresine **"Evet"** deyin.
3. Artık Windows bu uygulamayı güvenilir yayıncı olarak tanır ve hiçbir uyarı vermeden doğrudan açar.

*(Alternatif olarak SmartScreen çıktığında **"Ek bilgi"** ➔ **"Yine de çalıştır"** diyebilirsiniz).*

---

## 📦 Kurulum ve Çalıştırma

Kurulum veya ek yazılım (`.NET Runtime`) gerekmez!

1. [Releases](https://github.com/Akadirr1/alt_tab_canavari/releases) sayfasından son sürümü indirin.
2. `AltTabCanavari.exe` dosyasını çalıştırın.
3. Kontrol Paneli açıldığında monitörlerinizi ve durumunuzu görebilir, ardından pencereyi kapatıp tepside çalışmaya bırakabilirsiniz.

---

## 🛠️ Kaynak Koddan Derleme

```bash
# Projeyi klonlayın
git clone https://github.com/Akadirr1/alt_tab_canavari.git
cd alt_tab_canavari

# Tek dosya bağımsız (Self-Contained) olarak derleyin
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Çıktı `bin/Release/net8.0-windows/win-x64/publish/` klasöründe oluşacaktır.

---

## 📄 Lisans

Bu proje MIT lisansı ile lisanslanmıştır.
Geliştirici: **Akadirr1**
