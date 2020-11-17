# Building SPV Channels client API


SPV Channels client API is a client library for SPV Channels API that can be used inside nodejs environment or inside web browser.

## Requirements:

SPV Channels client API requires NodeJS that can be downloaded from https://nodejs.org/

## Building:

1. Open the terminal

2. Navigate to folder containing SPV Channels client API

3. Install dependencies using npm

   ```
   npm install
   ```

4. Run build

   ```
   npm run build
   ```

   This will build client library into `build` folder and can be used for local develompent where you list client API as dependency via package.json.

5. Pack libraries into single .js file using webpack

   ```
   npm run dist
   ```

   This will pack client API and all of its dependencies into a single file inside `dist` folder.

6. Build and run node examples

   ```
   npm run build-examples
   ```
   
   To run node.js example run:

   ```
   npm run example-node [serviceUrl] [accountId] [username] [password]
   ```
   To run web example use:
   
   ```
   npm run example-web
   ```
   and open http://localhost:8080 in your browser.

7. Build documentation

   ```
   npm run doc
   ```

   This will create documentation into `doc` folder. Open [doc/index.html](./doc/index.html) in browser to view documentation.