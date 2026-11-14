import { Link, Navigate, Route, Routes } from 'react-router-dom';
import type { CSSProperties } from 'react';
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

function TenantShell() {
  const tenant = useTenant();
  const channels = tenant.primaryColor.slice(1).match(/.{2}/g)?.map((hex) => {
    const value = parseInt(hex, 16) / 255;
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  }) ?? [0, 0, 0];
  const luminance = channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
  const theme = {
    '--tenant-primary': tenant.primaryColor,
    '--tenant-secondary': tenant.secondaryColor,
    '--tenant-on-primary': luminance > 0.179 ? '#000000' : '#ffffff',
  } as CSSProperties;
  return (
    <div className="tenant-business" style={theme}>
      <header className="site-header py-3">
        <nav className="container" aria-label="Business navigation">
          <Link className="tenant-brand d-inline-flex align-items-center gap-3 fs-4 fw-semibold" to="/backoffice">
            {tenant.logoUrl && <img src={tenant.logoUrl} alt={`${tenant.name} logo`} className="tenant-logo" />}
            <span>{tenant.name}</span>
          </Link>
        </nav>
      </header>
      <main className="container py-5"><TenantRoutes /></main>
    </div>
  );
}

export function App() {
  const platform = isPlatformHost(window.location.hostname);
  return (
    <AuthProvider paths={PATHS}>
      {platform ? <>
      <header className="site-header py-3">
        <nav className="container" aria-label="Business navigation">
          <span className="fs-4 fw-semibold text-white">TailorSpadi Business</span>
        </nav>
      </header>
      <main className="container py-5">
        <PlatformRoutes />
      </main>
      </> : <TenantProvider><TenantGate><TenantShell /></TenantGate></TenantProvider>}
    </AuthProvider>
  );
}
