import { levels, useScope } from './scope'
import { useQuery } from './useQuery'

const labels = { tenant: 'Tenant', team: 'Team', project: 'Project' }

export default function ScopeSwitcher({ depth }) {
  const { scope, choose } = useScope()
  const shown = levels.slice(0, levels.indexOf(depth) + 1)

  const options = {
    tenant: useQuery(shown.includes('tenant') ? '/tenants' : null),
    team: useQuery(shown.includes('team') && scope.tenant ? `/tenants/${scope.tenant}/teams` : null),
    project: useQuery(shown.includes('project') && scope.team ? `/teams/${scope.team}/projects` : null),
  }

  return (
    <div className="scope">
      {shown.map((level, index) => {
        const parent = levels[index - 1]
        if (parent && !scope[parent]) return null
        return (
          <ScopeSelect
            key={level}
            label={labels[level]}
            options={options[level]}
            value={scope[level]}
            onChange={id => choose(level, id)}
          />
        )
      })}
    </div>
  )
}

function ScopeSelect({ label, options: { data, error, loading }, value, onChange }) {
  const name = label.toLowerCase()
  const items = data ?? []
  const selected = items.some(item => item.id === value) ? value : ''

  let placeholder = `Choose a ${name}`
  if (loading) placeholder = 'Loading…'
  else if (error) placeholder = `Couldn't load ${name}s`
  else if (items.length === 0) placeholder = `No ${name}s yet`

  return (
    <label className="scope-level">
      <span>{label}</span>
      <select value={selected} disabled={items.length === 0} onChange={event => onChange(event.target.value)}>
        <option value="" disabled>{placeholder}</option>
        {items.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
      </select>
    </label>
  )
}
