Form primitives for pimly's dynamic attribute forms — every control is built to slot into `Field`.

```jsx
<Field label="Başlık" required help="Pazaryerinde görünen ad">
  <Input placeholder="Örn. Basic Tişört" />
</Field>

<Field label="SKU" auto="Boş bırakılırsa otomatik üretilecek">
  <Input mono placeholder="TS-0001-R01" />
</Field>

<Field label="Fiyat" required>
  <Input mono suffix="₺" inputMode="decimal" />
</Field>

<Field label="Kategori"><Select placeholder="Seç…" options={[{value:'1',label:'Tişört'}]} /></Field>

<Checkbox label="Zorunlu alan" hint="Aktife geçerken doldurulmalı" />
<Switch label="Aktif" defaultChecked />
```

- `Field` owns label, `required`/`optional`, `help`, `error`, and the `auto` hint. Pass `error` to flip the wrapped control into its invalid styling (set `invalid` on the control too).
- `Input` supports `icon`, `suffix`, and `mono` (SKU/barcode/price).
- `Select` takes `options=[{value,label}]` or `<option>` children, with `placeholder`.
- `Checkbox`/`Radio` accept `label` + `hint`; `Switch` is for flags/quick toggles.
