import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ProblemAlert, TextField, applyServerErrors, phoneProblem, toProblem, useAuth,
  type CompleteResult, type OnboardingBusiness, type OnboardingState, type SubdomainPreview,
} from 'tailorspadi-shared';

const STEPS = ['Business', 'Branding', 'Web address', 'Review'] as const;
const BUSINESS_FIELDS = ['businessName', 'description', 'phone', 'whatsApp', 'email', 'address', 'city', 'state', 'country'] as const;
const COLOR = /^#[0-9A-Fa-f]{6}$/;

function Progress({ step }: { step: number }) {
  return (
    <ol className="list-inline mb-4" aria-label="Progress">
      {STEPS.map((label, index) => (
        <li key={label} className={`list-inline-item ${index + 1 === step ? 'fw-bold' : 'text-secondary'}`} aria-current={index + 1 === step ? 'step' : undefined}>
          {index + 1}. {label}
        </li>
      ))}
    </ol>
  );
}

export function OnboardingPage() {
  const { api } = useAuth();
  const queryClient = useQueryClient();
  const [step, setStep] = useState<number | null>(null);
  const [result, setResult] = useState<CompleteResult | null>(null);

  const state = useQuery({
    queryKey: ['onboarding'],
    queryFn: async () => (await api.get<OnboardingState>('/onboarding')).data,
    staleTime: 0,
  });

  useEffect(() => {
    if (state.data && step === null) {
      setStep(state.data.step);
    }
  }, [state.data, step]);

  const refresh = (data: OnboardingState) => queryClient.setQueryData(['onboarding'], data);

  if (state.isError) {
    return <div role="alert"><p>We could not load your setup.</p><button className="btn btn-outline-secondary" onClick={() => void state.refetch()}>Try again</button></div>;
  }
  if (state.isPending || step === null) {
    return <p role="status">Loading...</p>;
  }
  if (result) {
    return <Done result={result} />;
  }

  const data = state.data;
  return (
    <section className="placeholder-card p-4 p-md-5">
      <p className="eyebrow">Set up your business</p>
      <Progress step={step} />
      {step === 1 && <BusinessStep data={data} onSaved={(next) => { refresh(next); setStep(2); }} />}
      {step === 2 && <BrandingStep data={data} onSaved={(next) => { refresh(next); setStep(3); }} onBack={() => setStep(1)} />}
      {step === 3 && <AddressStep data={data} onSaved={(next) => { refresh(next); setStep(4); }} onBack={() => setStep(2)} />}
      {step === 4 && <ReviewStep data={data} onBack={() => setStep(3)} onDone={(complete) => { void queryClient.invalidateQueries({ queryKey: ['onboarding'] }); setResult(complete); }} />}
    </section>
  );
}

function BusinessStep({ data, onSaved }: { data: OnboardingState; onSaved(state: OnboardingState): void }) {
  const { api } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const { register, handleSubmit, setError: setFieldError, formState: { errors, isSubmitting } } =
    useForm<OnboardingBusiness>({ defaultValues: { ...data.business, country: data.business.country || 'Nigeria' } });

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      onSaved((await api.put<OnboardingState>('/onboarding/business', values)).data);
    } catch (caught) {
      setError(applyServerErrors(toProblem(caught), setFieldError, BUSINESS_FIELDS));
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate>
      <h1 className="h3">Tell us about your business</h1>
      <ProblemAlert message={error} />
      <TextField<OnboardingBusiness> name="businessName" label="Business name" register={register} errors={errors}
        options={{ required: 'Enter your business name', minLength: { value: 2, message: 'At least 2 characters' }, maxLength: { value: 100, message: 'At most 100 characters' } }} />
      <TextField<OnboardingBusiness> name="description" label="Short description (optional)" register={register} errors={errors} options={{ maxLength: { value: 1000, message: 'At most 1000 characters' } }} />
      <div className="row">
        <div className="col-md-6"><TextField<OnboardingBusiness> name="phone" label="Phone" type="tel" register={register} errors={errors} options={{ validate: (value) => phoneProblem(value) ?? true }} /></div>
        <div className="col-md-6"><TextField<OnboardingBusiness> name="whatsApp" label="WhatsApp (optional)" type="tel" register={register} errors={errors} options={{ validate: (value) => phoneProblem(value, false) ? 'Enter a valid WhatsApp number with 7–15 digits' : true }} /></div>
      </div>
      <TextField<OnboardingBusiness> name="email" label="Business email" type="email" register={register} errors={errors}
        hint="Customers can contact your business at this address. It does not change your sign-in email."
        options={{ required: 'Enter a business email', pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email' } }} />
      <TextField<OnboardingBusiness> name="address" label="Address" register={register} errors={errors} />
      <div className="row">
        <div className="col-md-4"><TextField<OnboardingBusiness> name="city" label="City" register={register} errors={errors} /></div>
        <div className="col-md-4"><TextField<OnboardingBusiness> name="state" label="State" register={register} errors={errors} /></div>
        <div className="col-md-4"><TextField<OnboardingBusiness> name="country" label="Country" register={register} errors={errors} options={{ required: 'Enter a country' }} /></div>
      </div>
      <button className="btn btn-primary" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : 'Continue'}</button>
    </form>
  );
}

