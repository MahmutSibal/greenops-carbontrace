# Kaynaklar ve Üçüncü Taraf Bileşenler

Bu dosyadaki her bilgi **birincil kaynaktan** (lisansı veren kuruluşun kendi sayfası, paketin kendi lisans dosyası veya resmi kurum belgesi) doğrulanmıştır. "Doğrulama" sütunu, bilginin nereden ve nasıl kontrol edildiğini gösterir.
Son doğrulama tarihi: **28 Ekim 2026**. Lisanslar ve resmi katsayılar zamanla değişebilir; teslimden önce bağlantılar yeniden kontrol edilmelidir.

---

## 1. Yapay zeka modelleri

Modeller, backend'in çalıştığı makinede Ollama üzerinden yerel olarak çalışır. Bulut tabanlı bir AI servisine (OpenAI, Gemini API vb.) istek gönderilmez.

| Model (Ollama etiketi) | Geliştirici | Kullanım | Lisans | Doğrulama |
|---|---|---|---|---|
| `qwen3-vl:8b` | Qwen ekibi (Alibaba) | Fatura/sayaç görselinden metin okuma (OCR) | Apache-2.0 | (1) Yerel model dosyasıyla gelen lisans metni (`ollama show`): Apache License 2.0. (2) Kaynak modelin Hugging Face sayfası: https://huggingface.co/Qwen/Qwen3-VL-8B-Thinking — "License: apache-2.0" |
| `gemma4:12b` | Google DeepMind | OCR metnini yapılandırma, öngörü metni, asistan | Apache-2.0 | (1) Yerel model dosyasıyla gelen lisans metni: Apache License 2.0. (2) Google'ın resmi Gemma 4 sayfası (https://ai.google.dev/gemma/docs/core) "Gemma 4 license" bağlantısıyla https://ai.google.dev/gemma/apache_2 adresindeki Apache 2.0 metnine yönlendirir. (3) https://huggingface.co/google/gemma-4-12B-it — "License: apache-2.0" |
| Ollama (model sunucusu) | Ollama | Modelleri yerelde çalıştırma | MIT | https://github.com/ollama/ollama/blob/main/LICENSE |

Notlar:
- Kurulu `qwen3-vl:8b` modelinin kaynağı, Ollama meta verisine göre **8B-Thinking** sürümüdür (`parent_model: dhiltgen/qwen3-vl:8b-thinking-q4_K_M-llamacpp`). Bu sürüm yanıttan önce akıl yürütme çıktısı üretir; bu yüzden uygulamada sadece metin okuma (OCR) için kullanılır.
- Her iki model de 4-bit nicemlenmiş (Q4_K_M) GGUF biçimindedir.
- Akademik atıf gerekirse Qwen için model sayfasının önerdiği atıf: *Qwen3 Technical Report*, arXiv:2505.09388.

### AI çıktılarının nasıl kullanıldığı
- AI'nın fatura okuması **sadece öneridir**; kullanıcı kontrol edip kaydetmeden emisyon kaydı oluşmaz.
- **Tahmin** (son en fazla 6 ayın doğrusal eğilimi) ve **anomali tespiti** (onaylı kayıtların kategori medyanının 3 katından büyük bekleyen kayıtlar) model tarafından değil, uygulama kodunda hesaplanır: `backend/GreenOps.Infrastructure/Ai/OllamaAiService.cs`. Model sadece bu sayıları yorumlayan özet ve öneri metnini yazar.

---

## 2. Backend (.NET 8) paketleri

Lisanslar, projede kullanılan **tam sürümün** NuGet paket tanımından (`.nuspec`) okunmuştur.

| Paket | Sürüm | Lisans | Doğrulama |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer (ve EF Core) | 8.0.6 | MIT | https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/8.0.6 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.6 | MIT | https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/8.0.6 |
| MediatR | 11.1.0 | Apache-2.0 | https://www.nuget.org/packages/MediatR/11.1.0 |
| MediatR.Extensions.Microsoft.DependencyInjection | 11.1.0 | Apache-2.0 | https://www.nuget.org/packages/MediatR.Extensions.Microsoft.DependencyInjection/11.1.0 |
| FluentValidation (+ DependencyInjectionExtensions) | 11.9.0 | Apache-2.0 | https://www.nuget.org/packages/FluentValidation/11.9.0 |
| BCrypt.Net-Next | 4.1.0 | MIT | https://www.nuget.org/packages/BCrypt.Net-Next/4.1.0 |
| Swashbuckle.AspNetCore | 6.5.0 | MIT | https://www.nuget.org/packages/Swashbuckle.AspNetCore/6.5.0 |

