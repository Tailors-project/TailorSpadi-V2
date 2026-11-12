import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';
import type { Role, Scope } from './types';

export function RequireAuth({ scope, roles, loginPath }: { scope: Scope; roles?: Role[]; loginPath: string }) {
  const { status, session } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <p role="status" className="text-secondary">Loading...</p>;
  }
  if (!session || session.scope !== scope) {
    return <Navigate to={loginPath} replace state={{ from: location.pathname }} />;
  }
  if (roles && (!session.role || !roles.includes(session.role))) {
    return (
      <section role="alert">
        <h1>No access</h1>
        <p>Your account does not have access to this area.</p>
      </section>
    );
  }
  return <Outlet />;
}

export function RedirectIfAuthenticated({ scope, to }: { scope: Scope; to: string }) {
  const { status, session } = useAuth();
  if (status === 'loading') {
    return <p role="status" className="text-secondary">Loading...</p>;
  }
  return session?.scope === scope ? <Navigate to={to} replace /> : <Outlet />;
}
