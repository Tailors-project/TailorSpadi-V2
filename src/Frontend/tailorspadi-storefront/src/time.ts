export const defaultTimeZone = 'Africa/Lagos';

export function formatBusinessTime(instant: string, timeZone = defaultTimeZone): string {
  const date = new Date(instant);
  if (Number.isNaN(date.getTime()) || !/(Z|[+-]\d{2}:\d{2})$/i.test(instant)) {
    throw new Error('An ISO 8601 instant with a time-zone offset is required.');
  }
  return new Intl.DateTimeFormat('en-NG', {
    timeZone,
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}

export function formatCalendarDate(value: string): string {
  // A calendar date is not an instant: keep its year/month/day unchanged.
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    throw new Error('A calendar date must use YYYY-MM-DD.');
  }
  const date = new Date(value + 'T00:00:00Z');
  if (Number.isNaN(date.getTime()) || date.toISOString().slice(0, 10) !== value) {
    throw new Error('A valid calendar date is required.');
  }
  return value;
}
