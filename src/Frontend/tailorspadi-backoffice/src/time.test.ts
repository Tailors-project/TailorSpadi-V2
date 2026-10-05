import { describe, expect, it } from 'vitest';
import { formatBusinessTime, formatCalendarDate } from './time';

describe('business dates and times', () => {
  it('converts an instant across midnight in Lagos', () => {
    const formatted = formatBusinessTime('2026-09-27T23:30:00Z');
    expect(formatted).toContain('28 Sept 2026');
    expect(formatted).toMatch(/12:30|00:30/);
  });
  it('preserves a date without a time-zone conversion', () => {
    expect(formatCalendarDate('2026-09-27')).toBe('2026-09-27');
  });
  it('rejects a timestamp without an explicit offset', () => {
    expect(() => formatBusinessTime('2026-09-27T23:30:00')).toThrow();
  });
  it('rejects an impossible calendar date', () => {
    expect(() => formatCalendarDate('2026-02-30')).toThrow();
  });
});
