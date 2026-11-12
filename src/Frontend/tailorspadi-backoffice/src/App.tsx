import { Link, Navigate, Route, Routes } from 'react-router-dom';
import {
  AuthProvider, RedirectIfAuthenticated, RequireAuth, TenantGate, TenantProvider, isPlatformHost, useTenant,
} from 'tailorspadi-shared';
import { ForgotPasswordPage, LoginPage, RegisterPage, ResetPasswordPage } from './pages/AuthPages';
import { OnboardingPage } from './pages/Onboarding';
import { PlatformHome, TenantBackoffice } from './pages/Workspaces';

const PATHS = { refresh: '/auth/refresh', login: '/auth/login', logout: '/auth/logout' };

function NotFound() {
  return <section><h1>Page not found</h1><Link to="/">Return home</Link></section>;
}

function PlatformRoutes() {
  return (
    <Routes>
      <Route element={<RedirectIfAuthenticated scope="platform" to="/businesses" />}>
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/login" element={<LoginPage />} />
      </Route>
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route element={<RequireAuth scope="platform" loginPath="/login" />}>
        <Route path="/businesses" element={<PlatformHome />} />
        <Route path="/onboarding" element={<OnboardingPage />} />
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

function TenantLogin() {
  return <LoginPage tenantName={useTenant().name} />;
}

function TenantRoutes() {
  return (
    <TenantGate>
      <Routes>
        <Route element={<RedirectIfAuthenticated scope="tenant" to="/backoffice" />}>
          <Route path="/login" element={<TenantLogin />} />
        </Route>
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route element={<RequireAuth scope="tenant" roles={['TenantOwner', 'TenantManager', 'TenantStaff']} loginPath="/login" />}>
          <Route path="/backoffice/*" element={<TenantBackoffice />} />
        </Route>
        <Route path="/" element={<Navigate to="/backoffice" replace />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </TenantGate>
  );
}

export function App() {
  const platform = isPlatformHost(window.location.hostname);
  return (
    <AuthProvider paths={PATHS}>
      <header className="site-header py-3">
        <nav className="container" aria-label="Business navigation">
          <span className="fs-4 fw-semibold text-white">TailorSpadi Business</span>
        </nav>
      </header>
      <main className="container py-5">
        {platform ? <PlatformRoutes /> : <TenantProvider><TenantRoutes /></TenantProvider>}
      </main>
    </AuthProvider>
  );
}
