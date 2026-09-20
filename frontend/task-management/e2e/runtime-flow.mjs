import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
const suffix = Date.now();
const email = `runtime-${suffix}@example.com`;
const userName = `Runtime User ${suffix}`;
const title = `Runtime task ${suffix}`;

try {
  await page.goto('http://localhost:4200/users');
  await page.getByLabel('Nombre').fill(userName);
  await page.getByLabel('Correo electrónico').fill(email);
  const userResponse = page.waitForResponse(
    (response) => response.url().endsWith('/api/users') && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Guardar usuario' }).click();
  if ((await userResponse).status() !== 201) throw new Error('User creation did not return 201.');

  await page.goto('http://localhost:4200/tasks');
  await page.getByRole('option', { name: userName }).waitFor({ state: 'attached' });
  await page.getByLabel('Responsable').selectOption({ label: userName });
  await page.getByLabel('Título').fill(title);
  await page.getByLabel('Información adicional').fill('{"priority":"High","tags":["runtime"]}');
  const taskResponse = page.waitForResponse(
    (response) => response.url().endsWith('/api/tasks') && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Guardar tarea' }).click();
  if ((await taskResponse).status() !== 201) throw new Error('Task creation did not return 201.');

  await page.goto('http://localhost:4200/task-list');
  await page.getByText(title).waitFor();
  await page.getByLabel('Estado').selectOption('Pending');
  await page.getByLabel('Prioridad').selectOption('High');
  const item = page.locator('[data-testid="task-card"]').filter({ hasText: title });
  await item.getByText('Pending').waitFor();

  await item.getByRole('button', { name: 'Avanzar a InProgress' }).click();
  await item.getByText('InProgress').waitFor();
  await item.getByRole('button', { name: 'Avanzar a Done' }).click();
  await item.getByText('Done').waitFor();

  await page.goto('http://localhost:4200/users');
  await page.getByLabel('Nombre').fill('Duplicate User');
  await page.getByLabel('Correo electrónico').fill(email.toUpperCase());
  const duplicateResponse = page.waitForResponse(
    (response) => response.url().endsWith('/api/users') && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Guardar usuario' }).click();
  if ((await duplicateResponse).status() !== 409) throw new Error('Duplicate user did not return 409.');
  await page.getByRole('alert').waitFor();

  console.log('UI runtime PASS: user/task creation, durable listing, filters, status transitions, and error display.');
} finally {
  await browser.close();
}
