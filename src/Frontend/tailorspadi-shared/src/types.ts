export type Scope = 'tenant' | 'platform' | 'admin';
export type Role = 'TenantOwner' | 'TenantManager' | 'TenantStaff' | 'Customer' | 'PlatformAdmin';

export interface Business {
  name: string;
  subdomain: string;
  role: Role;
  websiteUrl: string;
  backofficeUrl: string;
}

export interface SessionInfo {
  scope: Scope;
  user: { id: string; email: string; firstName: string; lastName: string; phone: string | null };
  role: Role | null;
  tenant: { name: string; subdomain: string; websiteUrl: string } | null;
  businesses: Business[];
  hasDraft: boolean;
}

export interface SessionResponse {
  accessToken: string;
  expiresInSeconds: number;
  session: SessionInfo;
}

export interface OnboardingBusiness {
  businessName: string;
  description: string;
  phone: string;
  whatsApp: string;
  email: string;
  address: string;
  city: string;
  state: string;
  country: string;
}

export interface OnboardingState {
  hasDraft: boolean;
  draftId: string | null;
  step: 1 | 2 | 3 | 4;
  slugBase: string | null;
  business: OnboardingBusiness;
  branding: { primaryColor: string; secondaryColor: string; logoPreviewDataUrl: string | null };
}

export interface CompleteResult {
  businessName: string;
  subdomain: string;
  websiteUrl: string;
  backofficeUrl: string;
}

export interface SubdomainPreview {
  slugBase: string;
  suggestedSubdomain: string;
  url: string;
  available: boolean;
}

export interface TenantBranding {
  name: string;
  description: string;
  subdomain: string;
  logoUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  phone: string;
  whatsApp: string;
  email: string;
  address: string;
  city: string;
  state: string;
  country: string;
}
