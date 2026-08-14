# UI Geri Bildirim Kuralları — modal, toast, hata, ayrılma koruması

Yeni bir sayfa/akış eklerken bu kurallara uy. Amaç: **hiçbir yerde tarayıcının
kendi pencereleri görünmez**, tüm geri bildirim uygulamanın kendi dilinde ve
görsel sistemindedir.

## 1. Tarayıcı pencereleri yasak

`window.confirm()`, `window.alert()`, `window.prompt()` **kullanma**. Tek
istisna: sekme kapatma/yenilemedeki `beforeunload` uyarısı (tarayıcı özel
modala teknik olarak izin vermez; `App.jsx` bunu merkezî olarak yapar).

Onay gerektiren her işlem için uygulama içi modal kullan:

```jsx
import { askConfirm } from '../lib/confirm.jsx'   // parts/ altından: '../../lib/confirm.jsx'

const ok = await askConfirm({
  title: 'Varyantı sil',                    // kısa, eylem odaklı başlık
  body: `"${label}" varyantı, fiyat ve stok kayıtlarıyla birlikte kalıcı olarak silinecek.`,
  tone: 'danger',                           // yıkıcı işlemlerde zorunlu
  confirmLabel: 'Sil',                      // düğme eylemi söyler ("Evet" değil)
  cancelLabel: 'Vazgeç',                    // varsayılan: Vazgeç
})
if (!ok) return
```

Altyapı: `ConfirmHost` `App.jsx` kökünde bir kez render edilir; `askConfirm`
promise döndürür, Esc ve scrim tıklaması "vazgeç" sayılır.

## 2. Silme = her zaman onay modali

Kalıcı veri silen **her** işlem (`api.delete*` çağrısı yapan her akış) önce
`askConfirm` ile sorar — satır içi çöp ikonu dâhil. Modal gövdesi:

- **Neyin** silineceğini adıyla söyler (`"Bej" değeri…`),
- **yan etkiyi** söyler (…"ürünlerdeki seçimleri etkilenebilir"),
- geri alınamazsa bunu belirtir.

Bir düzenleme çekmecesinde satır kaldırıp **Kaydet**'e basmak da silmedir:
kaydetmeden önce kaldırılan kayıtları listeleyip onay iste (bkz.
`Variants.jsx` / `Attributes.jsx` `submit`).

## 3. Toast'lar (sağ alt bildirimler)

`onToast` her ekrana prop olarak gelir; gövdeye asla ham `e.message` yazma:

```jsx
onToast?.({ tone: 'success', title: 'Ürün kaydedildi' })                 // kısa ve net
onToast?.({ tone: 'danger', title: 'Kaydedilemedi', error: e })          // hata NESNESİ geç
```

- `error: e` geçilirse `App.jsx` gövdeyi `lib/errors.js → friendlyError(e)` ile
  kullanıcı dostu Türkçeye çevirir (ağ hatası, 401/404/409/500 eşlemeleri,
  RFC7807 alan hataları). Yeni durum eşlemeleri `lib/errors.js`'e eklenir.
- Başarı başlıkları geçmiş zamanlı ve kısadır: "Ürün silindi", "Görsel eklendi".
- Hata toast'ları daha uzun görünür (App bunu tone'dan ayarlar); süreyle oynama.

Form içi (sayfada kalan) hatalar için toast değil `Banner` kullan; gövdesini
yine `friendlyError(e)` ile üret.

## 4. Kaydedilmemiş değişiklik koruması

Formu olan her ekran, kirliyken ayrılmayı korur — sidebar navigasyonu,
tarayıcı geri/ileri ve sekme kapatma dâhil. Ekran yalnızca guard kaydeder;
sorma işini `App.jsx` yapar:

```jsx
import { registerNavGuard } from '../lib/navGuard.js'

const dirtyRef = useRef(false)
useEffect(() => registerNavGuard(() => dirtyRef.current), [])
dirtyRef.current = anyDirty   // her render'da güncel kirlilik durumu
```

- Kayıt başarılıysa navigasyondan **önce** `dirtyRef.current = false` yap
  (yoksa kendi yönlendirmen modalı tetikler).
- Otomatik doldurulan alanlar (ör. barkod tahsisi) kirlilik **sayılmaz** —
  kullanıcının kendi girdiği veri sayılır.
- Sayfadaki açık "İptal / geri al" düğmesi de değişiklikleri atmadan önce
  `askConfirm` ile sorar.

## 5. Özet kontrol listesi (yeni sayfa eklerken)

- [ ] Hiç `confirm/alert/prompt` yok; onaylar `askConfirm` ile.
- [ ] Her `api.delete*` akışı modal onaylı; gövde neyi/yan etkiyi söylüyor.
- [ ] Toast'larda `error: e` deseni; ham `e.message` yok.
- [ ] Banner gövdeleri `friendlyError(e)`'den geliyor.
- [ ] Form varsa `registerNavGuard` kayıtlı; kayıt sonrası guard sıfırlanıyor.
