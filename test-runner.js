#!/usr/bin/env node
const { exec } = require('child_process');
exec('npx electron .', { cwd: 'C:\\Users\\43824939860\\Desktop\\J.A.R.V.I.S' }, (error, stdout, stderr) => {
  if (error) {
    console.error('Error:', error.message);
    return;
  }
  if (stderr) {
    console.error('Stderr:', stderr);
    return;
  }
  console.log('Stdout:', stdout);
});