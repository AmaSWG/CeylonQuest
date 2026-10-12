import { useEffect, useState } from 'react'
import { getAdminAuditLogs } from '../../../api/adminAuditApi'
import './AdminAuditTrailTab.css'
import { getAdminUsers } from '../../../api/adminManagementApi'

const emptyFilters = {
    actionType: '',
    administratorId: '',
    targetId: '',
    dateFrom: '',
    dateTo: '',
}

const actionLabels = {
    UserReactivated: 'User Reactivated',
    UserSuspended: 'User Suspended',
    ListingRemoved: 'Listing Removed',
}

function formatTimestamp(value) {
    const date = new Date(value)
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

export default function AdminAuditTrailTab({ token }) {
    const [draft, setDraft] = useState({ ...emptyFilters })
    const [filters, setFilters] = useState({ ...emptyFilters })
    const [page, setPage] = useState(1)
    const [refresh, setRefresh] = useState(0)
    const [data, setData] = useState({ items: [], totalCount: 0 })
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')
    const [adminNames, setAdminNames] = useState({})

    const pageSize = 25
    const totalPages = Math.max(
        1,
        Math.ceil(data.totalCount / pageSize),
    )
    const accessiblePages = Math.min(totalPages, 100)

    useEffect(() => {
        const controller = new AbortController()

        async function load() {
            setLoading(true)
            setError('')

            try {
                const result = await getAdminAuditLogs(
                    token,
                    { ...filters, page, pageSize },
                    controller.signal,
                )

                if (!controller.signal.aborted) {
                    setData(result)
                }
            } catch (err) {
                if (!controller.signal.aborted) {
                    setError(err.message || 'Unable to load audit logs.')
                }
            } finally {
                if (!controller.signal.aborted) {
                    setLoading(false)
                }
            }
        }

        load()

        return () => controller.abort()
    }, [token, filters, page, refresh])

    function updateDraft(event) {
        const { name, value } = event.target

        setDraft(current => ({
            ...current,
            [name]: value,
        }))
    }

    function applyFilters(event) {
        event.preventDefault()

        if (draft.dateFrom && draft.dateTo &&
            new Date(draft.dateFrom) > new Date(draft.dateTo)) {
            setError('The start date must be before the end date.')
            return
        }

        setPage(1)
        setFilters({
            actionType: draft.actionType,
            administratorId: draft.administratorId.trim(),
            targetId: draft.targetId.trim(),
            dateFrom: draft.dateFrom
                ? new Date(draft.dateFrom).toISOString()
                : '',
            dateTo: draft.dateTo
                ? new Date(draft.dateTo).toISOString()
                : '',
        })
    }

    function resetFilters() {
        setDraft({ ...emptyFilters })
        setFilters({ ...emptyFilters })
        setPage(1)
    }

    return (
        <section className="admin-audit">
            <div className="ad-page-header">
                <div className="ad-page-header__left">
                    <h1>Audit Trail</h1>
                    <p>View administrative actions, user status changes, and listing removal history.</p>
                </div>

                <button
                    type="button"
                    className="ad-row-btn ad-row-btn--view"
                    disabled={loading}
                    onClick={() => setRefresh(value => value + 1)}
                >
                    Refresh
                </button>
            </div>

            <form
                className="admin-audit__filters"
                onSubmit={applyFilters}
            >
                <label>
                    Action type
                    <select
                        name="actionType"
                        value={draft.actionType}
                        onChange={updateDraft}
                    >
                        <option value="">All actions</option>
                        <option value="UserReactivated">User Reactivated</option>
                        <option value="UserSuspended">User Suspended</option>
                        <option value="ListingRemoved">Listing Removed</option>
                    </select>
                </label>

                <label>
                    Admin ID
                    <input
                        name="administratorId"
                        value={draft.administratorId}
                        onChange={updateDraft}
                        placeholder="Administrator UUID"
                    />
                </label>

                <label>
                    Target ID
                    <input
                        name="targetId"
                        value={draft.targetId}
                        onChange={updateDraft}
                        placeholder="User or listing UUID"
                    />
                </label>

                <label>
                    From
                    <input
                        type="datetime-local"
                        name="dateFrom"
                        value={draft.dateFrom}
                        onChange={updateDraft}
                    />
                </label>

                <label>
                    To
                    <input
                        type="datetime-local"
                        name="dateTo"
                        value={draft.dateTo}
                        onChange={updateDraft}
                    />
                </label>

                <div className="admin-audit__actions">
                    <button className="ad-filter-pill active" type="submit" disabled={loading}>
                        Apply filters
                    </button>
                    <button
                        type="button"
                        className="ad-filter-pill"
                        disabled={loading}
                        onClick={resetFilters}
                    >
                        Reset
                    </button>
                </div>
            </form>

            {loading && <p role="status">Loading audit logs…</p>}
            {error && <p role="alert">{error}</p>}

            {!loading && !error && (
                <>
                    <p className="admin-audit__count">{data.totalCount} audit records</p>

                    <div className="ad-card">
                        <div className="ad-card__body admin-audit__card-body">
                            {data.items.length === 0 ? (
                                <div className="ad-empty">
                                    <p className="ad-empty__title">No audit records found</p>
                                    <p className="ad-empty__msg">No administrative actions match your filters.</p>
                                </div>
                            ) : (
                                <div className="ad-table-wrap admin-audit__table">
                                    <table className="ad-table">
                                        <caption className="admin-audit__caption">
                                            Administrative actions, newest first.
                                            Times use your local timezone.
                                        </caption>
                                        <thead>
                                            <tr>
                                                <th scope="col">Action</th>
                                                <th scope="col">Timestamp</th>
                                                <th scope="col">Administrator</th>
                                                <th scope="col">Target</th>
                                                <th scope="col">Status change</th>
                                                <th scope="col">Reason</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {data.items.map(item => (
                                                <tr key={`${item.actionType}-${item.id}`}>
                                                    <td>
                                                        {actionLabels[item.actionType] || item.actionType}
                                                    </td>
                                                    <td>{formatTimestamp(item.timestampUtc)}</td>
                                                    <td>
                                                        {adminNames[item.administratorId?.toLowerCase()] ||
                                                            'Name unavailable'}
                                                    </td>
                                                    <td className="admin-audit__id">
                                                        <strong>{item.targetType}</strong>
                                                        <br />
                                                        {item.targetId}
                                                    </td>
                                                    <td>
                                                        {item.previousStatus} → {item.newStatus}
                                                    </td>
                                                    <td>{item.reason || '—'}</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>
                    </div>

                    <div
                        className="admin-audit__pagination"
                        aria-label="Audit log pagination"
                    >
                        <button
                            type="button"
                            className="ad-row-btn ad-row-btn--view"
                            disabled={page <= 1}
                            onClick={() => setPage(value => value - 1)}
                        >
                            Previous
                        </button>

                        <span>Page {page} of {totalPages}</span>

                        <button
                            type="button"
                            className="ad-row-btn ad-row-btn--view"
                            disabled={page >= accessiblePages}
                            onClick={() => setPage(value => value + 1)}
                        >
                            Next
                        </button>
                    </div>

                    {totalPages > 100 && (
                        <p>
                            Use filters to narrow the results.
                            Browsing is limited to the first 100 pages.
                        </p>
                    )}
                </>
            )}
        </section>
    )
}
