import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

test('register, resume onboarding, launch and sign in on the tenant host', async ({ page, context, baseURL }) => {
  const email = `browser-${randomUUID()}@example.test`;
  const password = `Tailor#${randomUUID()}`;
  try {
    await page.goto('/register');
    await page.getByLabel('First name', { exact: true }).fill('Browser');
    await page.getByLabel('Last name', { exact: true }).fill('Verification');
    await page.getByLabel('Email', { exact: true }).fill(email);
    await page.getByLabel('Phone', { exact: true }).fill('+2348012345678');
    await page.getByLabel('Password', { exact: true }).fill(password);
    await page.getByLabel('Confirm password', { exact: true }).fill(password);
    await page.getByRole('button', { name: 'Create account', exact: true }).click();
    await expect(page.getByText('Tell us about your business')).toBeVisible();
    await page.getByLabel('Business name', { exact: true }).fill(`Browser Verification ${randomUUID().slice(0, 8)}`);
    await page.getByLabel('Phone', { exact: true }).fill('+2348012345678');
    await page.getByLabel('Business email', { exact: true }).fill(email);
    await page.getByRole('button', { name: 'Continue', exact: true }).click();
    await expect(page.getByText('Make it yours')).toBeVisible();
    await page.getByRole('button', { name: 'Continue', exact: true }).click();
    await expect(page.getByLabel('Address name')).toHaveCount(0);
    await expect(page.getByText('Your store will be at')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Continue', exact: true })).toBeEnabled();
    await page.getByRole('button', { name: 'Continue', exact: true }).click();
    await expect(page.getByText('Review and launch')).toBeVisible();
    await page.reload();
    await expect(page.getByText('Review and launch')).toBeVisible();
    const platformCookie = (await context.cookies()).find(cookie => cookie.name === 'ts_rt_p');
    expect(platformCookie?.secure).toBe(true);
    expect(platformCookie?.httpOnly).toBe(true);
    expect(platformCookie?.sameSite).toBe('Strict');
    expect(platformCookie?.domain).toBe(new URL(baseURL!).hostname);
    await page.getByRole('button', { name: 'Launch my business' }).click();
    await expect(page.getByText('Your TailorSpadi website is ready.')).toBeVisible();
    const tenantLogin = await page.locator('a[href$="/login"]').getAttribute('href');
    const tenantUrl = new URL(tenantLogin!);
    expect(tenantUrl.protocol).toBe('https:');
    expect(tenantUrl.hostname).toMatch(/^browser-/);
    expect(tenantUrl.hostname.endsWith(`.${new URL(baseURL!).hostname}`)).toBe(true);
    expect(tenantUrl.pathname).toBe('/login');
    await page.goto(tenantLogin!);
    await page.getByLabel('Email', { exact: true }).fill(email);
    await page.getByLabel('Password', { exact: true }).fill(password);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Your workspace' })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Your workspace' })).toBeVisible();
    const tenantCookie = (await context.cookies()).find(cookie => cookie.name === 'ts_rt_t');
    expect(tenantCookie?.domain).toBe(tenantUrl.hostname);
    expect(tenantCookie?.secure).toBe(true);
    expect(tenantCookie?.httpOnly).toBe(true);
    expect(await page.evaluate(() => localStorage.length)).toBe(0);
    expect(await page.evaluate(() => sessionStorage.length)).toBe(0);
  } finally {
    // Remove only this test's randomly named account and its business. No demo data is retained.
    const env = Object.fromEntries(readFileSync('.env', 'utf8').split(/\r?\n/).filter(line => /^[A-Z_]+=/.test(line)).map(line => {
      const separator = line.indexOf('=');
      return [line.slice(0, separator), line.slice(separator + 1)];
    }));
    const user = `(SELECT "Id" FROM "Users" WHERE "Email" = '${email}')`;
    const tenants = `(SELECT "Id" FROM "Tenants" WHERE "CreatedByUserId" IN ${user})`;
    const sql = `BEGIN;
      DELETE FROM "AuditLog" WHERE "UserId" IN ${user} OR "TenantId" IN ${tenants};
      DELETE FROM "RefreshTokens" WHERE "UserId" IN ${user};
      DELETE FROM "PasswordResetTokens" WHERE "UserId" IN ${user};
      DELETE FROM "TenantMemberships" WHERE "UserId" IN ${user};
      DELETE FROM "Tenants" WHERE "CreatedByUserId" IN ${user};
      DELETE FROM "Users" WHERE "Email" = '${email}'; COMMIT;`;
    execFileSync('docker', ['compose', 'exec', '-T', 'postgres', 'psql', '-U', env.POSTGRES_USER, '-d', env.POSTGRES_DB, '-v', 'ON_ERROR_STOP=1', '-c', sql], { stdio: 'pipe' });
  }
});
