import ActionDialog, { Field } from '../ActionDialog'
import { command, newId } from '../api'
import QueryView from '../QueryView'
import { useScope } from '../scope'
import { navigate } from '../useHashRoute'

export default function TeamAdministration() {
  const { scope, lists } = useScope()
  const projects = lists.project

  async function add(form) {
    const id = newId()
    await command(`/teams/${scope.team}/projects`, {
      id,
      name: form.get('name'),
      referenceLanguage: form.get('referenceLanguage').trim(),
    })
    projects.reloadUntil(list => list.some(project => project.id === id))
  }

  return (
    <section className="section">
      <header className="section-header">
        <h2>Projects</h2>
        <ActionDialog label="Add project" onRun={add}>
          <Field label="Name"><input name="name" required /></Field>
          <Field label="Reference language">
            <input name="referenceLanguage" placeholder="e.g. en" required />
          </Field>
          <p className="quiet">Texts are written in the reference language first. Every other language falls back to it.</p>
        </ActionDialog>
      </header>
      <QueryView result={projects} empty="No projects yet. Add a project to start adding keys.">
        {list => list.map(project => <Project key={project.id} project={project} projects={projects} />)}
      </QueryView>
    </section>
  )
}

function Project({ project, projects }) {
  const { scope, choose } = useScope()

  async function addLanguage(form) {
    const languageCode = form.get('languageCode').trim()
    await command(`/teams/${scope.team}/projects/${project.id}/languages`, {
      languageCode,
      parentLanguageCode: form.get('parentLanguageCode'),
    })
    projects.reloadUntil(list =>
      list.find(p => p.id === project.id)?.languages.some(language => language.code === languageCode))
  }

  function editTexts() {
    choose('project', project.id)
    navigate('translation-editor')
  }

  return (
    <article className="project">
      <header className="section-header">
        <h3>{project.name}</h3>
        <div className="actions">
          <ActionDialog label="Add language" title={`Add a language to ${project.name}`} className="secondary" onRun={addLanguage}>
            <Field label="Language code"><input name="languageCode" placeholder="e.g. de-AT" required /></Field>
            <Field label="Falls back to">
              <select name="parentLanguageCode" defaultValue={project.referenceLanguage}>
                {project.languages.map(language => (
                  <option key={language.code} value={language.code}>{language.code}</option>
                ))}
              </select>
            </Field>
          </ActionDialog>
          <button type="button" className="secondary" onClick={editTexts}>Edit texts</button>
        </div>
      </header>
      <LanguageTree languages={project.languages} code={project.referenceLanguage} isReference />
    </article>
  )
}

function LanguageTree({ languages, code, isReference = false }) {
  const children = languages.filter(language => language.parentCode === code)
  return (
    <ul className="language-tree">
      <li>
        {code}
        {isReference && <span className="quiet"> (reference)</span>}
        {children.map(child => <LanguageTree key={child.code} languages={languages} code={child.code} />)}
      </li>
    </ul>
  )
}
