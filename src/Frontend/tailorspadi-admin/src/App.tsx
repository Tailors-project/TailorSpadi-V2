import { Link, Route, Routes } from 'react-router-dom';

function AdminPage() {
  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Platform administration</p>
      <h1>TailorSpadi Administration</h1>
      <p className="lead mt-3">The platform management workspace is coming soon.</p>
    </section>
  );
}

export function App() {
  return (
    <>
      <header className="site-header py-3">
        <nav className="container" aria-label="Administration navigation">
          <Link className="fs-4 fw-semibold" to="/admin">TailorSpadi Admin</Link>
        </nav>
      </header>
      <main className="container py-5">
        <Routes>
          <Route path="/" element={<AdminPage />} />
          <Route path="/admin/*" element={<AdminPage />} />
          <Route path="*" element={<section><h1>Page not found</h1><Link to="/admin">Return to administration</Link></section>} />
        </Routes>
      </main>
    </>
  );
}
