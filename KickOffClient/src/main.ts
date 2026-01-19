import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

fetch('/api/home')
  .then(response => response.json())
  .then(data => {
    console.log(data.message);
  }
  ).catch(error => {
    console.error('Error fetching greeting:', error);
  });