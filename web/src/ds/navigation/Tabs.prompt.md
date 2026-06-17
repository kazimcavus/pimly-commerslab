Underline tabs for switching sections within a page (group detail, settings).

```jsx
const [tab, setTab] = React.useState('urunler');
<Tabs value={tab} onChange={setTab} tabs={[
  { value: 'urunler', label: 'Ürünler', icon: 'package', count: 4 },
  { value: 'medya', label: 'Medya', icon: 'image', count: 12 },
  { value: 'pazaryeri', label: 'Pazaryeri', icon: 'store' },
]} />
```

- Controlled: pass `value` + `onChange`. `tabs[].icon` is a Lucide name; `count` shows a neutral count badge.
- The active tab gets an ink underline and semibold weight.
