import { useId } from 'react'

export default function StarRating({
  value = 0,
  onChange,
  disabled = false
}) {
  const groupName = useId()

  if (!onChange) {
    return (
      <span
        className="cq-stars"
        role="img"
        aria-label={`${value} out of 5 stars`}
      >
        {'★'.repeat(value)}{'☆'.repeat(5 - value)}
      </span>
    )
  }

  return (
    <fieldset className="cq-star-picker" disabled={disabled}>
      <legend>Rating *</legend>

      {[1, 2, 3, 4, 5].map(star => (
        <label key={star}>
          <input
            type="radio"
            name={groupName}
            value={star}
            checked={value === star}
            onChange={() => onChange(star)}
            required
          />
          <span aria-hidden="true">
            {star <= value ? '★' : '☆'}
          </span>
          <span className="cq-sr-only">
            {star} star{star === 1 ? '' : 's'}
          </span>
        </label>
      ))}
    </fieldset>
  )
}