> **Uyarı — MediatR:** Projede kullanılan 11.1.0 sürümü Apache-2.0'dır. Ancak MediatR'ın güncel deposunun lisansı artık açık kaynak olarak tanımlanmamaktadır (GitHub lisans API'si: `NOASSERTION`, https://github.com/LuckyPennySoftware/MediatR/blob/main/LICENSE.md). **MediatR sürümü yükseltilmeden önce yeni sürümün lisansı incelenmelidir.**

TOTP (RFC 6238) iki aşamalı doğrulama, harici kütüphane kullanılmadan yazılmıştır: `backend/GreenOps.Infrastructure/Security/Totp.cs`.

---

## 3. Mobil uygulama (Flutter) paketleri

Lisanslar, projede çözümlenen **tam sürümün** paket arşivindeki `LICENSE` dosyasından okunmuştur (sürümler: `pubspec.lock`).

| Paket | Sürüm | Lisans | Doğrulama |
|---|---|---|---|
| Flutter SDK (flutter, flutter_localizations) | 3.47.8 | BSD-3-Clause | https://github.com/flutter/flutter/blob/master/LICENSE |
| http | 1.6.0 | BSD-3-Clause | https://pub.dev/packages/http/license |
| shared_preferences | 2.5.3 | BSD-3-Clause | https://pub.dev/packages/shared_preferences/license |
| intl | 0.20.3 | BSD-3-Clause | https://pub.dev/packages/intl/license |
| image_picker | 1.2.4 | BSD-3-Clause | https://pub.dev/packages/image_picker/license |
| google_fonts | 6.3.2 | BSD-3-Clause | https://pub.dev/packages/google_fonts/license |
| fl_chart | 0.68.0 | MIT | https://github.com/imaNNeo/fl_chart/blob/main/LICENSE |

### Yazı tipi
| Yazı tipi | Tasarımcı | Lisans | Doğrulama |
|---|---|---|---|
| Manrope | Mikhail Sharanda | SIL Open Font License 1.1 | Google Fonts resmi deposu: https://github.com/google/fonts/blob/main/ofl/manrope/OFL.txt ve METADATA.pb |

> **Ağ bağlantısı notu:** `google_fonts` paketi yazı tipini çalışma anında Google'ın `fonts.gstatic.com` sunucusundan indirir (paket README'si: "HTTP fetching at runtime"). Yani uygulama, emisyon verisi göndermese de ilk açılışta Google sunucusuna bağlanır. Tamamen çevrimdışı çalışma iddiası yapılacaksa yazı tipi dosyası uygulamaya gömülmelidir.

---

## 4. Metodoloji

| Konu | Kaynak | Doğrulama |
|---|---|---|
| Kapsam (Scope 1/2/3) sınıflandırması | *GHG Protocol Corporate Accounting and Reporting Standard*, WRI ve WBCSD | https://ghgprotocol.org/corporate-standard |
| Yakıt yakma varsayılan emisyon faktörleri | *2006 IPCC Guidelines for National Greenhouse Gas Inventories*, Cilt 2 (Enerji), Bölüm 2 (Sabit Yakma), Tablo 2.2 | https://www.ipcc-nggip.iges.or.jp/public/2006gl/pdf/2_Volume2/V2_2_Ch2_Stationary_Combustion.pdf |

---

## 5. Emisyon katsayıları

### 5.1 Resmi kaynaklı değerler (doğrulandı)

**Elektrik (Scope 2, konum bazlı) — T.C. Enerji ve Tabii Kaynaklar Bakanlığı**
Belge: *Türkiye Elektrik Üretimi ve Elektrik Tüketim Noktası Emisyon Faktörleri Bilgi Formu*, Doküman No ETKB-EVÇED-FRM-042 Rev.01, hesaplama dönemi **2023**, hesaplama yayım tarihi **26.12.2025**.
https://enerji.gov.tr/Media/Dizin/EVCED/tr/%C3%87evreVe%C4%B0klim/%C4%B0klimDe%C4%9Fi%C5%9Fikli%C4%9Fi/EmisyonFaktorleri/2023_Turkiye_Elektrik_UretimiveElektrik_Tuketim_Noktasi_Emisyon_Faktorleri.pdf

| Faktör | tCO2/MWh | tCO2-eşd./MWh |
|---|---|---|
| Dağıtım hattından bağlı tüketim noktası | 0,465 | **0,469** |
| İletim hattından bağlı tüketim noktası | 0,433 | **0,436** |
| Türkiye geneli elektrik üretimi | 0,430 | 0,434 |

Belgeye göre tüketim noktası faktörleri "elektrik tüketimi kaynaklı karbon ayak izi ve azaltım hesaplamalarında kullanılabilecektir". 1 tCO2-eşd./MWh = 1 kgCO2e/kWh olduğundan, dağıtım hattına bağlı tipik bir tesis için katsayı **0,469 kgCO2e/kWh**'tir.

> **Karıştırılmaması gereken belge:** ETKB'nin *Türkiye Ulusal Elektrik Şebekesi Emisyon Faktörü Bilgi Formu* (ETKB-EVÇED-FRM-039; OM/BM/CM değerleri, ör. 2021 için birleşik marj 0,6345) UNFCCC CDM Tool 07 yöntemiyle hesaplanır. Belgenin kendisine göre bu değerler **yenilenebilir enerji üretimiyle sağlanacak sera gazı azaltımı** hesapları içindir; şirketin elektrik tüketiminden kaynaklanan Scope 2 emisyonu için kullanılmamalıdır.

