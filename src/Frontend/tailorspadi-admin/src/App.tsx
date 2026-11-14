import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, Route, Routes, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  AuthProvider, ProblemAlert, RedirectIfAuthenticated, RequireAuth, TextField, toProblem, useAuth, type SessionInfo,
} from 'tailorspadi-shared';

const PATHS = { refresh: '/admin/auth/refresh', login: '/admin/auth/login', logout: '/admin/auth/logout' };

interface LoginForm { email: string; password: string }

function AdminLogin() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginForm>();

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      await login(values);
      navigate('/admin', { replace: true });
    } catch (caught) {
      setError(toProblem(caught).title);
    }
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Platform administration</p>
      <h1>Administrator sign in</h1>
      <form onSubmit={onSubmit} noValidate className="mt-3">
        <ProblemAlert message={error} />
        <TextField<LoginForm> name="email" label="Email" type="email" autoComplete="username" register={register} errors={errors} options={{ required: 'Enter your email' }} />
        <TextField<LoginForm> name="password" label="Password" type="password" autoComplete="current-password" register={register} errors={errors} options={{ required: 'Enter your password' }} />
        <button className="btn btn-primary" disabled={isSubmitting}>{isSubmitting ? 'Signing in...' : 'Sign in'}</button>
      </form>
    </section>
  );
}

function AdminPage() {
  const { api, logout } = useAuth();
  const me = useQuery({
    queryKey: ['admin-me'],
    queryFn: async () => (await api.get<SessionInfo>('/admin/auth/me')).data,
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Platform administration</p>
      <h1>TailorSpadi Administration</h1>
      {me.data && <p className="lead mt-3">Signed in as {me.data.user.email}.</p>}
      {me.isError && <p role="alert">{toProblem(me.error).title}</p>}
      <p className="text-secondary">The platform management workspace is coming soon.</p>
      <button className="btn btn-outline-secondary" onClick={() => void logout()}>Sign out</button>
    </section>
  );
}

export function App() {
  return (
    <AuthProvider paths={PATHS}>
      <header className="site-header py-3">
        <nav className="container" aria-label="Administration navigation">
          <Link className="fs-4 fw-semibold" to="/admin">TailorSpadi Admin</Link>
        </nav>
      </header>
      <main className="container py-5">
        <Routes>
          <Route element={<RedirectIfAuthenticated scope="admin" to="/admin" />}>
            <Route path="/admin/login" element={<AdminLogin />} />
          </Route>
          <Route element={<RequireAuth scope="admin" roles={['PlatformAdmin']} loginPath="/admin/login" />}>
            <Route path="/" element={<AdminPage />} />
            <Route path="/admin" element={<AdminPage />} />
          </Route>
          <Route path="*" element={<section><h1>Page not found</h1><Link to="/admin">Return to administration</Link></section>} />
        </Routes>
      </main>
    </AuthProvider>
  );
}
