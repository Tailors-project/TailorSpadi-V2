export interface PasswordHint {
  ok: boolean;
  text: string;
}

/** Client-side hints only; the server is authoritative. */
export function passwordHints(password: string, email = ''): PasswordHint[] {
  return [
    { ok: password.length >= 10, text: 'At least 10 characters' },
    { ok: password.length <= 128, text: 'No more than 128 characters' },
    {
      ok: email === '' || password.toLowerCase() !== email.trim().toLowerCase(),
      text: 'Different from your email address',
    },
  ];
}

export function passwordProblem(password: string, email = ''): string | undefined {
  return passwordHints(password, email).find((hint) => !hint.ok)?.text;
}
