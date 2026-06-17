Feedback & messaging for pimly — all tied to the calm, factual content voice.

```jsx
<Banner tone="warning" title="Salt-okunur mod">
  readonly rolündesin; değişiklik yapamazsın.
</Banner>
<Banner tone="danger" title="Kayıt başarısız">
  3 zorunlu alan eksik. İşaretli alanları doldur.
</Banner>

<Toast tone="success" title="Ürün kaydedildi" onClose={()=>{}}>
  4 ürün, 18 varyant oluşturuldu.
</Toast>

<Tooltip label="Diğer işlemler"><IconButton icon={<i data-lucide="more-horizontal"/>} label="Diğer" /></Tooltip>

<EmptyState icon="package" title="Henüz ürün grubu yok"
  description="İlk grubunu oluştur; ürünler ve varyantlar tek formda eklenir."
  action={<Button variant="accent">Ürün Oluştur</Button>} />
```

- `Banner.tone`: `info` · `success` · `warning` · `danger`. Use `danger` for API error-envelope messages.
- `Toast` is transient; you own the timeout + the fixed stack container.
- `EmptyState` always states what goes here + the single next step (no fluff).
- `Tooltip` is required on every icon-only control.
