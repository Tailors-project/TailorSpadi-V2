/** Format validation only; does not verify that a phone number is assigned. */
export function phoneProblem(value: string, required = true): string | undefined {
  const phone = value.trim();
  if (!phone) return required ? 'Enter your phone number' : undefined;
  const digits = phone.replace(/[^0-9]/g, '');
  if (phone.length > 32 || !/^\+?(?:[0-9]+|\([0-9]+\))(?:[ -]?(?:[0-9]+|\([0-9]+\)))*$/.test(phone)
      || digits.length < 7 || digits.length > 15) {
    return 'Enter a valid phone number with 7–15 digits';
  }
  return undefined;
}
