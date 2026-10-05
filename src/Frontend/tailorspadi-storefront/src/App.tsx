import { useQuery } from '@tanstack/react-query';
import { Link, Route, Routes } from 'react-router-dom';
import { apiClient } from './api/client';

interface SystemInfo {
  applicationName: string;
}

function HomePage() {
  const systemInfo = useQuery({
    queryKey: ['system-info'],
    queryFn: async ({ signal }) => {
      const response = await apiClient.get<SystemInfo>('/system-info', { signal });
      return response.data;
    },
  });

  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Made to measure. Made for you.</p>
      <h1 className="display-5 fw-semibold">A home for great tailoring.</h1>
      <p className="lead mt-3">Discover a new way to connect with tailoring businesses.</p>
      <p className="text-secondary">Our storefront is taking shape. Collections and ordering are coming soon.</p>
      <div className="border-top mt-4 pt-3" aria-live="polite">
        {systemInfo.isPending && <p className="mb-0">Connecting...</p>}
        {systemInfo.isError && (
          <div role="alert">
            <p>We could not connect right now.</p>
            <button className="btn btn-outline-secondary" onClick={() => void systemInfo.refetch()}>Try again</button>
          </div>
        )}
        {systemInfo.data && <p className="mb-0">{systemInfo.data.applicationName} is connected.</p>}
      </div>
    </section>
  );
}

export function App() {
  return (
    <>
      <header className="site-header py-3">
        <nav className="container" aria-label="Main navigation">
          <Link className="fs-4 fw-semibold" to="/">TailorSpadi</Link>
        </nav>
      </header>
      <main className="container py-5">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="*" element={<section><h1>Page not found</h1><Link to="/">Return home</Link></section>} />
        </Routes>
      </main>
    </>
  );
}
