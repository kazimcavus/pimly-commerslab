Ink-primary / emerald-accent button set for pimly actions, plus matching icon-only buttons.

```jsx
<Button variant="accent" iconLeft={<i data-lucide="plus" />}>Ürün Oluştur</Button>
<Button variant="primary">Kaydet</Button>
<Button variant="secondary">İptal</Button>
<Button variant="ghost" size="sm">Daha fazla</Button>
<Button variant="danger" iconLeft={<i data-lucide="trash-2" />}>Sil</Button>
<IconButton icon={<i data-lucide="more-horizontal" />} label="Diğer işlemler" />
```

- `variant`: `primary` (ink — main commit), `accent` (emerald — affirmative create), `secondary`, `ghost`, `danger`, `danger-solid`.
- `size`: `sm` (28px) · `md` (34px, default) · `lg` (40px).
- `loading` shows a spinner and disables. `iconLeft`/`iconRight` take any node (Lucide icons recommended).
- Use exactly one `primary` per view. `accent` is for the hero create action; everything else is `secondary`/`ghost`.
