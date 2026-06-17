Centered modal dialog for confirmations and small forms.

```jsx
const [open, setOpen] = React.useState(false);
<Dialog
  open={open}
  tone="danger"
  title="Grubu sil?"
  description="Bu grup, içindeki 4 ürün ve 18 varyant kalıcı olarak silinecek."
  confirmLabel="Kalıcı sil"
  onConfirm={() => setOpen(false)}
  onClose={() => setOpen(false)}
/>
```

- Controlled by `open`. Clicking the scrim or cancel calls `onClose`.
- `tone="danger"` makes the confirm button destructive; `busy` shows a spinner.
- Pass `children` for small forms (e.g. add field, status change). Footer is built in.
