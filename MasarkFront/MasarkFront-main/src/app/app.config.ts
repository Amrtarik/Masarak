import { ApplicationConfig, APP_INITIALIZER, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { ChatSignalRService } from './core/services/signalr';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';

// ميثود لتشغيل الخدمة عند بدء التطبيق
export function initializeSignalR(signalrService: ChatSignalRService) {
  return () => {
    // بمجرد عمل الحقل، الـ Constructor الخاص بالخدمة سيعمل ويبدأ الاتصال
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),

    provideHttpClient(withInterceptors([authInterceptor])),

    {
      provide: APP_INITIALIZER,
      useFactory: initializeSignalR,
      deps: [ChatSignalRService],
      multi: true,
    },
  ],
};
