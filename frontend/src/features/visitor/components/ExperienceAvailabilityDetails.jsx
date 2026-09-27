import { CalendarMonthIcon } from '../../../components/Icons'

function formatDate(value) {
  if (!value) return 'Not specified'
  const date = new Date(`${String(value).slice(0, 10)}T00:00:00Z`)
  if (Number.isNaN(date.getTime())) return 'Not specified'
  return date.toLocaleDateString('en-GB', {
    day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC'
  })
}

export default function ExperienceAvailabilityDetails({ item }) {
  if (item?.type !== 'Experience') return null
  const days = Array.isArray(item.availableDays)
    ? item.availableDays.join(', ')
    : item.availableDays
  const rows = [
    ['Valid From', formatDate(item.validFrom)],
    ['Valid Until', formatDate(item.validUntil)],
    ['Available Days', days || 'Not specified']
  ]

  return rows.map(([label, value]) => (
    <div className="vd-detail-row" key={label}>
      <span className="vd-detail-row__label">
        <CalendarMonthIcon size={16} /> {label}:
      </span>
      <span className="vd-detail-row__val">{value}</span>
    </div>
  ))
}
