/** The platform host has no tenant label: `localhost`, `tailorspadi.com`. Tenant hosts add one label in front. */
export function isTenantHost(hostname: string): boolean {
  const host = hostname.toLowerCase();
  if (host === 'localhost' || host === 'tailorspadi.localhost' || host === '127.0.0.1' || host === '[::1]') {
    return false;
  }
  if (host.endsWith('.localhost')) {
    return true;
  }
  return host.split('.').length > 2 && !/^\d+\.\d+\.\d+\.\d+$/.test(host);
}

export function isPlatformHost(hostname: string): boolean {
  return !isTenantHost(hostname);
}

/** Mirrors the server's slug base rules for a live preview; the server stays authoritative. */
export function slugPreview(businessName: string): string {
  const slug = businessName
    .normalize('NFKD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/&/g, ' and ')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return slug.slice(0, 59).replace(/-+$/g, '');
}

/** Builds `<scheme>//<sub>.<base>[:port]` from the current location, for hints only. */
export function tenantOrigin(location: { protocol: string; hostname: string; port: string }, subdomain: string): string {
  const base = isTenantHost(location.hostname) ? location.hostname.split('.').slice(1).join('.') : location.hostname;
  const port = location.port ? `:${location.port}` : '';
  return `${location.protocol}//${subdomain}.${base}${port}`;
}