interface BrandingForm { primaryColor: string; secondaryColor: string }

function BrandingStep({ data, onSaved, onBack }: { data: OnboardingState; onSaved(state: OnboardingState): void; onBack(): void }) {
  const { api } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [logo, setLogo] = useState<string | null>(data.branding.logoPreviewDataUrl);
  const [uploading, setUploading] = useState(false);
  const { register, handleSubmit, watch, setValue, formState: { errors, isSubmitting } } =
    useForm<BrandingForm>({ defaultValues: { primaryColor: data.branding.primaryColor, secondaryColor: data.branding.secondaryColor } });
  const colors = watch();

  async function upload(file: File | undefined) {
    if (!file) {
      return;
    }
    setError(null);
    setUploading(true);
    try {
      const form = new FormData();
      form.append('file', file);
      const response = await api.post<OnboardingState>('/onboarding/logo', form, { headers: { 'Content-Type': undefined } });
      setLogo(response.data.branding.logoPreviewDataUrl);
    } catch (caught) {
      const problem = toProblem(caught);
      setError(problem.code === 'invalid_image' ? 'Use a PNG, JPEG or WebP image.' : problem.title);
    } finally {
      setUploading(false);
    }
  }

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      onSaved((await api.put<OnboardingState>('/onboarding/branding', values)).data);
    } catch (caught) {
      setError(toProblem(caught).title);
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate>
      <h1 className="h3">Make it yours</h1>
      <div className="p-3 mb-3 rounded" aria-label="Brand colour preview"
        style={{ backgroundColor: COLOR.test(colors.primaryColor) ? colors.primaryColor : undefined,
          border: `6px solid ${COLOR.test(colors.secondaryColor) ? colors.secondaryColor : 'transparent'}` }}>
        <span className="bg-white text-dark px-2 rounded">{data.business.businessName}</span>
      </div>
      <ProblemAlert message={error} />
      <div className="mb-3">
        <label htmlFor="logo" className="form-label">Logo (PNG, JPEG or WebP, optional)</label>
        <input id="logo" type="file" accept="image/png,image/jpeg,image/webp" className="form-control" disabled={uploading}
          onChange={(event) => void upload(event.target.files?.[0])} />
        {logo && <img src={logo} alt="Logo preview" className="mt-2" style={{ maxHeight: 96, maxWidth: 192 }} />}
      </div>
      <div className="row">
        {(['primaryColor', 'secondaryColor'] as const).map((name) => {
          const label = name === 'primaryColor' ? 'Primary colour' : 'Secondary colour';
          return (
            <div className="col-md-6 mb-3" key={name}>
              <label htmlFor={`field-${name}`} className="form-label">{label}</label>
              <div className="d-flex gap-2">
                <input type="color" className="form-control form-control-color flex-shrink-0"
                  aria-label={`Pick ${label.toLowerCase()}`} title={`Pick ${label.toLowerCase()}`}
                  value={COLOR.test(colors[name]) ? colors[name] : '#000000'}
                  onChange={(event) => setValue(name, event.target.value.toUpperCase(), { shouldValidate: true, shouldDirty: true })} />
                <input id={`field-${name}`} className={`form-control${errors[name] ? ' is-invalid' : ''}`}
                  aria-invalid={errors[name] ? true : undefined}
                  aria-describedby={errors[name] ? `field-${name}-error` : undefined}
                  {...register(name, { required: 'Choose a colour', pattern: { value: COLOR, message: 'Use the form #RRGGBB' } })} />
              </div>
              {errors[name] && <div id={`field-${name}-error`} className="invalid-feedback d-block">{errors[name]?.message}</div>}
            </div>
          );
        })}
      </div>
      <button type="button" className="btn btn-outline-secondary me-2" onClick={onBack}>Back</button>
      <button className="btn btn-primary" disabled={isSubmitting || uploading}>Continue</button>
    </form>
  );
}

