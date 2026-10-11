export const newId = () => crypto.randomUUID()

export const command = (path, body) => send('POST', path, body)

export const query = path => send('GET', path)

async function send(method, path, body) {
  let response
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new Error("The server can't be reached. Check that it is running and try again.")
  }

  const text = await response.text()
  const content = text ? JSON.parse(text) : null
  if (response.ok) return content

  if ([400, 404, 409].includes(response.status) && typeof content?.detail === 'string') throw new Error(content.detail)
  throw new Error('Something went wrong on the server. Try again in a moment.')
}
