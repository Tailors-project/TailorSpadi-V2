// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { RequireAuth } from './guards';

const { auth } = vi.hoisted(() => ({ auth: { status: 'authenticated', session: null as null | { scope: string; role: string | null } } }));
vi.mock('./AuthProvider', () => ({ useAuth: () => auth }));
afterEach(cleanup);

function show() {
  render(<MemoryRouter initialEntries={['/backoffice']}><Routes>
    <Route path="/login" element={<p>Sign in</p>} />
    <Route element={<RequireAuth scope="tenant" roles={['TenantOwner']} loginPath="/login" />}>
      <Route path="/backoffice" element={<p>Protected workspace</p>} />
    </Route>
  </Routes></MemoryRouter>);
}

describe('route guards', () => {
  it('redirects a platform session away from a tenant workspace', () => {
    auth.session = { scope: 'platform', role: null };
    show();
    expect(screen.getByText('Sign in')).toBeTruthy();
  });
  it('denies a customer access to the owner workspace', () => {
    auth.session = { scope: 'tenant', role: 'Customer' };
    show();
    expect(screen.getByText('No access')).toBeTruthy();
  });
  it('allows the matching owner session', () => {
    auth.session = { scope: 'tenant', role: 'TenantOwner' };
    show();
    expect(screen.getByText('Protected workspace')).toBeTruthy();
  });
});
