import { describe, expect, it } from 'vitest';
import { passwordProblem } from './password';

describe('password hints', () => {
  it('accepts seven characters and rejects six', () => {
    expect(passwordProblem('sewing7')).toBeUndefined();
    expect(passwordProblem('sewing')).toBeDefined();
  });
  it('flags short passwords and email reuse', () => {
    expect(passwordProblem('short')).toBeDefined();
    expect(passwordProblem('ada@example.com', 'ADA@example.com')).toBeDefined();
    expect(passwordProblem('Sew-Tailor#Lagos-2026', 'ada@example.com')).toBeUndefined();
  });
});
