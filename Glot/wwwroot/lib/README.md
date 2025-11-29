Place the following pre-built files in this folder to enable local Kuroshiro loading (recommended for offline / CSP-safe deployments):

- kuromoji.js
- kuroshiro.min.js
- kuroshiro-analyzer-kuromoji.min.js

You can obtain these from their npm/unpkg distributions and save the minified builds here. Filenames must match exactly as listed above so the interop loader can find them under `/lib/`.

Example (using curl or a browser):

1. Download kuromoji:
   - https://unpkg.com/kuromoji@0.1.2/dist/kuromoji.js

2. Download kuroshiro:
   - https://unpkg.com/kuroshiro@1.2.0/dist/kuroshiro.min.js

3. Download analyzer:
   - https://unpkg.com/kuroshiro-analyzer-kuromoji@1.0.0/dist/kuroshiro-analyzer-kuromoji.min.js

After placing the files here, the app will try the local `/lib` files before falling back to the CDN.