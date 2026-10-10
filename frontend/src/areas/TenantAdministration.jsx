import ActionDialog, { Field } from '../ActionDialog'
import { command, newId } from '../api'
import QueryView from '../QueryView'
import { useScope } from '../scope'
import { navigate } from '../useHashRoute'
import { useQuery } from '../useQuery'

export default function TenantAdministration() {
  const { scope, lists } = useScope()
  const details = useQuery(`/tenants/${scope.tenant}`)

  return (
    <>
      <Tenant details={details} tenants={lists.tenant} />
      <Teams teams={lists.team} subscriptionModels={details.data?.subscriptionModels ?? []} />
    </>
  )
}

function Tenant({ details, tenants }) {
  const { scope } = useScope()

  async function rename(form) {
    const name = form.get('name').trim()
    await command(`/tenants/${scope.tenant}/rename`, { name })
    details.reloadUntil(tenant => tenant.name === name)
    tenants.reloadUntil(list => list.find(tenant => tenant.id === scope.tenant)?.name === name)
  }

  return (
    <section className="section">
      <QueryView result={details}>
        {tenant => (
          <>
            <header className="section-header">
              <h2>{tenant.name}</h2>
              <ActionDialog label="Rename tenant" className="secondary" onRun={rename}>
                <Field label="New name"><input name="name" defaultValue={tenant.name} required /></Field>
              </ActionDialog>
            </header>
            <p className="quiet">
              Subscription models: {tenant.subscriptionModels.map(model => model.name).join(', ')}
            </p>
          </>
        )}
      </QueryView>
    </section>
  )
}

function Teams({ teams, subscriptionModels }) {
  const { scope, choose } = useScope()

  async function add(form) {
    const id = newId()
    await command(`/tenants/${scope.tenant}/teams`, {
      id,
      name: form.get('name'),
      subscriptionModelId: form.get('subscriptionModelId'),
    })
    teams.reloadUntil(list => list.some(team => team.id === id))
  }

  function manageProjects(team) {
    choose('team', team.id)
    navigate('team-administration')
  }

  return (
    <section className="section">
      <header className="section-header">
        <h2>Teams</h2>
        <ActionDialog label="Add team" onRun={add}>
          <Field label="Name"><input name="name" required /></Field>
          <Field label="Subscription model">
            <select name="subscriptionModelId" required defaultValue="">
              <option value="" disabled>Choose a subscription model</option>
              {subscriptionModels.map(model => <option key={model.id} value={model.id}>{model.name}</option>)}
            </select>
          </Field>
        </ActionDialog>
      </header>
      <QueryView result={teams} empty="No teams yet. Add a team to start setting up projects.">
        {list => (
          <table>
            <thead><tr><th>Name</th><th>Subscription model</th><th /></tr></thead>
            <tbody>
              {list.map(team => (
                <tr key={team.id}>
                  <td>{team.name}</td>
                  <td>{team.subscriptionModel}</td>
                  <td className="row-actions">
                    <button type="button" className="secondary" onClick={() => manageProjects(team)}>Manage projects</button>
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
