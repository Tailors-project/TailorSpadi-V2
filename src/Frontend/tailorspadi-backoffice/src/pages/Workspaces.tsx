import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ProblemAlert, toProblem, useAuth, useTenant, type SessionInfo } from 'tailorspadi-shared';

/** Platform host home for a signed-in owner: their businesses and any unfinished setup. */
export function PlatformHome() {
  const { api, session, logout } = useAuth();
  const me = useQuery({
    queryKey: ['me', session?.user.id],
    queryFn: async () => (await api.get<SessionInfo>('/auth/me')).data,
    staleTime: 0,
  });
  const info = me.data ?? session;

  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Your craft. Your business.</p>
      <h1>Hello, {session?.user.firstName}</h1>
      {info?.hasDraft && <p><Link className="btn btn-primary" to="/onboarding">Continue setting up your business</Link></p>}
      {info && info.businesses.length === 0 && !info.hasDraft && (
        <p><Link className="btn btn-primary" to="/onboarding">Set up your business</Link></p>
      )}
      {info && info.businesses.length > 0 && (
        <>
          <h2 className="h5 mt-4">Your businesses</h2>
          <ul className="list-group mb-3">
            {info.businesses.map((business) => (
              <li key={business.subdomain} className="list-group-item d-flex justify-content-between align-items-center">
                <span>{business.name}</span>
                <span><a href={business.websiteUrl} className="me-3">Store</a><a href={business.backofficeUrl}>Backoffice</a></span>
              </li>
            ))}
          </ul>
          <p className="text-secondary">Each business has its own address, so you sign in again when you open its backoffice.</p>
          <Link to="/onboarding">Add another business</Link>
        </>
      )}
      <p className="mt-4 mb-0"><button className="btn btn-outline-secondary" onClick={() => void logout()}>Sign out</button></p>
    </section>
  );
}

/** Tenant host backoffice landing for owners, managers and staff. */
export function TenantBackoffice() {
  const { api, session, logout } = useAuth();
  const tenant = useTenant();
  const [error, setError] = useState<string | null>(null);
  const [logoUrl, setLogoUrl] = useState(tenant.logoUrl);
  const isOwner = session?.role === 'TenantOwner';

  async function replaceLogo(file: File | undefined) {
    if (!file) { return; }
    setError(null);
    try {
      const form = new FormData();
      form.append('file', file);
      const response = await api.post<{ logoUrl: string }>('/tenant/logo', form, { headers: { 'Content-Type': undefined } });
      setLogoUrl(`${response.data.logoUrl}${response.data.logoUrl.includes('?') ? '&' : '?'}v=${Date.now()}`);
    } catch (caught) {
      const problem = toProblem(caught);
      setError(problem.code === 'invalid_image' ? 'Use a PNG, JPEG or WebP image.' : problem.title);
    }
  }

  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">{tenant.name}</p>
      <h1>Your workspace</h1>
      <p className="lead">Orders, customers, and your team's work will come together here.</p>
      {logoUrl && <img src={logoUrl} alt={`${tenant.name} logo`} style={{ maxHeight: 96, maxWidth: 192 }} className="mb-3 d-block" />}
      {isOwner && (
        <div className="mb-3">
          <ProblemAlert message={error} />
          <label htmlFor="logo" className="form-label">Replace logo</label>
          <input id="logo" type="file" accept="image/png,image/jpeg,image/webp" className="form-control" onChange={(event) => void replaceLogo(event.target.files?.[0])} />
        </div>
      )}
      <button className="btn btn-outline-secondary" onClick={() => void logout()}>Sign out</button>
    </section>
  );
}
