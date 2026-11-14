import { describe, expect, it } from 'vitest';
import { isPlatformHost, isTenantHost, slugPreview, tenantOrigin } from './hosts';

describe('host helpers', () => {
  it('tells platform and tenant hosts apart', () => {
    expect(isPlatformHost('localhost')).toBe(true);
    expect(isPlatformHost('tailorspadi.localhost')).toBe(true);
    expect(isTenantHost('acme.tailorspadi.localhost')).toBe(true);
    expect(isPlatformHost('tailorspadi.com')).toBe(true);
    expect(isPlatformHost('127.0.0.1')).toBe(true);
    expect(isTenantHost('john.localhost')).toBe(true);
    expect(isTenantHost('john.tailorspadi.com')).toBe(true);
  });
  it('previews slugs like the server', () => {
    expect(slugPreview('John & Sons Fashion')).toBe('john-and-sons-fashion');
    expect(slugPreview('  Café Élégance!! ')).toBe('cafe-elegance');
    expect(slugPreview('x'.repeat(80))).toHaveLength(59);
  });
  it('builds tenant origins from the current location', () => {
    expect(tenantOrigin({ protocol: 'https:', hostname: 'tailorspadi.localhost', port: '8443' }, 'acme')).toBe('https://acme.tailorspadi.localhost:8443');
    expect(tenantOrigin({ protocol: 'http:', hostname: 'localhost', port: '8080' }, 'acme-001')).toBe('http://acme-001.localhost:8080');
    expect(tenantOrigin({ protocol: 'https:', hostname: 'a.tailorspadi.com', port: '' }, 'b')).toBe('https://b.tailorspadi.com');
  });
});
