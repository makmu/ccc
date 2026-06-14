import { useState } from 'react'
import { registerTenant } from './api'

function randomUuid() {
  return crypto.randomUUID()
}

export default function RegisterTenantForm() {
  const [name, setName] = useState('')
  const [subIds, setSubIds] = useState([''])
  const [status, setStatus] = useState(null)

  function updateSubId(index, value) {
    setSubIds(ids => ids.map((id, i) => (i === index ? value : id)))
  }

  function addSubIdField() {
    setSubIds(ids => [...ids, ''])
  }

  function removeSubIdField(index) {
    setSubIds(ids => ids.filter((_, i) => i !== index))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setStatus(null)
    const subscriptionModelIds = subIds.filter(id => id.trim() !== '')
    if (subscriptionModelIds.length === 0) {
      setStatus({ ok: false, message: 'At least one subscription model ID is required.' })
      return
    }
    try {
      await registerTenant({ id: randomUuid(), name, subscriptionModelIds })
      setStatus({ ok: true, message: 'Tenant registered successfully.' })
      setName('')
      setSubIds([''])
    } catch (err) {
      setStatus({ ok: false, message: err.message })
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form-card">
      <h2>Register Tenant</h2>
      <label>
        Name
        <input
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          required
          placeholder="Acme Corp"
        />
      </label>
      <fieldset>
        <legend>Subscription Model IDs</legend>
        {subIds.map((id, i) => (
          <div key={i} className="sub-id-row">
            <input
              type="text"
              value={id}
              onChange={e => updateSubId(i, e.target.value)}
              placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
              pattern="[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
              title="UUID format required"
            />
            {subIds.length > 1 && (
              <button type="button" className="btn-remove" onClick={() => removeSubIdField(i)}>
                ✕
              </button>
            )}
          </div>
        ))}
        <button type="button" className="btn-add" onClick={addSubIdField}>
          + Add ID
        </button>
      </fieldset>
      <button type="submit">Register Tenant</button>
      {status && (
        <p className={status.ok ? 'msg-ok' : 'msg-err'}>{status.message}</p>
      )}
    </form>
  )
}
