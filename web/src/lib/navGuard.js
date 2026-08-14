// Kaydedilmemiş değişiklik koruması (bkz. web/docs/ui-feedback.md).
// Kirli formu olan ekran mount olurken bir guard kaydeder; App her ekran
// geçişinde (navigate, tarayıcı geri/ileri) guard'lara sorar ve kirliyse
// onay modali açar. Sekme kapatma/yenilemede beforeunload devreye girer.
//
//   useEffect(() => registerNavGuard(() => dirtyRef.current), [])
//
// Guard true dönerse "kaydedilmemiş değişiklik var" demektir.
const guards = new Set()

export function registerNavGuard(fn) {
  guards.add(fn)
  return () => guards.delete(fn)
}

export function hasUnsavedChanges() {
  for (const g of guards) {
    try { if (g()) return true } catch { /* guard hatası engel olmasın */ }
  }
  return false
}
