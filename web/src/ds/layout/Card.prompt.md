Bordered white surface — the default content container across pimly.

```jsx
<Card>
  <CardHeader title="Grup bilgileri" actions={<Button variant="ghost" size="sm">Düzenle</Button>} />
  <CardBody>…</CardBody>
</Card>

<Card pad>Basit padded kutu</Card>
```

- Flat by default: 1px border, no shadow. Reserve elevation for overlays.
- `CardHeader` has a title + right-aligned `actions`; `CardBody` is the padded region. Use `pad` on `Card` for a one-off simple box.
