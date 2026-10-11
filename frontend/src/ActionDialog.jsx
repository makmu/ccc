import { useId, useRef, useState } from 'react'

export default function ActionDialog({ label, title = label, trigger = label, triggerLabel, className, onRun, secondaryAction, children }) {
  const dialog = useRef(null)
  const titleId = useId()
  const [rejection, setRejection] = useState(null)
  const [running, setRunning] = useState(false)

  function open() {
    setRejection(null)
    dialog.current.showModal()
  }

  async function run(event) {
    event.preventDefault()
    const form = event.currentTarget
    const action = event.nativeEvent.submitter?.name === 'secondary' ? secondaryAction.onRun : onRun
    setRunning(true)
    try {
      await action(new FormData(form))
      dialog.current.close()
      form.reset()
    } catch (error) {
      setRejection(error.message)
    } finally {
      setRunning(false)
    }
  }

  return (
    <>
      <button type="button" className={className} aria-label={triggerLabel} onClick={open}>{trigger}</button>
      <dialog ref={dialog} aria-labelledby={titleId}>
        <form onSubmit={run}>
          <h2 id={titleId}>{title}</h2>
          {children}
          {rejection && <p className="rejection" role="alert">{rejection}</p>}
          <div className="dialog-actions">
            {secondaryAction && (
              <button type="submit" name="secondary" className="secondary leading" disabled={running}>
                {secondaryAction.label}
              </button>
            )}
            <button type="button" className="secondary" onClick={() => dialog.current.close()}>Cancel</button>
            <button type="submit" disabled={running}>{label}</button>
          </div>
        </form>
      </dialog>
    </>
  )
}

export function Field({ label, children }) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
    </label>
  )
}
