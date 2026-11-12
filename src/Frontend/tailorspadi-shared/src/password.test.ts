import { describe, expect, it } from 'vitest';
import { passwordProblem } from './password';

describe('password hints', () => {
  it('flags short passwords and email reuse', () => {
    expect(passwordProblem('short')).toBeDefined();
    expect(passwordProblem('ada@example.com', 'ADA@example.com')).toBeDefined();
    expect(passwordProblem('Sew-Tailor#Lagos-2026', 'ada@example.com')).toBeUndefined();
  });
});
