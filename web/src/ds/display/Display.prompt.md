Status, metadata, and identity display for pimly tables and headers.

```jsx
<Badge status="draft">Taslak</Badge>
<Badge status="active">Aktif</Badge>
<Badge status="archived">Arşiv</Badge>
<Badge count>12 varyant</Badge>

<Tag swatch="#d7382b" onRemove={() => {}}>Kırmızı</Tag>
<Tag>S</Tag>

<Avatar name="Acme Owner" />
```

- `Badge status`: `draft` (gri) · `active` (yeşil) · `archived` (sarı) · `danger` · `info` — always dot + word. `count` renders a neutral monospace count.
- `Tag` is for metaobject values and selected filters; `swatch` shows a Renk hex chip, `onRemove` adds the × button.
- `Avatar` derives initials from `name`, or shows `src`. Sizes `sm`/`md`/`lg`.
