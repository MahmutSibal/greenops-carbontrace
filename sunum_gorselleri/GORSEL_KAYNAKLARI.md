# Sunum Görselleri — Kaynakça ve Kullanım Koşulları

Bu klasördeki her görselin kaynağı, lisansı ve sunumda nasıl kullanılması gerektiği aşağıdadır.
Şartname: *"Kullanılan veri, açık kaynak yazılım, görsel ve üçüncü taraflara ait içeriklerin kaynakları belirtilmelidir."*
Son kontrol: **28 Ekim 2026**.

---

## 1. `uygulama/` — GreenOps CarbonTrace ekran görüntüleri (takımın kendi çalışması)

Android emulator'de (Medium Phone, API 37) çalışan uygulamadan alınmıştır.

**Gösterilen tüm şirket, kişi, tesis ve tüketim verileri KURGUSALDIR:**
- Şirket: "Örnek Tekstil A.Ş.", tesisler: "Bursa Dokuma Tesisi", "İzmir Lojistik Deposu"
- Kişiler: Deniz Yılmaz, Ege Kaya, Selin Aydın; e-posta alan adı `ornek.test` (gerçek olmayan test alan adı)
- Tedarikçiler: "Örnek Lojistik Ltd.", "Örnek İplik San."
- Nisan–Ekim 2026 elektrik tüketimleri ve 2030 hedefi demo amaçlı girilmiştir.

Ekranlardaki emisyon değerleri **yalnızca resmi kaynaklı elektrik katsayısıyla** hesaplanmıştır: ETKB, ETKB-EVÇED-FRM-042, 2023, dağıtım hattından bağlı tüketim noktası, 0,469 kgCO2e/kWh (bkz. `../KAYNAKLAR.md` bölüm 5). Kaynaksız doğalgaz/motorin yer tutucu katsayıları bu ekranlarda kullanılmamıştır.

| Dosya | İçerik |
|---|---|
| `00_demo_fatura_kurgusal.png` | AI okumasında kullanılan demo fatura. Sağlayıcı adı kurgusaldır ("Örnek Enerji Dağıtım A.Ş."); üzerinde "KURGUSAL ÖRNEK" filigranı ve açıklaması vardır. **Gerçek bir kuruma veya faturaya ait değildir.** |
| `01_giris.png` | Giriş ekranı |
| `02_panel_genel.png` | Dashboard: toplam onaylı emisyon (155,2 tCO2e), KPI kartları |
| `03_panel_ai_ongoruler.png` | AI öngörüleri (gemma4): özet, gelecek ay tahmini, şüpheli kayıt, öneriler |
| `04_panel_aylik_grafik.png` | Faaliyet dönemine göre aylık emisyon + tahmin çubuğu |
| `05_panel_kapsam.png` | GHG Protokolü kapsam dağılımı |
| `06_panel_tesis_hedef.png` | Tesis karşılaştırması ve azaltım hedefi (yıllık tahmine göre %0 — şirket hedefin gerisinde) |
| `07_emisyon_listesi.png` | Emisyon kayıtları, durum filtreleri |
| `08_kayit_detay_bekleyen.png` | Onay bekleyen (anomali olarak işaretlenen) kayıt, onay/red butonları |
| `09_kayit_detay_onayli_resmi_katsayi.png` | Onaylı kayıt: hesaplama formülü |
| `10_kayit_detay_katsayi_kaynagi.png` | Kullanılan katsayının resmi kaynağı ve bağlantısı |
| `11_yeni_kayit_formu.png` | Yeni kayıt formu, "Faturadan AI ile doldur" |
| `12_ai_fatura_okunuyor.png` | AI fatura okuma sürüyor (qwen3-vl → gemma4) |
| `13_ai_fatura_sonucu.png` | AI'nın doldurduğu alanlar + resmi katsayıyla tahmini sonuç |
| `14_ai_asistan_karsilama.png` | Karbon asistanı, örnek sorular |
| `15_ai_asistan_yanit.png` | Asistanın şirket verisine dayalı yanıtı |
| `16_tesisler.png` | Tesis listesi ve tesis bazlı metrikler |
| `17_daha_fazla_menu.png` | Rol bazlı modül menüsü |
| `18_hedefler.png` | Hedef ilerlemesi (gerçekleşen + yıllık tahmin ayrımı) |
| `19_tedarikciler.png` | Tedarikçiler ve ESG puanı |
| `20_kullanicilar.png` | Ekip ve roller |
| `21_denetim_kaydi.png` | Değiştirilemez denetim kaydı |
| `22_profil_guvenlik.png` | Profil, şifre değiştirme, AI model durumu |

