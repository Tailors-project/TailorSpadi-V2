// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { OnboardingPage } from './Onboarding';
import { RegisterPage } from './AuthPages';

const { api, login } = vi.hoisted(() => ({
  api: { get: vi.fn(), put: vi.fn(), post: vi.fn() }, login: vi.fn(),
}));
vi.mock('tailorspadi-shared', async (original) => ({
  ...await original<typeof import('tailorspadi-shared')>(),
  useAuth: () => ({ api, login }),
}));

const draft = {
  hasDraft: true, draftId: 'draft-1', step: 4, slugBase: 'ada-custom',
  business: { businessName: 'Ada Fashion', description: '', phone: '+2348012345678', whatsApp: '', email: 'ada@example.test', address: '', city: 'Lagos', state: '', country: 'Nigeria' },
  branding: { primaryColor: '#112233', secondaryColor: '#AABBCC', logoPreviewDataUrl: null },
};

function show(page = <OnboardingPage />) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(<MemoryRouter><QueryClientProvider client={client}>{page}</QueryClientProvider></MemoryRouter>);
}

beforeEach(() => vi.resetAllMocks());
afterEach(cleanup);

describe('onboarding', () => {
  it.each(['222222222222222222222222', '-------', '12+3456789'])('blocks invalid WhatsApp number %s before saving', async (whatsApp) => {
    api.get.mockResolvedValue({ data: { ...draft, step: 1 } });
    show();
    fireEvent.change(await screen.findByLabelText('WhatsApp (optional)'), { target: { value: whatsApp } });
    fireEvent.click(screen.getByText('Continue'));
    await screen.findByText('Enter a valid WhatsApp number with 7–15 digits');
    expect(api.put).not.toHaveBeenCalled();
  });

  it.each(['', '+234 (901) 234-5678'])('allows optional or valid WhatsApp number %s', async (whatsApp) => {
    api.get.mockResolvedValue({ data: { ...draft, step: 1 } });
    api.put.mockResolvedValue({ data: { ...draft, step: 2 } });
    show();
    fireEvent.change(await screen.findByLabelText('WhatsApp (optional)'), { target: { value: whatsApp } });
    fireEvent.click(screen.getByText('Continue'));
    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/onboarding/business', expect.objectContaining({ whatsApp })));
  });

  it('shows an initial error and allows retry instead of staying on loading', async () => {
    api.get.mockRejectedValueOnce(new Error('Unavailable')).mockResolvedValue({ data: draft });
    show();
    await screen.findByText('We could not load your setup.');
    fireEvent.click(screen.getByText('Try again'));
    await screen.findByText('Review and launch');
  });

  it('resumes the review step and submits the saved address', async () => {
    api.get.mockResolvedValue({ data: draft });
    api.post.mockResolvedValue({ data: { businessName: 'Ada Fashion', subdomain: 'ada-custom-001', websiteUrl: 'https://ada-custom-001.localhost:8443/', backofficeUrl: 'https://ada-custom-001.localhost:8443/login' } });
    show();
    fireEvent.click(await screen.findByText('Launch my business'));
    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/onboarding/complete', { draftId: 'draft-1' }));
    await screen.findByText('Your TailorSpadi website is ready.');
  });

  it('saves the address before showing review', async () => {
    api.get.mockResolvedValue({ data: { ...draft, step: 3 } });
    api.post.mockResolvedValue({ data: { slugBase: 'ada-custom', suggestedSubdomain: 'ada-fashion', url: 'https://ada-fashion.localhost/', available: true } });
    api.put.mockResolvedValue({ data: draft });
    show();
    await screen.findByText(/Your store will be at/);
    expect(screen.queryByLabelText('Address name')).toBeNull();
    expect(screen.getByText('https://ada-fashion.localhost/')).toBeTruthy();
    fireEvent.click(screen.getByText('Continue'));
    await screen.findByText('Review and launch');
    expect(api.put).toHaveBeenCalledWith('/onboarding/subdomain', {});
  });
});

describe('registration', () => {
  it('validates empty fields before contacting the API', async () => {
    show(<RegisterPage />);
    fireEvent.click(screen.getByText('Create account'));
    await screen.findByText('Enter your first name');
    expect(api.post).not.toHaveBeenCalled();
  });

  it('shows duplicate-email errors on the email field', async () => {
    api.post.mockRejectedValue({ isAxiosError: true, response: { data: { status: 409, code: 'email_taken', title: 'Email taken' } } });
    show(<RegisterPage />);
    for (const [label, value] of Object.entries({ 'First name': 'Ada', 'Last name': 'Okafor', Email: 'ada@example.test', Phone: '+2348012345678', Password: 'Sew-Tailor#Lagos-2026', 'Confirm password': 'Sew-Tailor#Lagos-2026' })) {
      fireEvent.change(screen.getByLabelText(label, { exact: true }), { target: { value } });
    }
    fireEvent.click(screen.getByText('Create account'));
    await screen.findByText('An account with this email already exists. Try signing in.');
    expect(login).not.toHaveBeenCalled();
  });
});