function AddressStep({ data, onSaved, onBack }: { data: OnboardingState; onSaved(state: OnboardingState): void; onBack(): void }) {
  const { api } = useAuth();
  const [saving, setSaving] = useState(false);
  const [preview, setPreview] = useState<SubdomainPreview | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setPreview(null);
    const timer = setTimeout(async () => {
      try {
        const response = await api.post<SubdomainPreview>('/onboarding/subdomain/preview', {});
        if (active) { setPreview(response.data); setError(null); }
      } catch (caught) {
        if (active) {
          const problem = toProblem(caught);
          setPreview(null);
          setError(problem.errors.slugBase?.[0] ?? problem.title);
        }
      }
    }, 300);
    return () => { active = false; clearTimeout(timer); };
  }, [api, data.business.businessName]);

  async function saveAddress() {
    setSaving(true);
    setError(null);
    try {
      const response = await api.put<OnboardingState>('/onboarding/subdomain', {});
      onSaved(response.data);
    } catch (caught) {
      setError(toProblem(caught).title);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h1 className="h3">Your web address</h1>
      <p>Your address is generated automatically from your business name.</p>
      <ProblemAlert message={error} />
      {preview && <p aria-live="polite">Your store will be at <strong>{preview.url}</strong></p>}
      <button type="button" className="btn btn-outline-secondary me-2" onClick={onBack}>Back</button>
      <button className="btn btn-primary" disabled={!preview?.available || saving} onClick={() => void saveAddress()}>{saving ? 'Saving...' : 'Continue'}</button>
    </div>
  );
}

function ReviewStep({ data, onBack, onDone }: { data: OnboardingState; onBack(): void; onDone(result: CompleteResult): void }) {
  const { api } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function launch() {
    if (!data.draftId) { return; }
    setBusy(true);
    setError(null);
    try {
      const response = await api.post<CompleteResult>('/onboarding/complete', { draftId: data.draftId });
      onDone(response.data);
    } catch (caught) {
      setError(toProblem(caught).title);
      setBusy(false);
    }
  }

  const business = data.business;
  return (
    <div>
      <h1 className="h3">Review and launch</h1>
      <ProblemAlert message={error} />
      <dl className="row">
        <dt className="col-sm-3">Business</dt><dd className="col-sm-9">{business.businessName}</dd>
        <dt className="col-sm-3">Contact</dt><dd className="col-sm-9">{business.phone} · {business.email}</dd>
        <dt className="col-sm-3">Location</dt><dd className="col-sm-9">{[business.address, business.city, business.state, business.country].filter(Boolean).join(', ')}</dd>
        <dt className="col-sm-3">Colours</dt>
        <dd className="col-sm-9 d-flex flex-wrap gap-4">
          {(['primaryColor', 'secondaryColor'] as const).map((name) => {
            const label = name === 'primaryColor' ? 'Primary' : 'Secondary';
            const color = data.branding[name];
            return (
              <span key={name} className="d-inline-flex align-items-center gap-2">
                <span role="img" aria-label={`${label} colour: ${color}`} title={color}
                  className="d-inline-block border rounded"
                  style={{ width: 28, height: 28, backgroundColor: COLOR.test(color) ? color : undefined }} />
                {label}
              </span>
            );
          })}
        </dd>
      </dl>
      <button type="button" className="btn btn-outline-secondary me-2" onClick={onBack}>Back</button>
      <button className="btn btn-primary" onClick={() => void launch()} disabled={busy}>{busy ? 'Launching...' : 'Launch my business'}</button>
    </div>
  );
}

function Done({ result }: { result: CompleteResult }) {
  const navigate = useNavigate();
  return (
    <section className="placeholder-card p-4 p-md-5" role="status">
      <p className="eyebrow">You are live</p>
      <h1>{result.businessName} is ready</h1>
      <p>Your TailorSpadi website is ready.</p>
      <p>Your store: <a href={result.websiteUrl}>{result.websiteUrl}</a></p>
      <p>Sign in to manage it at <a href={result.backofficeUrl}>{result.backofficeUrl}</a>. You will sign in again there, because each business has its own address.</p>
      <button className="btn btn-outline-secondary" onClick={() => navigate('/businesses')}>Back to my businesses</button>
    </section>
  );
}