> **Sunumda söylenmesi gerekenler:** "Ekranlardaki veriler kurgusal demo verisidir." Fatura okuma ekranında AI'nın alanları doldurduğu ama kaydın kullanıcı onayıyla oluştuğu belirtilmelidir.
> **Süre uyarısı:** Bu bilgisayarda AI fatura okuma ~1–5 dk sürdü (iki model sırayla belleğe yükleniyor). Canlı demoda modeller önceden "ısıtılmalı" veya bu adım önceden kaydedilmiş videoyla gösterilmelidir.

---

## 2. `logolar/` — Kullanılan teknolojilerin logoları (ticari markalar)

Logolar sahiplerinin **ticari markalarıdır**; yalnızca projenin bu teknolojileri **kullandığını belirtmek** için, değiştirilmeden kullanılmalıdır. Hiçbiri projeyi onaylamaz veya desteklemez; slaytta bu izlenim verilmemelidir.

| Dosya | Sahibi | Kaynak (resmi) | Kullanım koşulu |
|---|---|---|---|
| `flutter_built-with.svg/.png` | Google LLC | https://flutter.dev/brand | Flutter marka sayfasına göre bir uygulamanın Flutter ile yapıldığını belirtirken **bu "Built with Flutter" lockup'ı** kullanılır. Değiştirilmemeli, sayfadaki en baskın öğe olmamalı, kendi ürün adınızla birleştirilmemelidir. |
| `flutter_logomark.svg/.png` | Google LLC | https://flutter.dev/brand | Aynı kurallar; ürünün içinde veya Flutter'ın ürünü onayladığı izlenimi verecek şekilde kullanılmaz. |
| `dart_yatay.svg/.png`, `dart_yatay_beyaz.svg`, `dart_logomark.svg/.png` | Google LLC | https://dart.dev/brand (varlık paketi: https://services.google.com/fh/files/misc/dart_brand_guidelines_assets.zip) | İlk/en belirgin kullanımın yakınına şu bildirim eklenmelidir: **"Dart and the related logo are trademarks of Google LLC. We are not endorsed by or affiliated with Google LLC."** |
| `dotnet_logo.svg/.png` | .NET Foundation / .NET yazarları | https://github.com/dotnet/brand (`logo/` klasörü) | Depoya göre logo ".NET authors" telif hakkıdır; .NET'i temsil etmek için marka yönergelerine uygun kullanılır. |
| `android_robot_flat.svg/.png` | Google LLC | https://developer.android.com/distribute/marketing-tools/brand-guidelines | CC BY 3.0. **Zorunlu atıf satırı:** "The Android robot is reproduced or modified from work created and shared by Google and used according to terms described in the Creative Commons 3.0 Attribution License." |
| `ollama_logo.png` | Ollama | https://ollama.com/public/ollama.png (resmi site) | Ollama yayımlanmış bir marka kullanım yönergesi bulunamadı; yalnızca teknolojiyi tanımlamak için kullanın. |
| `qwen3-vl_logo.png` | Alibaba Cloud / Qwen | https://qianwen-res.oss-accelerate.aliyuncs.com/Qwen3-VL/qwen3vllogo.png — resmi depo README'sinin kullandığı görsel (https://github.com/QwenLM/Qwen3-VL) | Deponun Apache-2.0 lisansı kodu kapsar, markayı kapsamaz; yalnızca modeli tanımlamak için kullanın. |

