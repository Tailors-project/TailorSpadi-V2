import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { ProblemAlert, TextField, applyServerErrors, passwordHints, passwordProblem, phoneProblem, toProblem, useAuth } from 'tailorspadi-shared';

interface RegisterForm { firstName: string; lastName: string; email: string; phone: string; password: string; confirmPassword: string }
const FIELDS = ['firstName', 'lastName', 'email', 'phone', 'password', 'confirmPassword'] as const;

export function RegisterCustomerPage() {
  const { api, login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [existing, setExisting] = useState(false);
  const { register, handleSubmit, watch, setError: setFieldError, formState: { errors, isSubmitting } } = useForm<RegisterForm>();
  const password = watch('password') ?? '';
  const email = watch('email') ?? '';

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    setExisting(false);
    try {
      await api.post('/auth/register-customer', values);
      await login({ email: values.email, password: values.password });
      navigate('/customer', { replace: true });
    } catch (caught) {
      const problem = toProblem(caught);
      if (problem.code === 'email_taken') {
        setExisting(true);
      } else {
        setError(applyServerErrors(problem, setFieldError, FIELDS));
      }
    }
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <h1>Create your account</h1>
      <form onSubmit={onSubmit} noValidate className="mt-3">
        <ProblemAlert message={error} />
        {existing && (
          <div className="alert alert-info" role="alert">
            You already have a TailorSpadi account. <Link to="/customer/login">Sign in here</Link> to join this store with it.
          </div>
        )}
        <div className="row">
          <div className="col-md-6"><TextField<RegisterForm> name="firstName" label="First name" autoComplete="given-name" register={register} errors={errors} options={{ required: 'Enter your first name' }} /></div>
          <div className="col-md-6"><TextField<RegisterForm> name="lastName" label="Last name" autoComplete="family-name" register={register} errors={errors} options={{ required: 'Enter your last name' }} /></div>
        </div>
        <TextField<RegisterForm> name="email" label="Email" type="email" autoComplete="email" register={register} errors={errors}
          options={{ required: 'Enter your email', pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email' } }} />
        <TextField<RegisterForm> name="phone" label="Phone (optional)" type="tel" autoComplete="tel" register={register} errors={errors} options={{ validate: (value) => phoneProblem(value, false) ?? true }} />
        <TextField<RegisterForm> name="password" label="Password" type="password" autoComplete="new-password" register={register} errors={errors}
          options={{ validate: (value) => passwordProblem(value, email) ?? true }}
          hint={<ul className="list-unstyled mb-0">{passwordHints(password, email).map((hint) => <li key={hint.text}>{hint.ok ? '✓' : '○'} {hint.text}</li>)}</ul>} />
        <TextField<RegisterForm> name="confirmPassword" label="Confirm password" type="password" autoComplete="new-password" register={register} errors={errors}
          options={{ validate: (value) => value === password || 'Passwords do not match' }} />
        <button className="btn btn-primary" disabled={isSubmitting}>{isSubmitting ? 'Creating...' : 'Create account'}</button>
        <p className="mt-3 mb-0">Already have an account? <Link to="/customer/login">Sign in</Link></p>
      </form>
    </section>
  );
}

interface LoginForm { email: string; password: string }

export function CustomerLoginPage() {
  const { login, api, applyResponse } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [needsJoin, setNeedsJoin] = useState<LoginForm | null>(null);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginForm>();

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    setNeedsJoin(null);
    try {
      await login(values);
      navigate('/customer', { replace: true });
    } catch (caught) {
      const problem = toProblem(caught);
      if (problem.code === 'membership_required') {
        setNeedsJoin(values);
      } else {
        setError(problem.title);
      }
    }
  });

  async function join() {
    if (!needsJoin) { return; }
    try {
      const response = await api.post('/auth/join', needsJoin);
      applyResponse(response.data);
      navigate('/customer', { replace: true });
    } catch (caught) {
      setError(toProblem(caught).title);
    }
  }

  return (
    <section className="placeholder-card p-4 p-md-5">
      <h1>Sign in</h1>
      <form onSubmit={onSubmit} noValidate className="mt-3">
        <ProblemAlert message={error} />
        {needsJoin && (
          <div className="alert alert-info" role="alert">
            <p className="mb-2">You have a TailorSpadi account, but you have not joined this store yet.</p>
            <button type="button" className="btn btn-primary btn-sm" onClick={() => void join()}>Join this store</button>
          </div>
        )}
        <TextField<LoginForm> name="email" label="Email" type="email" autoComplete="username" register={register} errors={errors} options={{ required: 'Enter your email' }} />
        <TextField<LoginForm> name="password" label="Password" type="password" autoComplete="current-password" register={register} errors={errors} options={{ required: 'Enter your password' }} />
        <button className="btn btn-primary" disabled={isSubmitting}>{isSubmitting ? 'Signing in...' : 'Sign in'}</button>
        <p className="mt-3 mb-0">New here? <Link to="/register-customer">Create an account</Link></p>
      </form>
    </section>
  );
}

export function CustomerHome() {
  const { session, logout } = useAuth();
  return (
    <section className="placeholder-card p-4 p-md-5">
      <h1>Welcome, {session?.user.firstName}</h1>
      <p className="lead">Your orders and measurements will appear here soon.</p>
      <button className="btn btn-outline-secondary" onClick={() => void logout()}>Sign out</button>
    </section>
  );
}
