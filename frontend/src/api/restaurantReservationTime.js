export const pastSlotMessage = 'This time slot has already passed. Please select a future time slot.'

export function sriLankaDate(now = Date.now()) {
  return new Date(now + 330 * 60000).toISOString().slice(0, 10)
}

export function restaurantTimeError(date, slot, now = Date.now()) {
  if (!date || !slot) return null
  if (date < sriLankaDate(now)) return pastSlotMessage
  const start = slot.split('-')[0].trim()
  const match = /^(\d{1,2}):(\d{2})(?:\s+(AM|PM))?$/i.exec(start)
  if (!match) return 'The reservation start time could not be determined.'
  let hour = Number(match[1])
  const minute = Number(match[2])
  if (minute > 59 || (match[3] ? hour < 1 || hour > 12 : hour > 23)) {
    return 'The reservation start time could not be determined.'
  }
  if (match[3]) hour = hour % 12 + (match[3].toUpperCase() === 'PM' ? 12 : 0)
  const timestamp = Date.parse(`${date}T${String(hour).padStart(2, '0')}:${match[2]}:00+05:30`)
  return timestamp <= now ? pastSlotMessage : null
}
