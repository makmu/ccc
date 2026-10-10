export default function QueryView({ result: { data, error, loading }, empty, children }) {
  if (error) return <p className="rejection" role="alert">{error}</p>
  if (data === undefined && loading) return <p className="quiet">Loading…</p>
  if (data === undefined) return null
  if (Array.isArray(data) && data.length === 0) return <p className="quiet">{empty}</p>
  return children(data)
}
