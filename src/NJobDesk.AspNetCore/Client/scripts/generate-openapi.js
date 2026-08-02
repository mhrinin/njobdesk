import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { styleText } from 'node:util';
import { createClient, defaultPlugins } from '@hey-api/openapi-ts';

// The API's route prefix is host-configurable and injected at runtime as the client baseUrl
// (window.__NJOBDESK__.apiBase), so generated SDK paths must be prefix-relative.
const API_PREFIX = '/njobdesk/api/v1';

// Default input is the checked-in spec exported by the standalone demo:
//   dotnet run --project demo/Standalone.DemoSite -- --export-openapi src/NJobDesk.AspNetCore/openapi/openapi.json
const DEFAULT_SPEC = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..', 'openapi', 'openapi.json');

const green = (text) => styleText('green', text);
const red = (text) => styleText('red', text);
const yellow = (text) => styleText('yellow', text);

const input = process.argv[2] ?? DEFAULT_SPEC;

if (!/^https?:/.test(input) && !fs.existsSync(input)) {
  console.error(red(`ERROR: OpenAPI spec not found at ${input}`));
  console.error(`Export it first: ${yellow('dotnet run --project ../../demo/Standalone.DemoSite -- --export-openapi ../src/NJobDesk.AspNetCore/openapi/openapi.json')}`);
  process.exit(1);
}

console.log(green('Generating OpenAPI client...'));
console.log(`Using OpenAPI spec: ${yellow(input)}`);

await createClient({
  input,
  output: 'src/core/api',
  plugins: [
    ...defaultPlugins,
    {
      name: '@hey-api/client-fetch',
      runtimeConfigPath: '../hey-api',
    },
    {
      name: '@hey-api/sdk',
      asClass: true,
      classNameBuilder: '{{name}}Service',
    },
  ],
});

const sdkPath = 'src/core/api/sdk.gen.ts';
const sdk = fs.readFileSync(sdkPath, 'utf8');
fs.writeFileSync(sdkPath, sdk.replaceAll(`url: '${API_PREFIX}`, `url: '`));
console.log(`Stripped ${yellow(API_PREFIX)} from SDK paths (runtime baseUrl carries the prefix)`);
