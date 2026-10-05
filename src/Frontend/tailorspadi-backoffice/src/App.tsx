import { Link, Route, Routes } from 'react-router-dom';

function Placeholder({ title, description }: { title: string; description: string }) {
  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Your craft. Your business.</p>
      <h1>{title}</h1>
      <p className="lead mt-3">{description}</p>
      <p className="text-secondary mb-0">This area is coming soon.</p>
    </section>
  );
}

export function App() {
  return (
    <>
      <header className="site-header py-3">
        <nav className="container" aria-label="Business navigation">
          <span className="fs-4 fw-semibold text-white">TailorSpadi Business</span>
        </nav>
      </header>
      <main className="container py-5">
        <Routes>
          <Route path="/" element={<Placeholder title="Welcome to TailorSpadi Business" description="A place to manage your tailoring business." />} />
          <Route path="/register" element={<Placeholder title="Create your account" description="Start your journey with TailorSpadi." />} />
          <Route path="/onboarding" element={<Placeholder title="Set up your business" description="Make a home for your brand." />} />
          <Route path="/login" element={<Placeholder title="Welcome back" description="Your business workspace is taking shape." />} />
          <Route path="/backoffice/*" element={<Placeholder title="Your workspace" description="Orders, customers, and your team's work will come together here." />} />
          <Route path="*" element={<section><h1>Page not found</h1><Link to="/">Return home</Link></section>} />
        </Routes>
      </main>
    </>
  );
}
