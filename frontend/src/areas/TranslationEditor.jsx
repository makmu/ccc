import { useState } from 'react'
import ActionDialog, { Field } from '../ActionDialog'
import { command, newId } from '../api'
import QueryView from '../QueryView'
import { useScope } from '../scope'
import { useQuery } from '../useQuery'

const nameField = 'name'

export default function TranslationEditor() {
  const { scope } = useScope()
  const base = `/teams/${scope.team}/projects/${scope.project}`
  const project = useQuery(`${base}/keys`)
  const [pending, setPending] = useState({})

  // Shows a change right away and keeps it until the read model has caught up with it.
  function track(keyId, field, value, isCaughtUp) {
    const id = `${keyId} ${field}`
    setPending(current => ({ ...current, [id]: value }))
    project.reloadUntil(data => isCaughtUp(data.keys.find(key => key.id === keyId))).then(() =>
      setPending(current => {
        const next = { ...current }
        if (next[id] === value) delete next[id]
        return next
      }))
  }

  function shown(key, field, current) {
    const id = `${key.id} ${field}`
    return id in pending ? pending[id] : current
  }

  async function addKey(form) {
    const id = newId()
    await command(`${base}/keys`, { id, name: form.get('name').trim() })
    project.reloadUntil(data => data.keys.some(key => key.id === id))
  }

  async function renameKey(key, form) {
    const name = form.get('name').trim()
    await command(`${base}/keys/${key.id}/rename`, { name })
    track(key.id, nameField, name, current => current?.name === name)
  }

  async function provideText(key, language, isReference, text) {
    if (isReference) await command(`${base}/keys/${key.id}/reference-text`, { text })
    else await command(`${base}/keys/${key.id}/translations`, { languageCode: language, text })
    track(key.id, language, text, current => (textIn(current, language, isReference) ?? null) === text)
  }

  return (
    <section className="section">
      <header className="section-header">
        <h2>Keys</h2>
        <ActionDialog label="Add key" onRun={addKey}>
          <Field label="Name"><input name="name" placeholder="e.g. checkout.title" required /></Field>
        </ActionDialog>
      </header>
      <QueryView result={project}>
        {({ referenceLanguage, languages, keys }) => keys.length === 0
          ? <p className="quiet">No keys yet. Add a key, then write its text in {referenceLanguage}.</p>
          : (
            <div className="table-scroll">
              <table className="editor">
                <thead>
                  <tr>
                    <th>Key</th>
                    <th>{referenceLanguage} <span className="quiet">(reference)</span></th>
                    {languages.map(language => <th key={language}>{language}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {keys.map(key => {
                    const name = shown(key, nameField, key.name)
                    const cell = (language, isReference) => (
                      <TextCell
                        key={language}
                        keyName={name}
                        language={language}
                        isReference={isReference}
                        text={shown(key, language, textIn(key, language, isReference))}
                        referenceText={shown(key, referenceLanguage, key.referenceText)}
                        onProvide={text => provideText(key, language, isReference, text)}
                      />
                    )
                    return (
                      <tr key={key.id}>
                        <th scope="row" className="key-name">
                          <span>{name}</span>
                          <ActionDialog
                            label="Rename key"
                            title={`Rename ${name}`}
                            trigger="Rename"
                            triggerLabel={`Rename ${name}`}
                            className="link"
                            onRun={form => renameKey(key, form)}
                          >
                            <Field label="New name"><input name="name" defaultValue={name} required /></Field>
                          </ActionDialog>
                        </th>
                        {cell(referenceLanguage, true)}
                        {languages.map(language => cell(language, false))}
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
      </QueryView>
    </section>
  )
}

function textIn(key, language, isReference) {
  if (!key) return undefined
  return isReference ? key.referenceText : key.translations[language]
}

function TextCell({ keyName, language, isReference, text, referenceText, onProvide }) {
  const label = isReference ? 'Provide reference text' : 'Provide translation'
  const withdrawal = {
    label: isReference ? 'Withdraw reference text' : 'Withdraw translation',
    onRun: () => onProvide(null),
  }
  return (
    <td>
      <ActionDialog
        label={label}
        title={isReference ? `Reference text for ${keyName}` : `${language} translation for ${keyName}`}
        trigger={text == null
          ? <span className="quiet">Add text</span>
          : text === '' ? <span className="quiet">Empty text</span> : text}
        triggerLabel={`${label} in ${language} for ${keyName}`}
        className="cell"
        onRun={form => onProvide(form.get('text'))}
        secondaryAction={text == null ? undefined : withdrawal}
      >
        {!isReference && (
          <p className="quiet">Reference text: {referenceText ?? 'none yet'}</p>
        )}
        <Field label="Text"><textarea name="text" rows="3" defaultValue={text ?? ''} /></Field>
      </ActionDialog>
    </td>
  )
}
