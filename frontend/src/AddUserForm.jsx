import { useState } from 'react'
import { addUser } from './api'

function randomUuid() {
  return crypto.randomUUID()
}

export default function AddUserForm() {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [status, setStatus] = useState(null)

  async function handleSubmit(e) {
    e.preventDefault()
    setStatus(null)
    try {
      await addUser({ id: randomUuid(), name, email })
      setStatus({ ok: true, message: 'User added successfully.' })
      setName('')
      setEmail('')
    } catch (err) {
      setStatus({ ok: false, message: err.message })
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form-card">
      <h2>Add User</h2>
      <label>
        Name
        <input
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          required
          placeholder="Alice"
        />
      </label>
      <label>
        Email
        <input
          type="email"
          value={email}
          onChange={e => setEmail(e.target.value)}
          required
          placeholder="alice@example.com"
        />
      </label>
      <button type="submit">Add User</button>
      {status && (
        <p className={status.ok ? 'msg-ok' : 'msg-err'}>{status.message}</p>
      )}
    </form>
  )
}
