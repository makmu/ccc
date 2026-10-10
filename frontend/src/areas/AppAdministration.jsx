import ActionDialog, { Field } from '../ActionDialog'
import { command, newId } from '../api'
import QueryView from '../QueryView'
import { useQuery } from '../useQuery'

export default function AppAdministration() {
  const subscriptionModels = useQuery('/subscription-models')
  const tenants = useQuery('/tenants')
  const users = useQuery('/users')

  return (
    <>
      <SubscriptionModels subscriptionModels={subscriptionModels} />
      <Tenants tenants={tenants} subscriptionModels={subscriptionModels.data ?? []} users={users.data ?? []} />
      <Users users={users} />
    </>
  )
}

function SubscriptionModels({ subscriptionModels }) {
  async function add(form) {
    const id = newId()
    await command('/subscription-models', {
      id,
      name: form.get('name'),
      keyLimit: Number(form.get('keyLimit')),
    })
    subscriptionModels.reloadUntil(list => list.some(model => model.id === id))
  }

  return (
    <section className="section">
      <header className="section-header">
        <h2>Subscription models</h2>
        <ActionDialog label="Add subscription model" onRun={add}>
          <Field label="Name"><input name="name" required /></Field>
          <Field label="Key limit"><input name="keyLimit" type="number" min="1" required /></Field>
        </ActionDialog>
      </header>
      <QueryView result={subscriptionModels} empty="No subscription models yet. Add one to offer it to tenants.">
        {list => (
          <table>
            <thead><tr><th>Name</th><th>Key limit</th></tr></thead>
            <tbody>
              {list.map(model => (
                <tr key={model.id}><td>{model.name}</td><td>{model.keyLimit}</td></tr>
              ))}
            </tbody>
          </table>
        )}
      </QueryView>
    </section>
  )
}

function Tenants({ tenants, subscriptionModels, users }) {
  async function register(form) {
    const id = newId()
    await command('/tenants', {
      id,
      name: form.get('name'),
      subscriptionModelIds: form.getAll('subscriptionModelIds'),
    })
    tenants.reloadUntil(list => list.some(tenant => tenant.id === id))
  }

  async function assignOwner(tenant, form) {
    const userId = form.get('userId')
    await command(`/tenants/${tenant.id}/owners`, { userId })
    const owner = users.find(user => user.id === userId)?.name
    tenants.reloadUntil(list => list.find(t => t.id === tenant.id)?.owners.includes(owner))
  }

  return (
    <section className="section">
      <header className="section-header">
        <h2>Tenants</h2>
        <ActionDialog label="Register tenant" onRun={register}>
          <Field label="Name"><input name="name" required /></Field>
          <fieldset className="choices">
            <legend>Subscription models</legend>
            {subscriptionModels.length === 0 && <p className="quiet">Add a subscription model first.</p>}
            {subscriptionModels.map(model => (
              <label key={model.id}>
                <input type="checkbox" name="subscriptionModelIds" value={model.id} /> {model.name}
              </label>
            ))}
          </fieldset>
        </ActionDialog>
      </header>
      <QueryView result={tenants} empty="No tenants yet. Register the first one.">
        {list => (
          <table>
            <thead><tr><th>Name</th><th>Subscription models</th><th>Owners</th><th /></tr></thead>
            <tbody>
              {list.map(tenant => (
                <tr key={tenant.id}>
                  <td>{tenant.name}</td>
                  <td>{tenant.subscriptionModels.join(', ')}</td>
                  <td>{tenant.owners.length ? tenant.owners.join(', ') : <span className="quiet">None</span>}</td>
                  <td className="row-actions">
                    <ActionDialog
                      label="Assign owner"
                      title={`Assign an owner to ${tenant.name}`}
                      className="secondary"
                      onRun={form => assignOwner(tenant, form)}
                    >
                      <Field label="User">
                        <select name="userId" required defaultValue="">
                          <option value="" disabled>Choose a user</option>
                          {users.map(user => <option key={user.id} value={user.id}>{user.name}</option>)}
                        </select>
                      </Field>
                    </ActionDialog>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </QueryView>
    </section>
  )
}

function Users({ users }) {
  async function add(form) {
    const id = newId()
    await command('/users', { id, name: form.get('name'), email: form.get('email') })
    users.reloadUntil(list => list.some(user => user.id === id))
  }

  return (
    <section className="section">
      <header className="section-header">
        <h2>Users</h2>
        <ActionDialog label="Add user" onRun={add}>
          <Field label="Name"><input name="name" required /></Field>
          <Field label="Email"><input name="email" type="email" required /></Field>
        </ActionDialog>
      </header>
      <QueryView result={users} empty="No users yet. Add one to make them a tenant owner later.">
        {list => (
          <table>
            <thead><tr><th>Name</th><th>Email</th></tr></thead>
            <tbody>
              {list.map(user => (
                <tr key={user.id}><td>{user.name}</td><td>{user.email}</td></tr>
              ))}
            </tbody>
          </table>
        )}
      </QueryView>
    </section>
  )
}
