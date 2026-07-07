// Bağlamsal yardım içeriği. Her anahtar bir ⓘ ipucu / yardım çekmecesini besler.
// Dil: kullanıcı odaklı Türkçe — yeni başlayan da anlasın, profesyonel de.
// İleride her başlığa gerçek video (videoUrl) eklenecek; şimdilik yer tutucu.
export const HELP = {
  'sku-generator': {
    eyebrow: 'Ayarlar',
    title: 'Ürün Kodu Oluşturucu',
    lead: 'Ürün kodu (SKU), bir ürünü tek başına tanımlayan benzersiz koddur. Burada bir şablon kurarsanız, ürün eklerken kodlar otomatik üretilir — her seferinde elle yazmazsınız.',
    video: 'Ürün kodu şablonu nasıl kurulur? (2 dk)',
    steps: [
      'Segment ekleyin: her segment kodun bir parçasıdır (firma kodu, yıl, sıra no, renk, beden…).',
      'Segmentleri sürükleyerek sıralayın — kod soldan sağa bu sırayla oluşur.',
      '“Renk” ve “Beden” segmentleri yalnızca varyant koduna eklenir, ana ürün koduna değil.',
      'Önizlemede ürün kodunun ve varyant kodunun nasıl görüneceğini anında görün, sonra Kaydet.',
    ],
    tips: [
      'Sıralı sayaç her yeni üründe otomatik artar; hane sayısıyla sıfır doldurulur (0001).',
      '“Elle girilir” segmenti, o ürüne özel bir değeri (örn. sezon) kayıt sırasında sormak için.',
    ],
  },
  barcode: {
    eyebrow: 'Ayarlar',
    title: 'Barkod (EAN-13)',
    lead: 'Barkod, ürünün fiziksel etiketinde okunan 13 haneli numaradır. Bir başlangıç numarası belirleyin; sistem her yeni ürüne sıradaki barkodu otomatik verir ve kontrol hanesini ekler.',
    video: 'Barkod serisi nasıl çalışır? (1 dk)',
    steps: [
      '“Sonraki numara” alanına serinizin başlayacağı numarayı yazın.',
      'Sistem her tahsiste numarayı 13 haneye tamamlar ve doğrulama (kontrol) hanesini ekler.',
      'Kaydedin — bundan sonra eklenen ürünler bu seriden barkod alır.',
    ],
    tips: [
      'Kendi GS1 numara aralığınız varsa oradan başlatın; yoksa dahili bir seri yeterlidir.',
      '“İstemci tahsisi zorunlu” açıksa barkodlar otomatik atanmaz; ürün eklerken elle girersiniz.',
    ],
  },
  categories: {
    eyebrow: 'Tanımlar',
    title: 'Kategoriler',
    lead: 'Kategoriler ürünlerinizi gruplar (örn. Giyim › Tişört). Bir kategoriye özellik atayınca, o kategorideki ürünleri eklerken bu özellikler otomatik karşınıza gelir.',
    video: 'Kategori ağacı ve özellik atama (2 dk)',
    steps: [
      'Bir kategori ekleyin; gerekiyorsa alt kategori oluşturun (ağaç yapısı).',
      'Kategoriyi seçip ona özellikler atayın (örn. Kumaş, Yaka tipi).',
      'Zorunlu özellikleri işaretleyin — ürün eklerken bunlar doldurulmadan kayıt tamamlanmaz.',
    ],
    tips: ['İyi kurgulanmış kategoriler, pazaryeri eşlemesini ileride çok kolaylaştırır.'],
  },
  attributes: {
    eyebrow: 'Tanımlar',
    title: 'Özellikler',
    lead: 'Özellikler, ürünleri zenginleştiren bilgilerdir (Kumaş, Menşei, Garanti…). Bir kez tanımlarsınız, değerlerini ekler ve kategorilere/ürünlere atarsınız.',
    video: 'Özellik ve değer nasıl tanımlanır? (2 dk)',
    steps: [
      'Bir özellik oluşturun (örn. “Kumaş”).',
      'O özelliğe seçilebilir değerler ekleyin (örn. Pamuk, Polyester).',
      'Özelliği kategorilere atayın; ürün eklerken değer seçilir.',
    ],
    tips: ['Tutarlı özellikler, müşterinin doğru ürünü bulmasını ve pazaryeri onayını kolaylaştırır.'],
  },
  brands: {
    eyebrow: 'Tanımlar',
    title: 'Markalar',
    lead: 'Markalar ürünlerinizin üreticisini/etiketini belirtir (örn. Nike, Adidas). Bir kez tanımlarsınız, ürün açarken ya da ürün detayında seçersiniz.',
    video: 'Marka nasıl tanımlanır? (1 dk)',
    steps: [
      'Sağ üstten “Marka ekle” ile yeni bir marka oluşturun (örn. Nike).',
      'İsterseniz markaya kısa bir kod verin (opsiyonel — örn. NKE).',
      'Ürün oluştururken ya da ürün detayında markayı seçin.',
    ],
    tips: ['Tutarlı marka adları, pazaryeri eşlemesini ve raporlamayı kolaylaştırır.'],
  },
  prices: {
    eyebrow: 'Tanımlar',
    title: 'Fiyatlar',
    lead: 'Fiyat alanları, aynı ürüne birden fazla fiyat girmenizi sağlar — örn. "TY Satış", "TY Karşılaştırma", "Toptan fiyat". Bir kez tanımlarsınız; ürün eklerken ve ürün detayında her varyanta bu alanlara tutar girersiniz. Genel (site) fiyatı ayrıdır ve her zaman vardır.',
    video: 'Fiyat alanları nasıl çalışır? (1 dk)',
    steps: [
      'Sağ üstten "Fiyat tanımı ekle" ile yeni bir alan oluşturun (örn. "Toptan fiyat").',
      'İsterseniz kısa bir kod verin (opsiyonel — örn. ty_sale).',
      'Ürün oluştururken tutar girin (tüm varyantlara uygulanır) ya da ürün detayında varyant bazında düzenleyin.',
    ],
    tips: [
      'Trendyol import\'u "TY Satış" ve "TY Karşılaştırma" alanlarını otomatik oluşturur ve çekilen fiyatlarla doldurur.',
      'Bir tanımı silerseniz ürünlerde o alana girilmiş fiyatlar da silinir.',
    ],
  },
  variants: {
    eyebrow: 'Tanımlar',
    title: 'Varyantlar',
    lead: 'Varyant, aynı ürünün farklı seçenekleridir — Renk, Beden gibi. Önce varyant tiplerini ve değerlerini tanımlarsınız; ürün oluştururken bunları seçip kombinasyonları otomatik üretirsiniz.',
    video: 'Varyant tipi ve değerleri (2 dk)',
    steps: [
      'Bir varyant tipi ekleyin (örn. Renk veya Beden).',
      'Değerlerini girin (Renk için renk seçici, Beden için S/M/L…).',
      'İstersen her değere kısa bir kod (key) verin — örn. Kırmızı → R08. Opsiyoneldir.',
      'Ürün oluştururken tipleri seçin; tüm kombinasyonlar (Kırmızı-M, Mavi-L…) otomatik oluşur.',
    ],
    tips: [
      'Değer kodu (key) opsiyoneldir: doluysa ürün kodu üreticisi onu kullanır (örn. R08).',
      'Kodu boş bırakırsan ürün kodunda değerin adı otomatik kullanılır — ayrıca uğraşmana gerek yok.',
    ],
  },
  products: {
    eyebrow: 'Katalog',
    title: 'Ürünler',
    lead: 'Tüm kataloğunuz burada. Ürünleri model bazında görür, durumlarını (taslak/aktif/arşiv) takip eder ve yeni ürün eklersiniz.',
    video: 'Ürün listesini yönetmek (1 dk)',
    steps: [
      'Sağ üstten “Ürün Oluştur” ile yeni ürün ekleyin.',
      'Durum sekmeleriyle filtreleyin; arama kutusuyla ad veya model koduyla bulun.',
      'Slicer’lı (örn. renk renk ayrılan) ürünler aynı model altında gruplanır.',
    ],
    tips: ['Taslak ürünler hazır olana kadar bekler; aktif ettiğinizde yayına hazır sayılır.'],
  },
  'product-builder': {
    eyebrow: 'Katalog',
    title: 'Ürün Oluştur',
    lead: 'Tek bir formdan ürünü baştan sona kurarsınız: temel bilgiler, ürün tipi (basit veya varyantlı) ve kategoriye göre özellikler. Hepsi tek kayıtta oluşturulur.',
    video: 'Adım adım ürün oluşturma (3 dk)',
    steps: [
      '1 · Temel: başlık, kategori ve durum. Ürün kodu üretici açıksa kod otomatik gelir.',
      '2 · Tip: “Basit” tek SKU; “Varyantlı” ise tip seçip değerleri işaretleyin, satırlar otomatik üretilir.',
      '3 · Özellikler: seçtiğiniz kategorinin özelliklerini doldurun (zorunlular işaretli).',
      'Fiyat, stok ve gerekiyorsa barkodu girin; Kaydet ile tüm ağaç oluşur.',
    ],
    tips: [
      '“Ayraç” (slicer) bir varyant tipini her değeri ayrı ürün olacak şekilde böler (örn. her renk ayrı ürün).',
      'Barkod üreticisi açıksa varyant satırı eklediğinizde barkodlar otomatik ayrılır ve alanda görünür; SKU alanı şablondan otomatik dolar.',
      'Kategori seçimi zorunludur — özellikler ve pazaryeri eşlemeleri kategoriye bağlanır.',
    ],
  },
  'channels': {
    eyebrow: 'Pazaryerleri',
    title: 'Pazaryerleri',
    lead: 'Pazaryeri hesaplarınızı bağlar, ürünlerinizi Pimly kataloğuna aktarır ve import geçmişini izlersiniz. Gönderim (v2) için gereken kategori/özellik eşlemeleri import sırasında otomatik kurulur.',
    video: 'Trendyol bağlama ve içe aktarma (2 dk)',
    steps: [
      '“Bağla ve içe aktar” ile Trendyol kurulum sihirbazını açın.',
      'Import bitince geçmişte sayaçları görürsünüz: aktarılan, atlanan (zaten vardı), hatalı.',
      'Yeni ürünleriniz olduğunda “Yeniden içe aktar” ile tekrar çekebilirsiniz; mevcut ürünler atlanır.',
    ],
    tips: ['Import bir arka plan işidir — ekranı kapatsanız da devam eder; buradan durumunu izleyebilirsiniz.'],
  },
  'trendyol-import': {
    eyebrow: 'Pazaryerleri',
    title: 'Trendyol Kurulumu',
    lead: 'Trendyol mağazanızı bağlar ve tüm ürünlerinizi tek seferde Pimly kataloğuna aktarır. Kategoriler, özellikler ve varyantlar (Renk, Beden…) otomatik tanımlanır; Renk varsayılan olarak ayraçtır (her renk ayrı ürün kartı).',
    video: 'Trendyol’dan ürün çekme (3 dk)',
    steps: [
      'Trendyol Satıcı Paneli → Hesap Bilgileri → Entegrasyon Bilgileri sayfasından Satıcı ID, API Key ve API Secret’ı kopyalayın.',
      'Bilgileri forma girip “Bağlan ve devam et”e tıklayın.',
      'Kategori eşitleme ve ürün aktarımı otomatik ilerler; ilerlemeyi ekrandan izleyin.',
      'Özet ekranında sayıları kontrol edin; “Ürünlerime git” ile katalogda zenginleştirmeye başlayın.',
    ],
    tips: [
      'Fiyatlar hem genel (site) fiyatı hem de “TY Satış” / “TY Karşılaştırma” fiyat alanları olarak kaydedilir (bkz. Tanımlar → Fiyatlar).',
      'Aynı barkod/model kodu zaten kataloğunuzdaysa o ürün atlanır; iki kez içe aktarmak güvenlidir.',
    ],
  },
}