**Bilerek eklenmeyenler:**
- **Gemma:** Google'ın yayımladığı resmi bir Gemma logo indirme sayfası bulunamadı; üçüncü taraf logo siteleri kaynak olarak kullanılmadı. Sunumda yazıyla belirtin: "Gemma 4 (Google DeepMind)".
- **SQL Server, GHG Protocol, IPCC, ETKB logoları:** Kurum logoları izin gerektirebilir; bu kurumlar metin olarak ve kaynak bağlantısıyla anılmalıdır.

---

## 3. `iklim_fotograflari/` — Serbest lisanslı fotoğraflar (Wikimedia Commons)

Tüm dosyalar Wikimedia Commons'tan 1920 px genişlikte indirildi; lisans ve yazar bilgisi Commons API meta verisinden alındı (`iklim_fotograflari/_meta.json`). **CC BY lisanslarında slayta atıf yazmak zorunludur.** Önerilen atıf biçimi slaytın altına küçük yazıyla: *Fotoğraf: Yazar, Lisans, Wikimedia Commons*.

| Dosya | Konu | Yazar | Lisans | Kaynak sayfa |
|---|---|---|---|---|
| `ruzgar_babali_kocaeli.jpg` | Babalı Rüzgâr Santrali, Kocaeli | Şadi Akdoğan | CC BY 2.0 | https://commons.wikimedia.org/wiki/File:Babal%C4%B1_Wind_Farm,_Kocaeli,_Turkey.jpg |
| `ruzgar_alacati_cesme.jpg` | Alaçatı Rüzgâr Santrali, Çeşme | David Broad | CC BY 3.0 | https://commons.wikimedia.org/wiki/File:Alacati_Wind_Farm_-_Cesme_Turkey_-_panoramio.jpg |
| `ruzgar_karli_dag_turkiye.jpg` | Karlı dağda rüzgâr santrali, Türkiye | Flyttblock | CC0 (atıf zorunlu değil) | https://commons.wikimedia.org/wiki/File:Wind_farm_on_snowy_mountain_landscape_Turkey_01.jpg |
| `turkiye_gunes_potansiyeli_haritasi.png` | Türkiye fotovoltaik enerji potansiyeli haritası (Türkçe) | Solargis (Dünya Bankası adına) | CC BY 4.0 — **zorunlu atıf:** "© The World Bank, Source: Global Solar Atlas 2.0, Solar resource data: Solargis" | https://commons.wikimedia.org/wiki/File:Turkey_PVOUT_Photovoltaic-power-potential-map_lang-TR_GlobalSolarAtlas_World-Bank-Esmap-Solargis.png |
| `sanayi_bacasi.jpg` | Sanayi bacası, **Zlatna, Romanya** (Türkiye değil — temsili görsel olarak kullanın) | Jan Pesula (Sapfan) | Kamu malı | https://commons.wikimedia.org/wiki/File:Zlatna_chimney.JPG |
| `antalya_sahil.jpg` | Antalya sahil bisiklet yolu (COP31 ev sahibi şehir) | Levi Meir Clancy | CC0 | https://commons.wikimedia.org/wiki/File:The_bicycle_path_along_the_coast_in_Antalya,_Turkey_DSF8523.jpg |
| `iletim_hatlari_gunbatimi.jpg` | Gün batımında enerji iletim hatları | Kkiefuik | CC BY 4.0 | https://commons.wikimedia.org/wiki/File:Transmission_Lines_at_Sunset.jpg |

Lisans metinleri: CC BY 2.0 https://creativecommons.org/licenses/by/2.0 · CC BY 3.0 https://creativecommons.org/licenses/by/3.0 · CC BY 4.0 https://creativecommons.org/licenses/by/4.0 · CC0 https://creativecommons.org/publicdomain/zero/1.0/

Fotoğraflar kırpılır veya üzerine yazı eklenirse CC BY lisansları gereği "değiştirildi" notu eklenmelidir (ör. "kırpılmıştır").

---

## 4. Kullanılmayanlar / dikkat

- **COP31 logosu** eklenmedi: Etkinlik logosu düzenleyicilere aittir; kullanım izni ve resmi dosyası düzenleyicilerden alınmalıdır.
- Arama sonuçlarındaki logo/vektör siteleri (logowik, vectorseek vb.) **kullanılmadı**: lisansları belirsiz veya ticari kullanıma kapalıdır.
