# ADR-010: Projections Stall After Repeated Failures

## Decision

A projection runner gives up on a failure it can't get past. It retries a failing step three times at most, waiting 2, 4 and 8 seconds, and then stalls. A stalled projection stops catching up until an operator unstalls it, including across restarts of the API.

- **What counts.** Every exception counts towards the three retries, except transient database errors (`NpgsqlException.IsTransient`, such as a lost connection or a timeout). Those keep retrying with a backoff that doubles up to one minute, as before. While the database can't be reached, a stall couldn't be recorded anyway.
- **Recording the stall.** The runner records the stall in the projection's bookmark table: `stalled_at` holds the time, and `stall_reason` names the event that failed and why, for example `Applying event 2 (UserAdded) failed: 23505: duplicate key value violates unique constraint "users_pkey"`. The bookmark's `position` stays at the last batch that was applied. If the stall can't be written, the runner gives up only until the next start.
- **Restarts.** On startup, a runner whose bookmark is stalled logs a warning and doesn't catch up. The projection keeps answering queries with the state it had when it stalled.
- **Install failures.** A runner that can't install its schema gives up the same way, but doesn't record a stall. It tries again on the next start.
- **Unstalling.** Fix the cause first, usually in the projection's code, then do one of the following:
  - If the projection's logic or tables changed, raise its `Version`. The rebuild recreates the bookmark, which clears the stall.
  - Otherwise, clear the stall and restart the API. The projection then resumes from its bookmark:

    ```sql
    UPDATE translation_delivery.bookmark SET stalled_at = NULL, stall_reason = NULL;
    ```

`ProjectionRetryPolicy` holds the number of retries and the first delay. `AddProjection<T>` registers the default, and the test fixture replaces it with a 10 ms delay, so tests stall in milliseconds.

## Context

Until now, a runner retried every failure forever, with a backoff of up to a minute. An event that a projection can't apply, such as one that violates a primary key, therefore made the runner retry the same batch for as long as the API ran, logging an error every minute and holding back every later event. Nothing outside the logs showed that the projection had stopped, and a restart started the same loop again.

## Consequences

**Positive:**
- A projection that can't apply an event stops trying, and its bookmark states why and where.
- A restart doesn't hide a stall or repeat failures that are known to recur.
- A short database outage still recovers on its own.

**Negative:**
- A stalled projection stays stale until someone notices and acts. Queries don't report that their projection is stalled; only `/health` does, with the stall reason.
- Unstalling without a `Version` change takes SQL and a restart, because there is no endpoint for it.
- A failure that would have gone away by itself, but that Npgsql doesn't mark as transient, now stalls the projection.

## Alternatives Considered

1. **Retry forever (the previous behaviour)**
   - *Advantages:* No operator action after transient problems, and nothing to unstall.
   - *Disadvantages:* A poison event blocks the projection forever, and the only sign of it is a log line repeated every minute.

2. **Count every exception, including transient database errors**
   - *Advantages:* One simple rule.
   - *Disadvantages:* A database outage of about 15 seconds would stall every projection, and the stall couldn't even be recorded while the database is unreachable.

3. **Skip the failing event and carry on**
   - *Advantages:* The projection keeps up with later events.
   - *Disadvantages:* The projection silently diverges from the event log, which is worse than a projection that is visibly behind.

4. **An endpoint that unstalls a projection**
   - *Advantages:* No SQL and no restart needed.
   - *Disadvantages:* It needs a path from `Api` to the projection runners, which today only `Program.cs` may reach. Clearing two columns is enough for a demo.
