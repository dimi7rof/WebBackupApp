import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering } from '@angular/platform-server';
import { appConfig } from './app.config';
import { LoadComponent } from './load/load.component';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(), LoadComponent
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
