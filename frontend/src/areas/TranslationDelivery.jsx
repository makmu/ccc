import { useState } from 'react'
import QueryView from '../QueryView'
import { useScope } from '../scope'
import { useQuery } from '../useQuery'

export default function TranslationDelivery() {
  const { scope } = useScope()
  const base = `/teams/${scope.team}/projects/${scope.project}`
  const languages = useQuery(`${base}/languages`)

  return (
    <QueryView result={languages}>
      {list => <Delivery key={scope.project} base={base} languages={list} />}
    </QueryView>
  )
}

function Delivery({ base, languages }) {
  const [language, setLanguage] = useState(languages[0])
  const path = `${base}/languages/${language}/translations`
  const translations = useQuery(path)
  const address = `${window.location.origin}/api${path}`
  const [copied, setCopied] = useState(false)

  async function copy() {
    await navigator.clipboard.writeText(address)
    setCopied(true)
  }

  return (
    <>
      <section className="section">
        <label className="field inline">
          <span>Language</span>
          <select value={language} onChange={event => { setLanguage(event.target.value); setCopied(false) }}>
            {languages.map((code, index) => (
              <option key={code} value={code}>{index === 0 ? `${code} (reference)` : code}</option>
            ))}
          </select>
        </label>
        <p className="quiet">Your application fetches these texts from</p>
        <div className="address">
          <code>{address}</code>
          <button type="button" className="secondary" onClick={copy}>{copied ? 'Copied' : 'Copy address'}</button>
        </div>
      </section>
      <section className="section">
        <h2>Texts in {language}</h2>
        <QueryView result={translations} empty="This project has no keys yet. Add them in the translation editor.">
          {list => (
            <table>
              <thead><tr><th>Key</th><th>Text</th><th>Taken from</th></tr></thead>
              <tbody>
                {list.map(translation => (
                  <tr key={translation.keyId}>
                    <td>{translation.keyName}</td>
                    <td>{translation.text ?? <span className="quiet">No text yet</span>}</td>
                    <td>
                      {translation.sourceLanguageCode === null && <span className="quiet">—</span>}
                      {translation.sourceLanguageCode === language && <span className="quiet">its own text</span>}
                      {translation.sourceLanguageCode && translation.sourceLanguageCode !== language && translation.sourceLanguageCode}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </QueryView>
      </section>
    </>
  )
}
