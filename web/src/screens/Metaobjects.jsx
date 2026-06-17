import React, { useEffect, useState } from 'react'
import { Button, Dialog, Field, Input } from '../ds'
import { I } from './icons.jsx'
import { PageHeader } from './PageHeader.jsx'
import { api } from '../lib/api.js'

export function Metaobjects({ onToast }) {
  const [defs, setDefs] = useState([])
  const [sel, setSel] = useState(null)
  const [fields, setFields] = useState([])
  const [entries, setEntries] = useState([])
  const [defOpen, setDefOpen] = useState(false)
  const [entryOpen, setEntryOpen] = useState(false)

  const loadDefs = () => api.listMetaDefs().then((d) => { setDefs(d); if (!sel && d.length) setSel(d[0].id) }).catch(() => {})
  useEffect(loadDefs, [])
  useEffect(() => {
    if (!sel) return
    api.listMetaFields(sel).then(setFields).catch(() => setFields([]))
    api.listMetaEntries(sel).then(setEntries).catch(() => setEntries([]))
  }, [sel])

  const active = defs.find((d) => d.id === sel)

  const parseValues = (raw) => { try { return typeof raw === 'string' ? JSON.parse(raw) : (raw || {}) } catch { return {} } }

  return (
    <div className="page">
      <PageHeader eyebrow="Tanımlar" title="Metaobject'ler" sub="Yapılandırılmış değer kümeleri — Renk, Beden, Materyal."
        actions={<Button variant="accent" iconLeft={I('plus')} onClick={() => setDefOpen(true)}>Tanım ekle</Button>} />
      <div className="split">
        <div className="tree">
          {defs.map((m) => (
            <div key={m.id} className="tree__node" data-active={sel === m.id} onClick={() => setSel(m.id)}>
              {I('boxes')}<span>{m.label}</span>
            </div>
          ))}
          {defs.length === 0 && <div className="list-meta" style={{ padding: 12 }}>Tanım yok.</div>}
        </div>
        {active && (
          <div className="pim-card">
            <div className="pim-card__header">
              <div className="hstack"><span className="pim-card__title">{active.label}</span><span className="typechip">{active.key}</span></div>
              <Button variant="secondary" size="sm" iconLeft={I('plus')} onClick={() => setEntryOpen(true)}>Kayıt ekle</Button>
            </div>
            <div className="pim-card__body">
              <div className="list-meta" style={{ marginBottom: 12 }}>Alanlar: {fields.map((f) => <span className="typechip" key={f.id} style={{ marginRight: 4 }}>{f.key} · {f.data_type}</span>)}{fields.length === 0 && '—'}</div>
              <div className="pim-table-wrap">
                <table className="pim-table">
                  <thead><tr>{fields.map((f) => <th key={f.id}>{f.label}</th>)}<th></th></tr></thead>
                  <tbody>
                    {entries.map((e) => {
                      const v = parseValues(e.values)
                      return (
                        <tr key={e.id}>
                          {fields.map((f) => (
                            <td key={f.id} className={f.data_type === 'color' ? 'pim-td-mono' : ''}>
                              <div className="cellrow">
                                {(f.data_type === 'color' || f.key === 'hex') && v[f.key] && <span className="swatch-sm" style={{ background: v[f.key] }}></span>}
                                <span className={f.key === 'ad' ? 'pim-td-strong' : ''}>{v[f.key] != null ? String(v[f.key]) : '—'}</span>
                              </div>
                            </td>
                          ))}
                          <td><div className="rowact"><button className="tb__icon" style={{ width: 28, height: 28 }} title="Sil" onClick={async () => { await api.deleteMetaEntry(e.id); setEntries(entries.filter((x) => x.id !== e.id)) }}>{I('trash-2')}</button></div></td>
                        </tr>
                      )
                    })}
                    {entries.length === 0 && <tr><td colSpan={fields.length + 1} className="subtle" style={{ padding: 14 }}>Kayıt yok.</td></tr>}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}
      </div>

      <DefDialog open={defOpen} onClose={() => setDefOpen(false)}
        onCreate={async (body) => { try { await api.createMetaDef(body); setDefOpen(false); loadDefs(); onToast?.({ tone: 'success', title: 'Tanım eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />

      <EntryDialog open={entryOpen} fields={fields} onClose={() => setEntryOpen(false)}
        onCreate={async (values) => { try { await api.createMetaEntry(sel, values); setEntryOpen(false); api.listMetaEntries(sel).then(setEntries); onToast?.({ tone: 'success', title: 'Kayıt eklendi' }) } catch (e) { onToast?.({ tone: 'danger', title: 'Eklenemedi', body: e.message }) } }} />
    </div>
  )
}

function DefDialog({ open, onClose, onCreate }) {
  const [key, setKey] = useState('')
  const [label, setLabel] = useState('')
  useEffect(() => { if (open) { setKey(''); setLabel('') } }, [open])
  return (
    <Dialog open={open} title="Tanım ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => key.trim() && label.trim() && onCreate({ key: key.trim(), label: label.trim() })}>
      <Field label="Key" required><Input mono value={key} onChange={(e) => setKey(e.target.value)} placeholder="materyal" /></Field>
      <Field label="Etiket" required><Input value={label} onChange={(e) => setLabel(e.target.value)} placeholder="Materyal" /></Field>
    </Dialog>
  )
}

function EntryDialog({ open, fields, onClose, onCreate }) {
  const [vals, setVals] = useState({})
  useEffect(() => { if (open) setVals({}) }, [open])
  return (
    <Dialog open={open} title="Kayıt ekle" confirmLabel="Ekle" cancelLabel="İptal" onClose={onClose}
      onConfirm={() => onCreate(vals)}>
      {fields.map((f) => (
        <Field key={f.id} label={`${f.label} (${f.data_type})`}>
          <Input value={vals[f.key] || ''} onChange={(e) => setVals((s) => ({ ...s, [f.key]: e.target.value }))} placeholder={f.key} />
        </Field>
      ))}
      {fields.length === 0 && <div className="list-meta">Önce bu tanıma alan eklemelisin.</div>}
    </Dialog>
  )
}
