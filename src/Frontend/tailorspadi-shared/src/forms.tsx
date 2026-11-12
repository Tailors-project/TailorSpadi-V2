import type { ReactNode } from 'react';
import type { FieldValues, Path, UseFormRegister, UseFormSetError, FieldErrors, RegisterOptions } from 'react-hook-form';
import type { ApiProblem } from './http';

interface FieldProps<T extends FieldValues> {
  name: Path<T>;
  label: string;
  register: UseFormRegister<T>;
  errors: FieldErrors<T>;
  type?: string;
  autoComplete?: string;
  options?: RegisterOptions<T, Path<T>>;
  hint?: ReactNode;
}

export function TextField<T extends FieldValues>({ name, label, register, errors, type = 'text', autoComplete, options, hint }: FieldProps<T>) {
  const message = (errors[name]?.message as string | undefined) ?? undefined;
  const id = `field-${name}`;
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label">{label}</label>
      <input
        id={id}
        type={type}
        autoComplete={autoComplete}
        className={`form-control${message ? ' is-invalid' : ''}`}
        aria-invalid={message ? true : undefined}
        aria-describedby={message ? `${id}-error` : hint ? `${id}-hint` : undefined}
        {...register(name, options)}
      />
      {hint && !message && <div id={`${id}-hint`} className="form-text">{hint}</div>}
      {message && <div id={`${id}-error`} className="invalid-feedback d-block">{message}</div>}
    </div>
  );
}

export function ProblemAlert({ message }: { message: string | null }) {
  return message ? <div className="alert alert-danger" role="alert">{message}</div> : null;
}

/** Maps server validation errors onto form fields; returns a message for anything left over. */
export function applyServerErrors<T extends FieldValues>(problem: ApiProblem, setError: UseFormSetError<T>, known: readonly string[]): string | null {
  let mapped = false;
  for (const [field, messages] of Object.entries(problem.errors)) {
    const name = field.startsWith('business.') ? field.slice('business.'.length) : field;
    if (known.includes(name) && messages.length > 0) {
      setError(name as Path<T>, { type: 'server', message: messages[0] });
      mapped = true;
    }
  }
  return mapped ? null : problem.title;
}
