# 🖥️ Alt Tab Canavarı

**Windows 10/11 için Monitör Bazlı Alt+Tab Değiştirici**

Mouse imlecinin bulunduğu monitöre göre çalışan, tamamen özel yapım Alt+Tab uygulaması.

---

## ✨ Özellikler

- **Monitör Bazlı Geçiş** — Alt+Tab sadece mouse'un bulunduğu monitördeki pencereleri gösterir
- **Özel Overlay** — Koyu temalı, modern görünümlü pencere geçiş arayüzü
- **Tam Pencere Döngüsü** — 3, 5, 10+ pencere arasında sırayla geçiş yapabilirsiniz
- **Mouse ile Tıklama** — Overlay'deki herhangi bir pencereye tıklayarak geçiş yapabilirsiniz
- **Mouse Hover** — Üzerine gelinen pencere otomatik olarak seçilir
- **Ters Geçiş** — `Shift+Tab` ile ters yönde dolaşabilirsiniz
- **Çoklu Monitör** — 2, 3 veya daha fazla monitörü destekler
- **DPI Uyumlu** — Farklı DPI/ölçeklendirme ayarlarıyla çalışır
- **Sistem Tepsisi** — Sessizce arka planda çalışır
- **Açma/Kapama** — Tray menüsünden etkinleştirip devre dışı bırakabilirsiniz

---

## ⌨️ Kısayollar

| Kısayol | İşlev |
|---------|-------|
| `Alt + Tab` | Overlay'i aç, sonraki pencereye geç |
| `Alt + Tab` (tekrar) | Pencereler arasında ileri doğru dolaş |
| `Alt + Shift + Tab` | Ters yönde dolaş |
| `Alt` (bırak) | Seçili pencereyi etkinleştir |
| `Esc` | İptal et, overlay'i kapat |
| **Mouse tıklama** | Tıklanan pencereyi etkinleştir |
| **Mouse hover** | Üzerine gelinen pencereyi seç |

---

## 🖱️ Nasıl Çalışır?

```
Mouse sol monitörde → Alt+Tab → Sadece sol monitör pencereleri
Mouse sağ monitörde → Alt+Tab → Sadece sağ monitör pencereleri
```

### Örnek Senaryo

```
Sol Monitör:           Sağ Monitör:
├── Chrome             ├── Discord
├── VS Code            ├── Explorer  
└── Terminal           ├── Edge
                       └── Notepad
```

Mouse **sağ monitörde**yken `Alt+Tab`:

```
Discord → Explorer → Edge → Notepad → Discord → ...
```

Chrome, VS Code, Terminal **kesinlikle gösterilmez**.

---

## 🚀 Kurulum & Çalıştırma

### Gereksinimler

- Windows 10 veya 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Kaynaktan Derleme

```bash
git clone https://github.com/Akadirr1/alt_tab_canavari.git
cd alt_tab_canavari
dotnet build -c Release
```

### Çalıştırma

```bash
dotnet run
```

Veya derlenmiş exe:

```bash
bin\Release\net8.0-windows\MonitorAltTab.exe
```

### Tek Dosya Yayınlama

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## 🏗️ Mimari

```
alt_tab_canavari/
├── App.xaml / App.xaml.cs              # Giriş noktası, tray icon
├── Core/
│   ├── NativeMethods.cs                # Win32 P/Invoke tanımları
│   ├── KeyboardHook.cs                 # Low-level keyboard hook (WH_KEYBOARD_LL)
│   ├── WindowEnumerator.cs             # Pencere listeleme ve filtreleme
│   ├── MonitorManager.cs               # Monitör tespiti
│   ├── WindowActivator.cs              # Pencere etkinleştirme
│   └── AltTabManager.cs               # Oturum yöneticisi
├── Models/
│   ├── WindowInfo.cs                   # Pencere veri modeli
│   └── MonitorInfo.cs                  # Monitör veri modeli
├── UI/
│   └── AltTabOverlay.xaml/.cs          # Özel overlay penceresi
├── app.manifest                        # Uygulama manifest'i
└── MonitorAltTab.csproj                # Proje dosyası
```

---

## ⚙️ Teknik Detaylar

### Kullanılan Windows API'leri

| API | Kullanım |
|-----|----------|
| `SetWindowsHookEx` / `WH_KEYBOARD_LL` | Alt+Tab yakalama |
| `EnumWindows` | Pencere listeleme |
| `MonitorFromPoint` | Mouse'un monitörünü tespit etme |
| `MonitorFromWindow` | Pencerenin monitörünü tespit etme |
| `GetMonitorInfo` | Monitör bilgisi alma |
| `SetForegroundWindow` | Pencere etkinleştirme |
| `DwmGetWindowAttribute` | Cloaked pencere kontrolü |
| `GetWindowLong` / `GetWindowLongPtr` | Pencere stilleri |

### Pencere Filtreleme

Alt+Tab listesine dahil edilmemesi gereken pencereler:

- Görünmez pencereler (`IsWindowVisible == false`)
- Başlıksız pencereler
- Cloaked pencereler (sanal masaüstü gizli pencereler)
- `WS_EX_TOOLWINDOW` pencereler
- Shell pencereleri (Progman, WorkerW, Shell_TrayWnd vb.)
- Devre dışı pencereler (`WS_DISABLED`)
- Sahipli yardımcı pencereler
- Overlay'in kendi penceresi

### Klavye Hook State Machine

```
IDLE ──Alt↓──► ALT_HELD ──Tab↓──► SESSION_ACTIVE
                                      │
                    ┌─────────────────┤
                    │                 │
                Tab ↓ → sonraki    Shift+Tab ↓ → önceki
                    │                 │
                    ├── Escape → iptal → IDLE
                    ├── Alt↑ → etkinleştir → IDLE
                    └── Tıklama → etkinleştir → IDLE
```

---

## 🧪 Test Senaryoları

| # | Test | Beklenen Sonuç |
|---|------|----------------|
| 1 | Mouse Monitor 2'de, Alt+Tab | Sadece Monitor 2 pencereleri görünür |
| 2 | 5 pencere açık, Tab ile dolaş | 5 pencere sırayla dolaşılır |
| 3 | Mouse'u Monitor 1'e geçir, Alt+Tab | Sadece Monitor 1 pencereleri |
| 4 | Alt+Shift+Tab | Ters sırada geçiş |
| 5 | Overlay'de pencereye tıkla | Tıklanan pencere etkinleşir |
| 6 | Overlay'de pencere üzerine gel | Hover ile seçim değişir |
| 7 | Escape | Overlay kapanır, iptal |

---

## 📝 Lisans

MIT

---

## 🤝 Katkıda Bulunma

1. Fork'layın
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Commit atın (`git commit -m 'Yeni özellik ekle'`)
4. Push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request açın
