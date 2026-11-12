import { Link, Route, Routes } from 'react-router-dom';
import { isPlatformHost, AuthProvider, RedirectIfAuthenticated, RequireAuth, TenantGate, TenantProvider, useAuth, useTenant } from 'tailorspadi-shared';
import { CustomerHome, CustomerLoginPage, RegisterCustomerPage } from './pages/CustomerAuth';

const PATHS = { refresh: '/auth/refresh', login: '/auth/login', logout: '/auth/logout' };

function Home() {
  const tenant = useTenant();
  return (
    <section className="placeholder-card p-4 p-md-5">
      {tenant.logoUrl && <img src={tenant.logoUrl} alt={`${tenant.name} logo`} style={{ maxHeight: 96, maxWidth: 192 }} className="mb-3 d-block" />}
      <h1 className="display-5 fw-semibold">{tenant.name}</h1>
      {tenant.description && <p className="lead mt-3">{tenant.description}</p>}
      <p className="text-secondary">{[tenant.city, tenant.state, tenant.country].filter(Boolean).join(', ')}</p>
      <p className="mb-0">{tenant.phone}{tenant.whatsApp && ` · WhatsApp ${tenant.whatsApp}`}</p>
      <p className="text-secondary mt-3 mb-0">Collections and ordering are coming soon.</p>
    </section>
  );
}

function Header() {
  const tenant = useTenant();
  const { session } = useAuth();
  return (
    <header className="site-header py-3" style={{ background: 'var(--tenant-primary, #164a41)' }}>
      <nav className="container d-flex justify-content-between" aria-label="Main navigation">
        <Link className="fs-4 fw-semibold" to="/">{tenant.name}</Link>
        <span>{session ? <Link to="/customer">My account</Link> : <Link to="/customer/login">Sign in</Link>}</span>
      </nav>
    </header>
  );
}

function Shell() {
  return (
    <>
      <Header />
      <main className="container py-5">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route element={<RedirectIfAuthenticated scope="tenant" to="/customer" />}>
            <Route path="/register-customer" element={<RegisterCustomerPage />} />
            <Route path="/customer/login" element={<CustomerLoginPage />} />
          </Route>
          <Route element={<RequireAuth scope="tenant" roles={['Customer', 'TenantOwner', 'TenantManager', 'TenantStaff']} loginPath="/customer/login" />}>
            <Route path="/customer" element={<CustomerHome />} />
          </Route>
          <Route path="*" element={<section><h1>Page not found</h1><Link to="/">Return home</Link></section>} />
        </Routes>
      </main>
    </>
  );
}

function PlatformLanding() {
  return (
    <>
      <header className="site-header py-3">
        <nav className="container" aria-label="Main navigation"><span className="fs-4 fw-semibold text-white">TailorSpadi</span></nav>
      </header>
      <main className="container py-5">
        <section className="placeholder-card p-4 p-md-5">
          <p className="eyebrow">Made to measure. Made for you.</p>
          <h1 className="display-5 fw-semibold">A home for great tailoring.</h1>
          <p className="lead mt-3">Tailors run their business on TailorSpadi. Customers find them at their own address.</p>
          <p className="mb-0"><a className="btn btn-primary me-2" href="/register">Start your business</a><a className="btn btn-outline-secondary" href="/login">Sign in</a></p>
        </section>
      </main>
    </>
  );
}

export function App() {
  if (isPlatformHost(window.location.hostname)) {
    return <PlatformLanding />;
  }
  return (
    <AuthProvider paths={PATHS}>
      <TenantProvider>
        <TenantGate><Shell /></TenantGate>
      </TenantProvider>
    </AuthProvider>
  );
}
