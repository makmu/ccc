import { useState } from 'react'
import AddUserForm from './AddUserForm'
import AddOrganizationForm from './AddOrganizationForm'
import './App.css'

const TABS = ['Add User', 'Add Organization']

export default function App() {
  const [activeTab, setActiveTab] = useState(0)

  return (
    <div className="app">
      <h1>CCC Admin</h1>
      <div className="tabs">
        {TABS.map((tab, i) => (
          <button
            key={tab}
            className={i === activeTab ? 'tab active' : 'tab'}
            onClick={() => setActiveTab(i)}
          >
            {tab}
          </button>
        ))}
      </div>
      <div className="tab-content">
        {activeTab === 0 && <AddUserForm />}
        {activeTab === 1 && <AddOrganizationForm />}
      </div>
    </div>
  )
}
