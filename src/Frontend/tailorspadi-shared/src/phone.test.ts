import { describe, expect, it } from 'vitest';
import { phoneProblem } from './phone';

describe('phone validation', () => {
  it.each(['+2349000000000', '09012345678', '+234 (901) 234-5678', '1234567', '+123456789012345'])('accepts %s', (phone) => {
    expect(phoneProblem(phone)).toBeUndefined();
  });
  it.each(['', '123456', '+1234567890123456', '+234' + '0'.repeat(60), '-------', '12+3456789', '++2349012345678', '(1234567', 'phone1234567'])('rejects %s', (phone) => {
    expect(phoneProblem(phone)).toBeDefined();
  });
  it('allows an empty optional customer phone', () => {
    expect(phoneProblem(' ', false)).toBeUndefined();
    expect(phoneProblem('invalid', false)).toBeDefined();
  });
});