**Doğalgaz ve motorin — IPCC 2006, Cilt 2, Bölüm 2, Tablo 2.2 (net ısıl değer bazında, kg gaz / TJ)**

| Yakıt | CO2 | CH4 | N2O |
|---|---|---|---|
| Doğalgaz (Natural Gas) | 56.100 | 1 | 0,1 |
| Motorin (Gas/Diesel Oil) | 74.100 | 3 | 0,6 |

Bu değerler **enerji birimi (TJ) başınadır**. m³ veya litre başına katsayıya çevirmek için yakıtın ısıl değeri (ör. tedarikçinin faturada belirttiği ısıl değer veya ulusal envanterdeki değer) gerekir; bu dönüşüm henüz yapılmamıştır.

### 5.2 Sistemde şu an kullanılan değerler

Her katsayı kaydı veritabanında kaynağını (`Source`, `SourceUrl`) ve resmi kaynağa dayanıp dayanmadığını (`IsVerified`) saklar. Uygulama, kayıt detayında ve yeni kayıt önizlemesinde bu bilgiyi gösterir: doğrulanmış katsayılar "Resmi katsayı", doğrulanmamışlar "Örnek katsayı — resmi kaynağa dayanmıyor" olarak etiketlenir.

| Kategori | Değer | Yıl | Durum |
|---|---|---|---|
| Elektrik | **0,469 kgCO2e/kWh** | 2023 | **Resmi — doğrulandı.** ETKB-EVÇED-FRM-042, dağıtım hattından bağlı tüketim noktası (bkz. 5.1) |
| Elektrik | 0,5 kgCO2e/kWh | 2026 | Kaynaksız ilk sürüm değeri — **pasif**. Yeni hesaplamalarda kullanılmaz; bu değerle daha önce hesaplanmış kayıtların denetim izi için silinmedi. |
| Doğalgaz | 2,0 kgCO2e/m³ | 2026 | **Yer tutucu — kaynağı yok** |
| Motorin | 2,7 kgCO2e/L | 2026 | **Yer tutucu — kaynağı yok** |

Hesaplama kuralı: bir kaydın katsayısı, kategorideki **aktif** katsayılar arasından faaliyet döneminin yılına eşit veya daha önceki **en güncel** yıla göre seçilir. Bu yüzden 2024–2026 dönemi elektrik kayıtları, yayımlanmış en güncel resmi değer olan 2023 katsayısıyla hesaplanır ve detayda "2023" olarak görünür. 2023'ten önceki dönemler için elektrik katsayısı tanımlı olmadığından sistem kayıt oluşturmaz, açık hata verir (geriye doğru değer uydurulmaz).

Önemli:
- Bu değişiklikten **önce** oluşturulmuş elektrik kayıtları 0,5 katsayısıyla hesaplanmış olarak kalır; her kayıt hesaplandığı katsayıyı saklar. Demoda yalnızca resmi katsayıyla hesaplanmış veri göstermek için demo kayıtları yeniden oluşturulmalıdır.
- Doğalgaz ve motorin değerleri sunumda kullanılacaksa "örnek katsayı" olduğu açıkça belirtilmelidir.
- Katsayılar `POST /api/factors/custom` (SuperAdmin) ile eklenir; bu uç artık **kaynak alanı olmadan katsayı kabul etmez**.

---

## 6. Demo verisi

| İçerik | Açıklama |
|---|---|
| Demo fatura görseli (`sunum_gorselleri/uygulama/00_demo_fatura_kurgusal.png`) | Programatik olarak çizilmiş **kurgusal** bir görseldir. Sağlayıcı adı kurgusaldır ("Örnek Enerji Dağıtım A.Ş."), üzerinde "KURGUSAL ÖRNEK" filigranı ve açıklaması vardır. (İlk testlerde gerçek bir şirket adı taşıyan bir test görseli kullanılmış, taklit izlenimi yaratmaması için silinmiştir.) |
| Demo şirket/tesis/emisyon kayıtları | Test betikleriyle oluşturulmuş kurgusal verilerdir ("Örnek Tekstil A.Ş.", `ornek.test` alan adı). Sunum ekran görüntüleri yalnızca resmi elektrik katsayısıyla hesaplanmış verileri gösterir. |
| Sunum görselleri (logolar, fotoğraflar) | Kaynak ve lisansları: `sunum_gorselleri/GORSEL_KAYNAKLARI.md` |

---

## 7. Geliştirme sürecinde yapay zeka kullanımı

Kodun ve bu belgenin bir kısmı yapay zeka destekli kodlama aracıyla (Claude Code, Anthropic) hazırlanmıştır. Şartname gereği üretilen içeriğin doğruluğunu kontrol etmek takımın sorumluluğundadır.
