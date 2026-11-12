import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { ProblemAlert, TextField, applyServerErrors, passwordHints, passwordProblem, toProblem, useAuth } from 'tailorspadi-shared';

interface RegisterForm {
  firstName: string; lastName: string; email: string; phone: string; password: string; confirmPassword: string;
}
const REGISTER_FIELDS = ['firstName', 'lastName', 'email', 'phone', 'password', 'confirmPassword'] as const;

export function RegisterPage() {
  const { api, login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, watch, setError: setFieldError, formState: { errors, isSubmitting } } = useForm<RegisterForm>();
  const password = watch('password') ?? '';
  const email = watch('email') ?? '';

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      await api.post('/auth/register-tailor', values);
      await login({ email: values.email, password: values.password });
      navigate('/onboarding', { replace: true });
    } catch (caught) {
      const problem = toProblem(caught);
      if (problem.code === 'email_taken') {
        setFieldError('email', { type: 'server', message: 'An account with this email already exists. Try signing in.' });
      } else {
        setError(applyServerErrors(problem, setFieldError, REGISTER_FIELDS));
      }
    }
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Your craft. Your business.</p>
      <h1>Create your account</h1>
      <form onSubmit={onSubmit} noValidate className="mt-3">
        <ProblemAlert message={error} />
        <div className="row">
          <div className="col-md-6"><TextField<RegisterForm> name="firstName" label="First name" autoComplete="given-name" register={register} errors={errors} options={{ required: 'Enter your first name' }} /></div>
          <div className="col-md-6"><TextField<RegisterForm> name="lastName" label="Last name" autoComplete="family-name" register={register} errors={errors} options={{ required: 'Enter your last name' }} /></div>
        </div>
        <TextField<RegisterForm> name="email" label="Email" type="email" autoComplete="email" register={register} errors={errors}
          options={{ required: 'Enter your email', pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email' } }} />
        <TextField<RegisterForm> name="phone" label="Phone" type="tel" autoComplete="tel" register={register} errors={errors} options={{ required: 'Enter your phone number' }} />
        <TextField<RegisterForm> name="password" label="Password" type="password" autoComplete="new-password" register={register} errors={errors}
          options={{ validate: (value) => passwordProblem(value, email) ?? true }}
          hint={<ul className="list-unstyled mb-0">{passwordHints(password, email).map((hint) => <li key={hint.text}>{hint.ok ? '✓' : '○'} {hint.text}</li>)}</ul>} />
        <TextField<RegisterForm> name="confirmPassword" label="Confirm password" type="password" autoComplete="new-password" register={register} errors={errors}
          options={{ validate: (value) => value === password || 'Passwords do not match' }} />
        <button className="btn btn-primary" disabled={isSubmitting}>{isSubmitting ? 'Creating...' : 'Create account'}</button>
        <p className="mt-3 mb-0">Already have an account? <Link to="/login">Sign in</Link></p>
      </form>
    </section>
  );
}

interface LoginForm { email: string; password: string }

export function LoginPage({ tenantName }: { tenantName?: string }) {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginForm>();

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      const session = await login(values);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? (session.scope === 'tenant' ? '/backoffice' : '/businesses'), { replace: true });
    } catch (caught) {
      const problem = toProblem(caught);
      setError(problem.code === 'membership_required' ? 'This account does not have access to this business.' : problem.title);
    }
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <h1>{tenantName ? `Sign in to ${tenantName}` : 'Welcome back'}</h1>
      <form onSubmit={onSubmit} noValidate className="mt-3">
        <ProblemAlert message={error} />
        <TextField<LoginForm> name="email" label="Email" type="email" autoComplete="username" register={register} errors={errors} options={{ required: 'Enter your email' }} />
        <TextField<LoginForm> name="password" label="Password" type="password" autoComplete="current-password" register={register} errors={errors} options={{ required: 'Enter your password' }} />
        <button className="btn btn-primary" disabled={isSubmitting}>{isSubmitting ? 'Signing in...' : 'Sign in'}</button>
        <p className="mt-3 mb-0">
          <Link to="/forgot-password">Forgot your password?</Link>
          {!tenantName && <> · <Link to="/register">Create an account</Link></>}
        </p>
      </form>
    </section>
  );
}

export function ForgotPasswordPage() {
  const { api } = useAuth();
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<{ email: string }>();

  const onSubmit = handleSubmit(async ({ email }) => {
    setError(null);
    try {
      await api.post('/auth/forgot-password', { email });
      setSent(true);
    } catch (caught) {
      setError(toProblem(caught).title);
    }
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <h1>Reset your password</h1>
      {sent ? (
        <p role="status">If an account exists for that email, we have sent a reset link. It expires in 30 minutes.</p>
      ) : (
        <form onSubmit={onSubmit} noValidate className="mt-3">
          <ProblemAlert message={error} />
          <TextField<{ email: string }> name="email" label="Email" type="email" autoComplete="email" register={register} errors={errors} options={{ required: 'Enter your email' }} />
          <button className="btn btn-primary" disabled={isSubmitting}>Send reset link</button>
        </form>
      )}
      <p className="mt-3 mb-0"><Link to="/login">Back to sign in</Link></p>
    </section>
  );
}

interface ResetForm { newPassword: string; confirmPassword: string }

export function ResetPasswordPage() {
  const { api } = useAuth();
  const navigate = useNavigate();
  // Read the token once, then remove it from the address bar so it never lingers in history or referrers.
  const [token] = useState(() => {
    const value = new URLSearchParams(window.location.search).get('token') ?? '';
    window.history.replaceState(null, '', window.location.pathname);
    return value;
  });
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<ResetForm>();
  const password = watch('newPassword') ?? '';

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      await api.post('/auth/reset-password', { token, ...values });
      setDone(true);
      setTimeout(() => navigate('/login', { replace: true }), 1500);
    } catch (caught) {
      const problem = toProblem(caught);
      setError(problem.code === 'reset_token_invalid' ? 'This reset link is invalid or has expired. Request a new one.' : problem.title);
    }
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <h1>Choose a new password</h1>
      {done ? <p role="status">Your password has been changed. Redirecting to sign in...</p> : (
        <form onSubmit={onSubmit} noValidate className="mt-3">
          <ProblemAlert message={error} />
          <TextField<ResetForm> name="newPassword" label="New password" type="password" autoComplete="new-password" register={register} errors={errors}
            options={{ validate: (value) => passwordProblem(value) ?? true }}
            hint={<ul className="list-unstyled mb-0">{passwordHints(password).map((hint) => <li key={hint.text}>{hint.ok ? '✓' : '○'} {hint.text}</li>)}</ul>} />
          <TextField<ResetForm> name="confirmPassword" label="Confirm password" type="password" autoComplete="new-password" register={register} errors={errors}
            options={{ validate: (value) => value === password || 'Passwords do not match' }} />
          <button className="btn btn-primary" disabled={isSubmitting || token === ''}>Change password</button>
        </form>
      )}
    </section>
  );
}
